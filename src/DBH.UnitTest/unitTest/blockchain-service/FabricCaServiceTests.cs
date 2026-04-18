using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.Blockchain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DBH.UnitTest.UnitTests;

public class FabricCaServiceTests
{
    [Fact]
    public async Task EnrollUserAsync_WhenBlockchainArtifactsAreMissing_ReturnsSuccessWithoutThrowing()
    {
        var service = new FabricCaService(
            Options.Create(new FabricCaOptions
            {
                Enabled = true,
                CaUrl = "https://ca_hospital1:7054",
                CaName = "ca-hospital1"
            }),
            new StubIdentityResolver(new FabricRuntimeIdentity
            {
                IdentityKey = "fallback|msp:Hospital1MSP",
                MspId = "Hospital1MSP",
                CaUrl = "https://ca_hospital1:7054",
                CaName = "ca-hospital1",
                AdminCertPath = "/missing/admin-cert.pem",
                AdminKeyDirectory = "/missing/keystore",
                TlsCaCertPath = "/missing/ca-cert.pem"
            }),
            NullLogger<FabricCaService>.Instance);

        var result = await service.EnrollUserAsync("user-1", "Alice", "doctor");

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.Certificate);
        Assert.Null(result.PrivateKeyPem);
    }

    private sealed class StubIdentityResolver(FabricRuntimeIdentity identity) : IFabricRuntimeIdentityResolver
    {
        public Task<FabricRuntimeIdentity> ResolveForCurrentContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(identity);
    }
}