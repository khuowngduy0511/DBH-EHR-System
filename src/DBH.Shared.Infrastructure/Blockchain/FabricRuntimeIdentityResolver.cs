using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace DBH.Shared.Infrastructure.Blockchain;

public sealed class FabricRuntimeIdentity
{
    public string IdentityKey { get; init; } = string.Empty;
    public string MspId { get; init; } = string.Empty;
    public string PeerEndpoint { get; init; } = string.Empty;
    public string? GatewayPeerOverride { get; init; }
    public bool UseTls { get; init; } = true;

    public string CaUrl { get; init; } = string.Empty;
    public string CaName { get; init; } = string.Empty;
    public string DefaultAffiliation { get; init; } = string.Empty;

    public string AdminCertPath { get; init; } = string.Empty;
    public string? AdminKeyPath { get; init; }
    public string? AdminKeyDirectory { get; init; }
    public string? TlsCaCertPath { get; init; }

    public string CertificatePath { get; init; } = string.Empty;
    public string? PrivateKeyPath { get; init; }
    public string? PrivateKeyDirectory { get; init; }
    public string? GatewayTlsCertificatePath { get; init; }
}

public interface IFabricRuntimeIdentityResolver
{
    Task<FabricRuntimeIdentity> ResolveForCurrentContextAsync(CancellationToken cancellationToken = default);
}

public sealed class FabricRuntimeIdentityResolver : IFabricRuntimeIdentityResolver
{
    private const string DefaultCryptoRoot = "/tmp/fabric-crypto";

    private readonly FabricOptions _fabricOptions;
    private readonly FabricCaOptions _fabricCaOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FabricRuntimeIdentityResolver> _logger;
    private readonly string _cryptoRoot;

    public FabricRuntimeIdentityResolver(
        IOptions<FabricOptions> fabricOptions,
        IOptions<FabricCaOptions> fabricCaOptions,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FabricRuntimeIdentityResolver> logger)
    {
        _fabricOptions = fabricOptions.Value;
        _fabricCaOptions = fabricCaOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _cryptoRoot = ResolveCryptoRoot();

        _logger.LogInformation("Fabric crypto root resolved to {CryptoRoot}", _cryptoRoot);
    }

    public async Task<FabricRuntimeIdentity> ResolveForCurrentContextAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var orgIdRaw = httpContext?.User?.FindFirstValue(ClaimTypes.GroupSid);

        if (string.IsNullOrWhiteSpace(orgIdRaw) || !Guid.TryParse(orgIdRaw, out var orgId))
        {
            return BuildFallbackIdentity();
        }

        try
        {
            var orgData = await GetOrganizationDataAsync(httpContext, orgId, cancellationToken);
            if (orgData == null)
            {
                _logger.LogWarning("Organization metadata not found for OrgId={OrgId}; falling back to static Fabric options", orgId);
                return BuildFallbackIdentity();
            }

            var mspId = orgData.Value<string>("fabricMspId") ?? _fabricOptions.MspId;
            var caUrl = orgData.Value<string>("fabricCaUrl") ?? DeriveCaUrlByMsp(mspId);
            var peerEndpoint = DerivePeerEndpoint(orgData["fabricChannelPeers"], mspId) ?? _fabricOptions.PeerEndpoint;
            var gatewayOverride = peerEndpoint.Split(':')[0];

            var orgDomain = DeriveOrgDomain(mspId);
            var orgAlias = DeriveOrgAlias(mspId);
            var caName = DeriveCaName(caUrl, mspId);

            var certPath = Path.Combine(_cryptoRoot, "peerOrganizations", orgDomain, "users", $"Admin@{orgDomain}", "msp", "signcerts", "cert.pem");
            var keyDir = Path.Combine(_cryptoRoot, "peerOrganizations", orgDomain, "users", $"Admin@{orgDomain}", "msp", "keystore");
            var tlsCert = Path.Combine(_cryptoRoot, "peerOrganizations", orgDomain, "peers", $"peer0.{orgDomain}", "tls", "ca.crt");
            var caTls = Path.Combine(_cryptoRoot, "fabric-ca", orgAlias, "ca-cert.pem");

            return new FabricRuntimeIdentity
            {
                IdentityKey = $"org:{orgId}|msp:{mspId}|peer:{peerEndpoint}|cert:{certPath}|keyDir:{keyDir}",
                MspId = mspId,
                PeerEndpoint = peerEndpoint,
                GatewayPeerOverride = gatewayOverride,
                UseTls = _fabricOptions.UseTls,
                CaUrl = caUrl,
                CaName = caName,
                DefaultAffiliation = _fabricCaOptions.DefaultAffiliation,
                AdminCertPath = certPath,
                AdminKeyPath = null,
                AdminKeyDirectory = keyDir,
                TlsCaCertPath = caTls,
                CertificatePath = certPath,
                PrivateKeyPath = null,
                PrivateKeyDirectory = keyDir,
                GatewayTlsCertificatePath = tlsCert
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed resolving org-specific Fabric identity. Falling back to static options");
            return BuildFallbackIdentity();
        }
    }

    private async Task<JObject?> GetOrganizationDataAsync(HttpContext? httpContext, Guid orgId, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["ServiceUrls:OrganizationService"] ?? "http://organization_service:5002";
        var client = _httpClientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl.TrimEnd('/')}/api/v1/organizations/{orgId}");

        var bearer = ExtractBearerToken(httpContext);
        if (!string.IsNullOrWhiteSpace(bearer))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer);
        }

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JObject.Parse(json);
        return parsed["data"] as JObject;
    }

    private FabricRuntimeIdentity BuildFallbackIdentity()
    {
        var peerHost = (_fabricOptions.GatewayPeerOverride ?? _fabricOptions.PeerEndpoint.Split(':')[0]);

        return new FabricRuntimeIdentity
        {
            IdentityKey = $"fallback|msp:{_fabricOptions.MspId}|peer:{_fabricOptions.PeerEndpoint}|cert:{_fabricOptions.CertificatePath}|key:{_fabricOptions.PrivateKeyPath}|dir:{_fabricOptions.PrivateKeyDirectory}",
            MspId = _fabricOptions.MspId,
            PeerEndpoint = _fabricOptions.PeerEndpoint,
            GatewayPeerOverride = peerHost,
            UseTls = _fabricOptions.UseTls,
            CaUrl = _fabricCaOptions.CaUrl,
            CaName = _fabricCaOptions.CaName,
            DefaultAffiliation = _fabricCaOptions.DefaultAffiliation,
            AdminCertPath = _fabricCaOptions.AdminCertPath,
            AdminKeyPath = _fabricCaOptions.AdminKeyPath,
            AdminKeyDirectory = _fabricCaOptions.AdminKeyDirectory,
            TlsCaCertPath = _fabricCaOptions.TlsCertPath,
            CertificatePath = _fabricOptions.CertificatePath,
            PrivateKeyPath = _fabricOptions.PrivateKeyPath,
            PrivateKeyDirectory = _fabricOptions.PrivateKeyDirectory,
            GatewayTlsCertificatePath = _fabricOptions.TlsCertificatePath
        };
    }

    private static string? ExtractBearerToken(HttpContext? httpContext)
    {
        var auth = httpContext?.Request?.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(auth)) return null;
        const string prefix = "Bearer ";
        return auth.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? auth[prefix.Length..].Trim()
            : null;
    }

    private static string DeriveOrgDomain(string mspId)
    {
        return mspId switch
        {
            "Hospital1MSP" => "hospital1.ehr.com",
            "Hospital2MSP" => "hospital2.ehr.com",
            "ClinicMSP" => "clinic.ehr.com",
            _ => "hospital1.ehr.com"
        };
    }

    private static string DeriveOrgAlias(string mspId)
    {
        return mspId switch
        {
            "Hospital1MSP" => "hospital1",
            "Hospital2MSP" => "hospital2",
            "ClinicMSP" => "clinic",
            _ => "hospital1"
        };
    }

    private static string DeriveCaUrlByMsp(string mspId)
    {
        return mspId switch
        {
            "Hospital1MSP" => "https://ca_hospital1:7054",
            "Hospital2MSP" => "https://ca_hospital2:8054",
            "ClinicMSP" => "https://ca_clinic:10054",
            _ => "https://ca_hospital1:7054"
        };
    }

    private static string DeriveCaName(string caUrl, string mspId)
    {
        try
        {
            var host = new Uri(caUrl).Host;
            if (host.StartsWith("ca_", StringComparison.OrdinalIgnoreCase))
            {
                return host.Replace("ca_", "ca-", StringComparison.OrdinalIgnoreCase);
            }

            if (host.StartsWith("ca-", StringComparison.OrdinalIgnoreCase))
            {
                return host;
            }
        }
        catch
        {
        }

        return mspId switch
        {
            "Hospital1MSP" => "ca-hospital1",
            "Hospital2MSP" => "ca-hospital2",
            "ClinicMSP" => "ca-clinic",
            _ => "ca-hospital1"
        };
    }

    private static string? DerivePeerEndpoint(JToken? fabricChannelPeers, string mspId)
    {
        if (fabricChannelPeers == null)
        {
            return DefaultPeerEndpointByMsp(mspId);
        }

        if (fabricChannelPeers.Type == JTokenType.Array)
        {
            var first = fabricChannelPeers.First?.Value<string>();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first.Contains(':') ? first : $"{first}:{DefaultPortByMsp(mspId)}";
            }
        }

        if (fabricChannelPeers.Type == JTokenType.String)
        {
            var raw = fabricChannelPeers.Value<string>();
            if (!string.IsNullOrWhiteSpace(raw))
            {
                if (raw.TrimStart().StartsWith("[", StringComparison.Ordinal))
                {
                    var arr = JArray.Parse(raw);
                    var first = arr.First?.Value<string>();
                    if (!string.IsNullOrWhiteSpace(first))
                    {
                        return first.Contains(':') ? first : $"{first}:{DefaultPortByMsp(mspId)}";
                    }
                }

                return raw.Contains(':') ? raw : $"{raw}:{DefaultPortByMsp(mspId)}";
            }
        }

        return DefaultPeerEndpointByMsp(mspId);
    }

    private static string DefaultPeerEndpointByMsp(string mspId)
    {
        return mspId switch
        {
            "Hospital1MSP" => "peer0.hospital1.ehr.com:7051",
            "Hospital2MSP" => "peer0.hospital2.ehr.com:9051",
            "ClinicMSP" => "peer0.clinic.ehr.com:11051",
            _ => "peer0.hospital1.ehr.com:7051"
        };
    }

    private static int DefaultPortByMsp(string mspId)
    {
        return mspId switch
        {
            "Hospital1MSP" => 7051,
            "Hospital2MSP" => 9051,
            "ClinicMSP" => 11051,
            _ => 7051
        };
    }

    private string ResolveCryptoRoot()
    {
        var configuredRoot = _fabricOptions.CryptoRoot ?? _configuration[$"{FabricOptions.SectionName}:CryptoRoot"];
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            return TrimTrailingSeparators(configuredRoot);
        }

        var roots = new[]
        {
            TryExtractCryptoRoot(_fabricOptions.CertificatePath),
            TryExtractCryptoRoot(_fabricOptions.PrivateKeyDirectory),
            TryExtractCryptoRoot(_fabricOptions.TlsCertificatePath),
            TryExtractCryptoRoot(_fabricCaOptions.AdminCertPath),
            TryExtractCryptoRoot(_fabricCaOptions.AdminKeyDirectory),
            TryExtractCryptoRoot(_fabricCaOptions.TlsCertPath)
        };

        var derived = roots.FirstOrDefault(static r => !string.IsNullOrWhiteSpace(r));
        return !string.IsNullOrWhiteSpace(derived)
            ? TrimTrailingSeparators(derived)
            : DefaultCryptoRoot;
    }

    private static string? TryExtractCryptoRoot(string? materialPath)
    {
        if (string.IsNullOrWhiteSpace(materialPath))
        {
            return null;
        }

        var normalized = materialPath.Replace('\\', '/');

        var peerOrgMarker = "/peerOrganizations/";
        var peerOrgIndex = normalized.IndexOf(peerOrgMarker, StringComparison.OrdinalIgnoreCase);
        if (peerOrgIndex > 0)
        {
            return normalized[..peerOrgIndex];
        }

        var fabricCaMarker = "/fabric-ca/";
        var fabricCaIndex = normalized.IndexOf(fabricCaMarker, StringComparison.OrdinalIgnoreCase);
        if (fabricCaIndex > 0)
        {
            return normalized[..fabricCaIndex];
        }

        return null;
    }

    private static string TrimTrailingSeparators(string path)
    {
        return path.TrimEnd('/', '\\');
    }
}
