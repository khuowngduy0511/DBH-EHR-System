using DBH.EHR.Service.Models.DTOs;

namespace DBH.UnitTest.ApiTests;

public class EncryptToIpfs_WithValidKeys_ReturnsPayload_WhenIpfsAvailable
{
    [SkippableFact]
    public async Task EncryptToIpfs_WithValidKeys_ReturnsPayload_WhenIpfsAvailable_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService(), includeUserIdentity: true, authMode: EhrServiceBlockChainIpfsTestSupport.AuthResponseMode.ValidKeys);

        var encrypted = await sut.EncryptToIpfsForCurrentUserAsync(new EncryptIpfsPayloadRequestDto
        {
            Data = "{\"message\":\"ipfs encrypt check\"}"
        });

        Skip.If(encrypted == null, "IPFS is not reachable in this environment (expected for unit-only runs).");

        Assert.False(string.IsNullOrWhiteSpace(encrypted!.IpfsCid));
        Assert.False(string.IsNullOrWhiteSpace(encrypted.WrappedAesKey));
        Assert.False(string.IsNullOrWhiteSpace(encrypted.DataHash));
    }
}
