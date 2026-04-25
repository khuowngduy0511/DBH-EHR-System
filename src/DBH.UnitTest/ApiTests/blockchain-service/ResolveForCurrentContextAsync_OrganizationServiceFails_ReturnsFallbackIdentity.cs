using System.Net;
using DBH.Shared.Infrastructure.Blockchain;

namespace DBH.UnitTest.ApiTests;

public class ResolveForCurrentContextAsync_OrganizationServiceFails_ReturnsFallbackIdentity
{
    [Fact]
    public async Task ResolveForCurrentContextAsync_OrganizationServiceFails_ReturnsFallbackIdentity_Test()
    {
        var orgId = Guid.NewGuid();

        var resolver = FabricRuntimeIdentityResolverTestSupport.CreateResolver(
            fabricOptions: new FabricOptions { MspId = "Hospital1MSP", PeerEndpoint = "peer0.hospital1.ehr.com:7051" },
            caOptions: new FabricCaOptions { CaUrl = "https://ca_hospital1:7054", CaName = "ca-hospital1" },
            httpContext: FabricRuntimeIdentityResolverTestSupport.CreateHttpContext(orgId),
            organizationServiceResponseFactory: _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var identity = await resolver.ResolveForCurrentContextAsync();
        Assert.StartsWith("fallback|", identity.IdentityKey);
        Assert.Equal("Hospital1MSP", identity.MspId);
    }
}
