using System.Net;
using DBH.Shared.Infrastructure.Blockchain;

namespace DBH.UnitTest.ApiTests;

public class ResolveForCurrentContextAsync_ValidOrgClaim_UsesOrganizationMetadata
{
    [Fact]
    public async Task ResolveForCurrentContextAsync_ValidOrgClaim_UsesOrganizationMetadata_Test()
    {
        HttpRequestMessage? capturedRequest = null;
        var orgId = Guid.NewGuid();

        var resolver = FabricRuntimeIdentityResolverTestSupport.CreateResolver(
            fabricOptions: new FabricOptions { MspId = "Hospital1MSP", PeerEndpoint = "peer0.hospital1.ehr.com:7051", UseTls = true },
            caOptions: new FabricCaOptions { DefaultAffiliation = "hospital.department1" },
            httpContext: FabricRuntimeIdentityResolverTestSupport.CreateHttpContext(orgId, "jwt-token-abc"),
            organizationServiceResponseFactory: request =>
            {
                capturedRequest = request;
                var body = """
                    {
                      \"data\": {
                        \"fabricMspId\": \"Hospital2MSP\",
                        \"fabricCaUrl\": \"https://ca_hospital2:8054\",
                        \"fabricChannelPeers\": [\"peer0.hospital2.ehr.com\"]
                      }
                    }
                    """;
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
            });

        var identity = await resolver.ResolveForCurrentContextAsync();
        Assert.NotNull(capturedRequest);
        Assert.Equal("Hospital2MSP", identity.MspId);
    }
}
