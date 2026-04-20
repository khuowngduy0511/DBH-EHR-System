using System.Reflection;
using DBH.Shared.Infrastructure.Blockchain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DBH.UnitTest.UnitTests;

public class FabricGatewayIdentitySwitchingTests
{
    [Fact]
    public async Task EvaluateTransactionAsync_SameIdentity_KeepsActiveIdentity()
    {
        var resolver = new SequenceIdentityResolver(
            CreateIdentity("org-A", "Hospital1MSP", "peer0.hospital1.ehr.com:7051", "peer0.hospital1.ehr.com"),
            CreateIdentity("org-A", "Hospital1MSP", "peer0.hospital1.ehr.com:7051", "peer0.hospital1.ehr.com"));

        await using var sut = CreateGatewayClient(resolver);

        await sut.EvaluateTransactionAsync("ehr-channel", "ehr-chaincode", "GetEhr", "ehr-1");
        var firstIdentity = GetPrivateField<string>(sut, "_activeIdentityKey");

        await sut.EvaluateTransactionAsync("ehr-channel", "ehr-chaincode", "GetEhr", "ehr-2");
        var secondIdentity = GetPrivateField<string>(sut, "_activeIdentityKey");

        Assert.Equal("org-A", firstIdentity);
        Assert.Equal("org-A", secondIdentity);
    }

    [Fact]
    public async Task EvaluateTransactionAsync_DifferentIdentity_SwitchesActiveState()
    {
        var resolver = new SequenceIdentityResolver(
            CreateIdentity("org-A", "Hospital1MSP", "peer0.hospital1.ehr.com:7051", "peer0.hospital1.ehr.com"),
            CreateIdentity("org-B", "Hospital2MSP", "peer0.hospital2.ehr.com:9051", "peer0.hospital2.ehr.com"));

        await using var sut = CreateGatewayClient(resolver);

        await sut.EvaluateTransactionAsync("ehr-channel", "ehr-chaincode", "GetEhr", "ehr-1");
        var firstIdentity = GetPrivateField<string>(sut, "_activeIdentityKey");
        var firstMsp = GetPrivateField<string>(sut, "_activeMspId");
        var firstPeer = GetPrivateField<string>(sut, "_activePeerEndpoint");

        await sut.EvaluateTransactionAsync("ehr-channel", "ehr-chaincode", "GetEhr", "ehr-2");
        var secondIdentity = GetPrivateField<string>(sut, "_activeIdentityKey");
        var secondMsp = GetPrivateField<string>(sut, "_activeMspId");
        var secondPeer = GetPrivateField<string>(sut, "_activePeerEndpoint");

        Assert.Equal("org-A", firstIdentity);
        Assert.Equal("Hospital1MSP", firstMsp);
        Assert.Equal("peer0.hospital1.ehr.com:7051", firstPeer);

        Assert.Equal("org-B", secondIdentity);
        Assert.Equal("Hospital2MSP", secondMsp);
        Assert.Equal("peer0.hospital2.ehr.com:9051", secondPeer);
    }

    private static FabricGatewayClient CreateGatewayClient(IFabricRuntimeIdentityResolver resolver)
    {
        var options = Options.Create(new FabricOptions
        {
            Enabled = true,
            SimulationMode = true,
            MspId = "Hospital1MSP",
            PeerEndpoint = "peer0.hospital1.ehr.com:7051",
            GatewayPeerOverride = "peer0.hospital1.ehr.com",
            UseTls = true,
            MaxRetries = 0
        });

        return new FabricGatewayClient(options, resolver, NullLogger<FabricGatewayClient>.Instance);
    }

    private static FabricRuntimeIdentity CreateIdentity(string identityKey, string mspId, string peerEndpoint, string peerOverride)
    {
        return new FabricRuntimeIdentity
        {
            IdentityKey = identityKey,
            MspId = mspId,
            PeerEndpoint = peerEndpoint,
            GatewayPeerOverride = peerOverride,
            UseTls = true,
            CaUrl = "https://ca_hospital1:7054",
            CaName = "ca-hospital1",
            DefaultAffiliation = "org1.department1",
            AdminCertPath = "/tmp/admin-cert.pem",
            AdminKeyDirectory = "/tmp/keystore",
            TlsCaCertPath = "/tmp/ca-cert.pem",
            CertificatePath = "/tmp/cert.pem",
            PrivateKeyDirectory = "/tmp/keystore",
            GatewayTlsCertificatePath = "/tmp/tls-ca.pem"
        };
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var value = field!.GetValue(target);
        Assert.NotNull(value);
        return (T)value!;
    }

    private sealed class SequenceIdentityResolver(params FabricRuntimeIdentity[] identities) : IFabricRuntimeIdentityResolver
    {
        private readonly Queue<FabricRuntimeIdentity> _queue = new(identities);
        private FabricRuntimeIdentity? _last;

        public Task<FabricRuntimeIdentity> ResolveForCurrentContextAsync(CancellationToken cancellationToken = default)
        {
            if (_queue.Count > 0)
            {
                _last = _queue.Dequeue();
                return Task.FromResult(_last);
            }

            if (_last == null)
            {
                throw new InvalidOperationException("No runtime identity configured for test resolver.");
            }

            return Task.FromResult(_last);
        }
    }
}