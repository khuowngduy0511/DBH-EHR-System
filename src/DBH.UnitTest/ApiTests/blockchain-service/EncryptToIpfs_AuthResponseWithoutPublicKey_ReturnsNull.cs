using DBH.EHR.Service.Models.DTOs;

namespace DBH.UnitTest.ApiTests;

public class EncryptToIpfs_AuthResponseWithoutPublicKey_ReturnsNull
{
    [Fact]
    public async Task EncryptToIpfs_AuthResponseWithoutPublicKey_ReturnsNull_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService(), includeUserIdentity: true, authMode: EhrServiceBlockChainIpfsTestSupport.AuthResponseMode.MissingPublicKey);

        var encrypted = await sut.EncryptToIpfsForCurrentUserAsync(new EncryptIpfsPayloadRequestDto
        {
            Data = "{\"hello\":\"world\"}"
        });

        Assert.Null(encrypted);
    }
}
