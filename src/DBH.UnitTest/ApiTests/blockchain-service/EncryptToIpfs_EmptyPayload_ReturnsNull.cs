using DBH.EHR.Service.Models.DTOs;

namespace DBH.UnitTest.ApiTests;

public class EncryptToIpfs_EmptyPayload_ReturnsNull
{
    [Fact]
    public async Task EncryptToIpfs_EmptyPayload_ReturnsNull_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService(), includeUserIdentity: true, authMode: EhrServiceBlockChainIpfsTestSupport.AuthResponseMode.ValidKeys);

        var encrypted = await sut.EncryptToIpfsForCurrentUserAsync(new EncryptIpfsPayloadRequestDto
        {
            Data = string.Empty
        });

        Assert.Null(encrypted);
    }
}
