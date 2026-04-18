using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using DBH.Shared.Infrastructure.Blockchain;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace DBH.UnitTest.UnitTests;

public class FabricRuntimeIdentityResolverTests
{
    [Fact]
    public async Task ResolveForCurrentContextAsync_NoHttpContext_ReturnsFallbackIdentity()
    {
        var resolver = CreateResolver(
            fabricOptions: new FabricOptions
            {
                MspId = "Hospital1MSP",
                PeerEndpoint = "peer0.hospital1.ehr.com:7051",
                GatewayPeerOverride = "peer0.hospital1.ehr.com",
                CertificatePath = "/cert.pem",
                PrivateKeyDirectory = "/keystore",
                TlsCertificatePath = "/tls-ca.pem"
            },
            caOptions: new FabricCaOptions
            {
                CaUrl = "https://ca_hospital1:7054",
                CaName = "ca-hospital1",
                DefaultAffiliation = "org1.department1",
                AdminCertPath = "/admin-cert.pem",
                AdminKeyDirectory = "/admin-keystore",
                TlsCertPath = "/ca-tls.pem"
            });

        var identity = await resolver.ResolveForCurrentContextAsync();

        Assert.StartsWith("fallback|", identity.IdentityKey);
        Assert.Equal("Hospital1MSP", identity.MspId);
        Assert.Equal("peer0.hospital1.ehr.com:7051", identity.PeerEndpoint);
        Assert.Equal("https://ca_hospital1:7054", identity.CaUrl);
        Assert.Equal("ca-hospital1", identity.CaName);
        Assert.Equal("/admin-cert.pem", identity.AdminCertPath);
    }

    [Fact]
    public async Task ResolveForCurrentContextAsync_ValidOrgClaim_UsesOrganizationMetadata()
    {
        HttpRequestMessage? capturedRequest = null;
        var orgId = Guid.NewGuid();

        var resolver = CreateResolver(
            fabricOptions: new FabricOptions
            {
                MspId = "Hospital1MSP",
                PeerEndpoint = "peer0.hospital1.ehr.com:7051",
                UseTls = true
            },
            caOptions: new FabricCaOptions
            {
                DefaultAffiliation = "hospital.department1"
            },
            httpContext: CreateHttpContext(orgId, "jwt-token-abc"),
            organizationServiceResponseFactory: request =>
            {
                capturedRequest = request;
                var body = """
                    {
                      "data": {
                        "fabricMspId": "Hospital2MSP",
                        "fabricCaUrl": "https://ca_hospital2:8054",
                        "fabricChannelPeers": ["peer0.hospital2.ehr.com"]
                      }
                    }
                    """;

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body)
                };
            });

        var identity = await resolver.ResolveForCurrentContextAsync();

        Assert.NotNull(capturedRequest);
        Assert.Contains($"/api/v1/organizations/{orgId}", capturedRequest!.RequestUri!.ToString());
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("jwt-token-abc", capturedRequest.Headers.Authorization?.Parameter);

        Assert.Equal("Hospital2MSP", identity.MspId);
        Assert.Equal("peer0.hospital2.ehr.com:9051", identity.PeerEndpoint);
        Assert.Equal("peer0.hospital2.ehr.com", identity.GatewayPeerOverride);
        Assert.Equal("https://ca_hospital2:8054", identity.CaUrl);
        Assert.Equal("ca-hospital2", identity.CaName);
        Assert.Equal("/tmp/fabric-crypto/peerOrganizations/hospital2.ehr.com/users/Admin@hospital2.ehr.com/msp/signcerts/cert.pem", identity.CertificatePath);
        Assert.Equal("/tmp/fabric-crypto/peerOrganizations/hospital2.ehr.com/users/Admin@hospital2.ehr.com/msp/keystore", identity.PrivateKeyDirectory);
        Assert.Equal("/tmp/fabric-crypto/fabric-ca/hospital2/ca-cert.pem", identity.TlsCaCertPath);
    }

    [Fact]
    public async Task ResolveForCurrentContextAsync_OrganizationServiceFails_ReturnsFallbackIdentity()
    {
        var orgId = Guid.NewGuid();

        var resolver = CreateResolver(
            fabricOptions: new FabricOptions
            {
                MspId = "Hospital1MSP",
                PeerEndpoint = "peer0.hospital1.ehr.com:7051"
            },
            caOptions: new FabricCaOptions
            {
                CaUrl = "https://ca_hospital1:7054",
                CaName = "ca-hospital1"
            },
            httpContext: CreateHttpContext(orgId),
            organizationServiceResponseFactory: _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var identity = await resolver.ResolveForCurrentContextAsync();

        Assert.StartsWith("fallback|", identity.IdentityKey);
        Assert.Equal("Hospital1MSP", identity.MspId);
        Assert.Equal("https://ca_hospital1:7054", identity.CaUrl);
    }

    [Fact]
    public async Task ResolveForCurrentContextAsync_StringArrayPeers_ParsesFirstPeerAndDefaultPort()
    {
        var orgId = Guid.NewGuid();

        var resolver = CreateResolver(
            fabricOptions: new FabricOptions { MspId = "Hospital1MSP", PeerEndpoint = "peer0.hospital1.ehr.com:7051" },
            caOptions: new FabricCaOptions { DefaultAffiliation = "org1.department1" },
            httpContext: CreateHttpContext(orgId),
            organizationServiceResponseFactory: _ =>
            {
                var body = """
                    {
                      "data": {
                        "fabricMspId": "ClinicMSP",
                        "fabricChannelPeers": "[\"peer0.clinic.ehr.com\"]"
                      }
                    }
                    """;

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body)
                };
            });

        var identity = await resolver.ResolveForCurrentContextAsync();

        Assert.Equal("ClinicMSP", identity.MspId);
        Assert.Equal("peer0.clinic.ehr.com:11051", identity.PeerEndpoint);
        Assert.Equal("https://ca_clinic:10054", identity.CaUrl);
        Assert.Equal("ca-clinic", identity.CaName);
    }

    private static FabricRuntimeIdentityResolver CreateResolver(
        FabricOptions fabricOptions,
        FabricCaOptions caOptions,
        HttpContext? httpContext = null,
        Func<HttpRequestMessage, HttpResponseMessage>? organizationServiceResponseFactory = null)
    {
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceUrls:OrganizationService"] = "http://organization_service:5002"
            })
            .Build();

        var responseFactory = organizationServiceResponseFactory ??
                              (_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var client = new HttpClient(new StubHttpMessageHandler(responseFactory));
        var clientFactory = new StubHttpClientFactory(client);

        return new FabricRuntimeIdentityResolver(
            Options.Create(fabricOptions),
            Options.Create(caOptions),
            accessor,
            clientFactory,
            configuration,
            NullLogger<FabricRuntimeIdentityResolver>.Instance);
    }

    private static HttpContext CreateHttpContext(Guid orgId, string? bearerToken = null)
    {
        var context = new DefaultHttpContext();

        var claims = new List<Claim>
        {
            new(ClaimTypes.GroupSid, orgId.ToString())
        };

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test-auth"));

        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            context.Request.Headers.Authorization = new StringValues($"Bearer {bearerToken}");
        }

        return context;
    }

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}