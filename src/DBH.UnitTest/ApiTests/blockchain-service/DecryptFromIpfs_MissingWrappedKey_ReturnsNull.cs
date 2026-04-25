using DBH.EHR.Service.Models.DTOs;

namespace DBH.UnitTest.ApiTests;

public class DecryptFromIpfs_MissingWrappedKey_ReturnsNull
{
    [Fact]
    public async Task DecryptFromIpfs_MissingWrappedKey_ReturnsNull_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService(), includeUserIdentity: true, authMode: EhrServiceBlockChainIpfsTestSupport.AuthResponseMode.ValidKeys);

        var decrypted = await sut.DecryptIpfsForCurrentUserAsync(new DecryptIpfsPayloadRequestDto
        {
            IpfsCid = "QmAnyCid",
            WrappedAesKey = string.Empty
        });

        Assert.Null(decrypted);
    }
}
