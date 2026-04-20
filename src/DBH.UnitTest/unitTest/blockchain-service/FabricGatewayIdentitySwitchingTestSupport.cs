using System.Reflection;
using DBH.Shared.Infrastructure.Blockchain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DBH.UnitTest.UnitTests;

internal static class FabricGatewayIdentitySwitchingTestSupport
{
    internal static FabricGatewayClient CreateGatewayClient(IFabricRuntimeIdentityResolver resolver)
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

    internal static FabricRuntimeIdentity CreateIdentity(string identityKey, string mspId, string peerEndpoint, string peerOverride)
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

    internal static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var value = field!.GetValue(target);
        Assert.NotNull(value);
        return (T)value!;
    }

    internal sealed class SequenceIdentityResolver(params FabricRuntimeIdentity[] identities) : IFabricRuntimeIdentityResolver
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