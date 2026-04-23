using DBH.Shared.Infrastructure.Blockchain;

namespace DBH.UnitTest.UnitTests;

public class ResolveForCurrentContextAsync_NoHttpContext_ReturnsFallbackIdentity
{
    [Fact]
    public async Task ResolveForCurrentContextAsync_NoHttpContext_ReturnsFallbackIdentity_Test()
    {
        var resolver = FabricRuntimeIdentityResolverTestSupport.CreateResolver(
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
    }
}