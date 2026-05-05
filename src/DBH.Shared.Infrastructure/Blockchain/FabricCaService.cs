using System.Net.Http.Headers;
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
///   1. Register  – POST /api/v1/register  authenticated with the admin's cert+key token.
///   2. Enroll    – POST /api/v1/enroll    authenticated with  Basic(id:secret) + a CSR.
///
/// When <see cref="FabricCaOptions.Enabled"/> is false the call is a no-op and returns Success=true
/// so that user registration is never blocked.
/// </summary>
public sealed class FabricCaService : IFabricCaService
{
    private readonly FabricCaOptions _options;
    private readonly IFabricRuntimeIdentityResolver _identityResolver;
    private readonly ILogger<FabricCaService> _logger;

    // Loaded lazily / once
    private ECDsa? _adminKey;
    private byte[]? _adminCertDer;   // DER bytes of the admin PEM certificate
    private bool _adminLoaded;
    private string? _adminIdentityKey;

    public FabricCaService(
        IOptions<FabricCaOptions> options,
        IFabricRuntimeIdentityResolver identityResolver,
        ILogger<FabricCaService> logger)
    {
        _options = options.Value;
        _identityResolver = identityResolver;
        _logger = logger;
    }

    // ========================================================================
    // Public API
    // ========================================================================

    public async Task<FabricEnrollResult> EnrollUserAsync(string enrollmentId, string username, string role, string? secret = null)
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
            var runtimeIdentity = await _identityResolver.ResolveForCurrentContextAsync();
            if (!await EnsureAdminLoadedAsync(runtimeIdentity))
            {
                _logger.LogWarning(
                    "Fabric CA crypto material is unavailable for {EnrollmentId}; skipping enrollment and continuing without blockchain registration.",
                    enrollmentId);

                return new FabricEnrollResult { Success = true };
            }

            // ----------------------------------------------------------------
            // Step 1: Register the identity on the CA
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
                AccountStoragePath = BuildAccountStoragePath(enrollmentId),
                Certificate = certPem,
                PrivateKeyPem = ExportPrivateKeyPem(userKey),
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Fabric CA is unreachable for {EnrollmentId}; skipping enrollment and continuing without blockchain registration.",
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
                "Fabric CA request timed out for {EnrollmentId}; skipping enrollment and continuing without blockchain registration.",
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
    // Register (POST /api/v1/register)
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
        // in Authorization, which does not conform to the standard scheme-based parser.
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
        var body = JsonConvert.SerializeObject(new
        {
            signingRequest = csrPem,
            caname = runtimeIdentity.CaName,
        });

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

        // Response shape: { "success": true, "result": { "Cert": "<b64 pem>" } }
        var json = JsonConvert.DeserializeObject<JObject>(responseBody);
        var success = json?.Value<bool?>("success") == true;
        var certB64 = json?["result"]?["Cert"]?.Value<string>();

        if (!success || string.IsNullOrWhiteSpace(certB64))
        {
            throw new InvalidOperationException($"Fabric CA enroll returned failure: {responseBody}");
        }

        // CA returns base64-encoded PEM cert
        return Encoding.UTF8.GetString(Convert.FromBase64String(certB64));
    }

    // ========================================================================
    // Fabric CA token auth: base64(certDER) + "." + base64(ECDSA-sign(payload))
    //
    // payload = method + "." + base64(caUrl+path) + "." + base64(body)
    // Matches fabric-ca-client lib/client.go GenTokenAuthority
    // ========================================================================

    private string BuildAuthToken(FabricRuntimeIdentity runtimeIdentity, string method, string urlPath, byte[] bodyBytes)
    {
        if (_adminCertDer == null || _adminKey == null)
            throw new InvalidOperationException("Admin crypto material not loaded.");

        var b64body = Convert.ToBase64String(bodyBytes);
        var b64url = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{runtimeIdentity.CaUrl.TrimEnd('/')}{urlPath}"));

        var sigPayload = $"{method}.{b64url}.{b64body}";
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(sigPayload));

        // DER-encoded ECDSA signature – matches Go's crypto/ecdsa output
        var sig = _adminKey.SignHash(digest, DSASignatureFormat.Rfc3279DerSequence);

        return $"{Convert.ToBase64String(_adminCertDer)}.{Convert.ToBase64String(sig)}";
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private async Task<bool> EnsureAdminLoadedAsync(FabricRuntimeIdentity runtimeIdentity)
    {
        if (_adminLoaded && _adminIdentityKey == runtimeIdentity.IdentityKey) return true;

        _adminKey?.Dispose();
        _adminKey = null;
        _adminCertDer = null;
        _adminLoaded = false;
        _adminIdentityKey = runtimeIdentity.IdentityKey;

        // Load admin private key
        if (!string.IsNullOrEmpty(runtimeIdentity.AdminKeyPath) && File.Exists(runtimeIdentity.AdminKeyPath))
        {
            var pem = await File.ReadAllTextAsync(runtimeIdentity.AdminKeyPath);
            _adminKey = ECDsa.Create();
            _adminKey.ImportFromPem(pem);
        }
        else if (!string.IsNullOrEmpty(runtimeIdentity.AdminKeyDirectory) &&
                 Directory.Exists(runtimeIdentity.AdminKeyDirectory))
        {
            var files = Directory.GetFiles(runtimeIdentity.AdminKeyDirectory, "*_sk");
            if (files.Length == 0)
                files = Directory.GetFiles(runtimeIdentity.AdminKeyDirectory, "*.pem");
            if (files.Length > 0)
            {
                var pem = await File.ReadAllTextAsync(files[0]);
                _adminKey = ECDsa.Create();
                _adminKey.ImportFromPem(pem);
            }
        }

        // Load admin certificate (DER bytes extracted from PEM)
        if (!string.IsNullOrEmpty(runtimeIdentity.AdminCertPath) && File.Exists(runtimeIdentity.AdminCertPath))
        {
            var certPem = await File.ReadAllTextAsync(runtimeIdentity.AdminCertPath);
            _adminCertDer = ExtractDerFromPem(certPem, "CERTIFICATE");
        }

        if (_adminKey == null || _adminCertDer == null)
        {
            _logger.LogWarning(
                "Fabric CA admin crypto material not found for {IdentityKey}; CA enrollment will be skipped.",
                runtimeIdentity.IdentityKey);

            return false;
        }

        _adminLoaded = true;
        _logger.LogInformation("Fabric CA admin identity loaded from {CertPath}", runtimeIdentity.AdminCertPath);
        return true;
    }

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

    private string BuildAccountStoragePath(string enrollmentId)
    {
        var domain = _options.CaName switch
        {
            "ca-hospital1" => "hospital1.ehr.com",
            "ca-hospital2" => "hospital2.ehr.com",
            "ca-clinic" => "clinic.ehr.com",
            _ => "hospital1.ehr.com"
        };

        return $"/tmp/fabric-crypto/peerOrganizations/{domain}/users/{enrollmentId}@{domain}/msp";
    }

    private static byte[] ExtractDerFromPem(string pem, string label)
    {
        var header = $"-----BEGIN {label}-----";
        var footer = $"-----END {label}-----";
        var start = pem.IndexOf(header, StringComparison.Ordinal);
        if (start < 0) throw new InvalidOperationException($"PEM marker '{header}' not found.");
        start += header.Length;
        var end = pem.IndexOf(footer, start, StringComparison.Ordinal);
        if (end < 0) throw new InvalidOperationException($"PEM marker '{footer}' not found.");
        var b64 = pem[start..end].Replace("\n", "").Replace("\r", "").Trim();
        return Convert.FromBase64String(b64);
    }

    private static string GenerateSecret()
    {
        // 16-char random alphanumeric secret that Fabric CA accepts as a password
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
    }
}
