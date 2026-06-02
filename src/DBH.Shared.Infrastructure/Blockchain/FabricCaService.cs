using System.Formats.Asn1;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DBH.Shared.Contracts.Blockchain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DBH.Shared.Infrastructure.Blockchain;

/// <summary>
/// Calls the Hyperledger Fabric CA REST API (v1) to register and enroll a new user identity.
///
/// Flow:
///   1. Enroll bootstrap admin with the CA (Basic Auth with admin:adminpw) to get a CA-issued cert+key
///   2. Register – POST /api/v1/register  authenticated with the enrolled admin's cert+key token.
///   3. Enroll  – POST /api/v1/enroll    authenticated with Basic(id:secret) + a CSR.
///
/// When <see cref="FabricCaOptions.Enabled"/> is false the call is a no-op and returns Success=true
/// so that user registration is never blocked.
/// </summary>
public sealed class FabricCaService : IFabricCaService
{
    private readonly FabricCaOptions _options;
    private readonly FabricOptions _fabricOptions;
    private readonly IFabricRuntimeIdentityResolver _identityResolver;
    private readonly ILogger<FabricCaService> _logger;

    // Loaded lazily / once
    private ECDsa? _adminKey;
    private byte[]? _adminCertPemBytes;   // PEM bytes for the admin certificate
    private bool _adminLoaded;
    private string? _adminIdentityKey;

    public FabricCaService(
        IOptions<FabricCaOptions> options,
        IOptions<FabricOptions> fabricOptions,
        IFabricRuntimeIdentityResolver identityResolver,
        ILogger<FabricCaService> logger)
    {
        _options = options.Value;
        _fabricOptions = fabricOptions.Value;
        _identityResolver = identityResolver;
        _logger = logger;
    }

    // ========================================================================
    // Public API
    // ========================================================================

    public async Task<FabricEnrollResult> EnrollUserAsync(string enrollmentId, string username, string role, string? secret = null, string? organizationId = null)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Fabric CA enrollment is disabled – skipping for user {EnrollmentId}", enrollmentId);
            return new FabricEnrollResult
            {
                Success = true,
                EnrollmentId = enrollmentId,
                EnrollmentSecret = secret,
                AccountStoragePath = BuildAccountStoragePath(enrollmentId)
            };
        }

        try
        {
            // Resolve identity: use explicit organizationId if provided, else fallback to JWT context
            FabricRuntimeIdentity runtimeIdentity;
            if (!string.IsNullOrWhiteSpace(organizationId) && Guid.TryParse(organizationId, out var orgGuid))
            {
                _logger.LogInformation("Resolving Fabric identity for explicit OrgId={OrgId}", organizationId);
                runtimeIdentity = await _identityResolver.ResolveForOrganizationAsync(orgGuid);
            }
            else
            {
                runtimeIdentity = await _identityResolver.ResolveForCurrentContextAsync();
            }
            _logger.LogWarning(
                "[DEBUG-CA-FLOW] EnrollUserAsync - Resolved runtime identity: MspId={MspId}, CaUrl={CaUrl}, CaName={CaName}, IdentityKey={IdentityKey}, OrgId={OrgId}",
                runtimeIdentity.MspId, runtimeIdentity.CaUrl, runtimeIdentity.CaName, runtimeIdentity.IdentityKey, organizationId ?? "<JWT-context>");

            if (!await EnsureAdminLoadedAsync(runtimeIdentity))
            {
                _logger.LogWarning(
                    "Fabric CA admin identity unavailable for {EnrollmentId}; skipping enrollment.",
                    enrollmentId);

                return new FabricEnrollResult { Success = false, ErrorMessage = "Admin identity unavailable" };
            }

            // ----------------------------------------------------------------
            // Step 1: Register the identity on the CA (token auth with enrolled admin cert+key)
            // ----------------------------------------------------------------
            var enrollmentSecret = string.IsNullOrWhiteSpace(secret) ? GenerateSecret() : secret;
            await RegisterAsync(runtimeIdentity, enrollmentId, username, role, enrollmentSecret);

            // ----------------------------------------------------------------
            // Step 2: Generate a local EC key + CSR, then enroll
            // ----------------------------------------------------------------
            using var userKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var csrPem = CreateCsrPem(enrollmentId, userKey);
            var certPem = await DoEnrollAsync(runtimeIdentity, enrollmentId, enrollmentSecret, csrPem);
            _logger.LogInformation(
                "Fabric CA enrollment succeeded for identity {EnrollmentId} (role={Role})",
                enrollmentId, role);

            return new FabricEnrollResult
            {
                Success = true,
                EnrollmentId = enrollmentId,
                EnrollmentSecret = enrollmentSecret,
                AccountStoragePath = BuildAccountStoragePath(enrollmentId, runtimeIdentity),
                Certificate = certPem,
                PrivateKeyPem = ExportPrivateKeyPem(userKey),
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Fabric CA is unreachable for {EnrollmentId}; skipping enrollment.",
                enrollmentId);

            return new FabricEnrollResult
            {
                Success = true,
                EnrollmentId = enrollmentId,
                EnrollmentSecret = secret,
                AccountStoragePath = BuildAccountStoragePath(enrollmentId)
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(
                ex,
                "Fabric CA request timed out for {EnrollmentId}; skipping enrollment.",
                enrollmentId);

            return new FabricEnrollResult
            {
                Success = true,
                EnrollmentId = enrollmentId,
                EnrollmentSecret = secret,
                AccountStoragePath = BuildAccountStoragePath(enrollmentId)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fabric CA enrollment failed for identity {EnrollmentId}", enrollmentId);
            return new FabricEnrollResult
            {
                Success = false,
                EnrollmentId = enrollmentId,
                EnrollmentSecret = secret,
                AccountStoragePath = BuildAccountStoragePath(enrollmentId),
                ErrorMessage = ex.Message
            };
        }
    }

    // ========================================================================
    // Register (POST /api/v1/register) — token auth with admin cert+key
    // ========================================================================

    private async Task RegisterAsync(FabricRuntimeIdentity runtimeIdentity, string enrollmentId, string username, string role, string secret)
    {
        var body = JsonConvert.SerializeObject(new
        {
            id = enrollmentId,
            type = "client",
            secret,
            affiliation = runtimeIdentity.DefaultAffiliation,
            attrs = new[]
            {
                new { name = "username", value = username, ecert = true },
                new { name = "role",     value = role,     ecert = true },
                new { name = "ou",       value = role,     ecert = true },
            },
            caname = runtimeIdentity.CaName,
        });

        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var token = BuildAuthToken(runtimeIdentity, "POST", "/api/v1/register", bodyBytes);

        using var http = BuildHttpClient(runtimeIdentity);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{runtimeIdentity.CaUrl.TrimEnd('/')}/api/v1/register");

        // Fabric CA expects the raw token format "<base64-cert>.<base64-signature>"
        request.Headers.TryAddWithoutValidation("Authorization", token);
        request.Content = new ByteArrayContent(bodyBytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Fabric CA register failed [{response.StatusCode}]: {responseBody}");
        }

        _logger.LogDebug("Fabric CA register response for {EnrollmentId}: {Body}", enrollmentId, responseBody);
    }

    // ========================================================================
    // Enroll (POST /api/v1/enroll)
    // ========================================================================

    private async Task<string> DoEnrollAsync(FabricRuntimeIdentity runtimeIdentity, string enrollmentId, string secret, string csrPem)
    {
        // Fabric CA (CFSSL) expects the full PEM CSR in a field named "certificate_request".
        // The JSON tag comes from cfssl/signer.SignRequest.Request (`json:"certificate_request"`).
        var requestObj = new JObject
        {
            ["certificate_request"] = csrPem.Replace("\r\n", "\n").Replace("\r", ""),
            ["caname"] = runtimeIdentity.CaName
        };
        var body = requestObj.ToString(Formatting.None);

        using var http = BuildHttpClient(runtimeIdentity);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{runtimeIdentity.CaUrl.TrimEnd('/')}/api/v1/enroll");

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{enrollmentId}:{secret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Fabric CA enroll failed [{response.StatusCode}]: {responseBody}");
        }

        var json = JsonConvert.DeserializeObject<JObject>(responseBody);
        var success = json?.Value<bool?>("success") == true;
        var certB64 = json?["result"]?["Cert"]?.Value<string>();

        if (!success || string.IsNullOrWhiteSpace(certB64))
        {
            throw new InvalidOperationException($"Fabric CA enroll returned failure: {responseBody}");
        }

        return Encoding.UTF8.GetString(Convert.FromBase64String(certB64));
    }

    // ========================================================================
    // Fabric CA token auth: base64(certPEM) + "." + base64(ECDSA-sign(SHA256(payload)))
    //
    // payload = method + "." + base64(path) + "." + base64(body) + "." + base64(cert)
    // Matches fabric-ca util/util.go CreateToken → GenECDSAToken
    // Server verifies using r.RequestURI (path only, not full URL).
    // ========================================================================

    private string BuildAuthToken(FabricRuntimeIdentity runtimeIdentity, string method, string urlPath, byte[] bodyBytes)
    {
        if (_adminCertPemBytes == null || _adminKey == null)
            throw new InvalidOperationException("Admin crypto material not loaded.");

        var b64cert = Convert.ToBase64String(_adminCertPemBytes);
        var b64body = Convert.ToBase64String(bodyBytes);
        // Fabric CA server verifies against r.RequestURI which is the path only (e.g. "/api/v1/register")
        var b64uri = Convert.ToBase64String(Encoding.UTF8.GetBytes(urlPath));

        // payload must match Go's: method + "." + b64uri + "." + b64body + "." + b64cert
        var sigPayload = $"{method}.{b64uri}.{b64body}.{b64cert}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(sigPayload));

        // DER-encoded ECDSA signature – matches Go's crypto/ecdsa output
        var sig = _adminKey.SignHash(digest, DSASignatureFormat.Rfc3279DerSequence);
        var lowSSig = NormalizeEcdsaSignatureLowS(sig, _adminKey.KeySize);

        return $"{b64cert}.{Convert.ToBase64String(lowSSig)}";
    }

    // ========================================================================
    // Admin identity loading — enrolls with CA if disk certs are not CA-issued
    // ========================================================================

    private async Task<bool> EnsureAdminLoadedAsync(FabricRuntimeIdentity runtimeIdentity)
    {
        if (_adminLoaded && _adminIdentityKey == runtimeIdentity.IdentityKey) return true;

        _adminKey?.Dispose();
        _adminKey = null;
        _adminCertPemBytes = null;
        _adminLoaded = false;
        _adminIdentityKey = runtimeIdentity.IdentityKey;

        _logger.LogInformation(
            "Loading Fabric CA admin identity. AdminUsername={AdminUsername}, CaUrl={CaUrl}",
            _options.AdminUsername, runtimeIdentity.CaUrl);

        // Generate a fresh EC key pair for the admin identity
        _adminKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        try
        {
            // Create a CSR as a PEM string (with headers and line breaks) and
            // enroll the bootstrap admin with the CA to get a CA-issued cert.
            var request = new CertificateRequest("CN=admin", _adminKey, HashAlgorithmName.SHA256);
            var csrDer = request.CreateSigningRequest();
            var csrPem = "-----BEGIN CERTIFICATE REQUEST-----\n" +
                         Convert.ToBase64String(csrDer, Base64FormattingOptions.InsertLineBreaks) +
                         "\n-----END CERTIFICATE REQUEST-----";

            var certPem = await DoEnrollWithBootstrapAuth(runtimeIdentity, csrPem);
            _adminCertPemBytes = Encoding.ASCII.GetBytes(certPem);
            _adminLoaded = true;

            _logger.LogInformation(
                "Successfully enrolled bootstrap admin with Fabric CA. CA-issued cert obtained.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to enroll bootstrap admin with Fabric CA. Will be unable to register new identities.");

            _adminKey?.Dispose();
            _adminKey = null;
            return false;
        }
    }

    /// <summary>
    /// Enrolls the bootstrap admin with the CA using Basic Auth (admin:adminpw).
    /// This is specifically for obtaining a CA-issued certificate that can then
    /// be used to sign register tokens (which require token auth, not Basic Auth).
    /// </summary>
    private async Task<string> DoEnrollWithBootstrapAuth(FabricRuntimeIdentity runtimeIdentity, string csrPem)
    {
        // Fabric CA (CFSSL) expects the full PEM CSR in a field named "certificate_request".
        var normalizedPem = csrPem.Replace("\r\n", "\n").Replace("\r", "");

        var requestObj = new JObject
        {
            ["certificate_request"] = normalizedPem,
            ["caname"] = runtimeIdentity.CaName
        };
        var body = requestObj.ToString(Formatting.None);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        _logger.LogDebug("Bootstrap enroll CSR PEM (first 80 chars): {Csr}",
            normalizedPem.Length > 80 ? normalizedPem[..80] : normalizedPem);

        using var http = BuildHttpClient(runtimeIdentity);
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"{runtimeIdentity.CaUrl.TrimEnd('/')}/api/v1/enroll");

        // Use bootstrap admin credentials (Basic Auth) for enrollment
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_options.AdminUsername}:{_options.AdminPassword}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await http.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Fabric CA bootstrap admin enroll failed [{response.StatusCode}]: {responseBody}");
        }

        var json = JsonConvert.DeserializeObject<JObject>(responseBody);
        var success = json?.Value<bool?>("success") == true;
        var certB64 = json?["result"]?["Cert"]?.Value<string>();

        if (!success || string.IsNullOrWhiteSpace(certB64))
        {
            throw new InvalidOperationException($"Fabric CA bootstrap enroll returned failure: {responseBody}");
        }

        // CA returns base64-encoded PEM cert
        return Encoding.UTF8.GetString(Convert.FromBase64String(certB64));
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private HttpClient BuildHttpClient(FabricRuntimeIdentity runtimeIdentity)
    {
        HttpClientHandler handler;

        if (!string.IsNullOrEmpty(runtimeIdentity.TlsCaCertPath) && File.Exists(runtimeIdentity.TlsCaCertPath))
        {
            var caCert = new X509Certificate2(runtimeIdentity.TlsCaCertPath);
            handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (_, cert, chain, _) =>
            {
                if (chain == null || cert == null) return false;
                chain.ChainPolicy.ExtraStore.Add(caCert);
                return chain.Build(cert);
            };
        }
        else
        {
            // Permit self-signed certificates in development / simulation
            handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private static string CreateCsrPem(string cn, ECDsa key)
    {
        var request = new CertificateRequest($"CN={cn}", key, HashAlgorithmName.SHA256);
        var csrBytes = request.CreateSigningRequest();
        return "-----BEGIN CERTIFICATE REQUEST-----\n" +
               Convert.ToBase64String(csrBytes, Base64FormattingOptions.InsertLineBreaks) +
               "\n-----END CERTIFICATE REQUEST-----\n";
    }

    private static string ExportPrivateKeyPem(ECDsa key)
    {
        return key.ExportECPrivateKeyPem();
    }

    private string BuildAccountStoragePath(string enrollmentId, FabricRuntimeIdentity? runtimeIdentity = null)
    {
        var cryptoRoot = !string.IsNullOrWhiteSpace(_fabricOptions.CryptoRoot)
            ? _fabricOptions.CryptoRoot
            : Environment.GetEnvironmentVariable("FABRIC_CRYPTO_ROOT") ?? "/tmp/fabric-crypto";

        // Derive domain from runtime identity's MspId when available, else fall back to CaName-based logic
        string domain;
        if (runtimeIdentity != null)
        {
            domain = runtimeIdentity.MspId switch
            {
                "Hospital1MSP" => "hospital1.ehr.com",
                "Hospital2MSP" => "hospital2.ehr.com",
                "ClinicMSP" => "clinic.ehr.com",
                _ => "hospital1.ehr.com"
            };
        }
        else
        {
            domain = _options.CaName switch
            {
                "ca-hospital1" => "hospital1.ehr.com",
                "ca-hospital2" => "hospital2.ehr.com",
                "ca-clinic" => "clinic.ehr.com",
                _ => "hospital1.ehr.com"
            };
        }

        return $"{cryptoRoot.TrimEnd('/', '\\')}/peerOrganizations/{domain}/users/{enrollmentId}@{domain}/msp";
    }

    private static string GenerateSecret()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
    }

    private static byte[] NormalizeEcdsaSignatureLowS(byte[] derSignature, int keySizeBits)
    {
        var curveOrder = GetCurveOrder(keySizeBits);
        if (curveOrder <= BigInteger.Zero)
            return derSignature;

        try
        {
            var reader = new AsnReader(derSignature, AsnEncodingRules.DER);
            var sequence = reader.ReadSequence();
            var rBytes = sequence.ReadIntegerBytes().ToArray();
            var sBytes = sequence.ReadIntegerBytes().ToArray();
            sequence.ThrowIfNotEmpty();
            reader.ThrowIfNotEmpty();

            var r = new BigInteger(rBytes, isUnsigned: true, isBigEndian: true);
            var s = new BigInteger(sBytes, isUnsigned: true, isBigEndian: true);

            var halfOrder = curveOrder >> 1;
            if (s <= halfOrder)
                return derSignature;

            var lowS = curveOrder - s;

            var writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.WriteInteger(r);
            writer.WriteInteger(lowS);
            writer.PopSequence();
            return writer.Encode();
        }
        catch
        {
            return derSignature;
        }
    }

    private static BigInteger GetCurveOrder(int keySizeBits)
    {
        if (keySizeBits <= 256)
            return ParseUnsignedHex("FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551");

        if (keySizeBits <= 384)
            return ParseUnsignedHex("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFC7634D81F4372DDF581A0DB248B0A77AECEC196ACCC52973");

        return ParseUnsignedHex("01FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
    }

    private static BigInteger ParseUnsignedHex(string hex)
    {
        return new BigInteger(Convert.FromHexString(hex), isUnsigned: true, isBigEndian: true);
    }
}
