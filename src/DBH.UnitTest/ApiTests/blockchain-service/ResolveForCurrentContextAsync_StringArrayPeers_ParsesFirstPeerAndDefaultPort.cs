using System.Net;
using DBH.Shared.Infrastructure.Blockchain;

namespace DBH.UnitTest.ApiTests;

public class ResolveForCurrentContextAsync_StringArrayPeers_ParsesFirstPeerAndDefaultPort
{
    [Fact]
    public async Task ResolveForCurrentContextAsync_StringArrayPeers_ParsesFirstPeerAndDefaultPort_Test()
    {
        var orgId = Guid.NewGuid();

        var resolver = FabricRuntimeIdentityResolverTestSupport.CreateResolver(
            fabricOptions: new FabricOptions { MspId = "Hospital1MSP", PeerEndpoint = "peer0.hospital1.ehr.com:7051" },
            caOptions: new FabricCaOptions { DefaultAffiliation = "org1.department1" },
            httpContext: FabricRuntimeIdentityResolverTestSupport.CreateHttpContext(orgId),
            organizationServiceResponseFactory: _ =>
            {
                var body = """
                    {
                      \"data\": {
                        \"fabricMspId\": \"ClinicMSP\",
                        \"fabricChannelPeers\": \"[\\\"peer0.clinic.ehr.com\\\"]\"
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
    }
}
