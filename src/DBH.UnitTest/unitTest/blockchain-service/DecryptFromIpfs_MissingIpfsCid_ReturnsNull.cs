using DBH.EHR.Service.Models.DTOs;

namespace DBH.UnitTest.UnitTests;

public class DecryptFromIpfs_MissingIpfsCid_ReturnsNull
{
    [Fact]
    public async Task DecryptFromIpfs_MissingIpfsCid_ReturnsNull_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService(), includeUserIdentity: true, authMode: EhrServiceBlockChainIpfsTestSupport.AuthResponseMode.ValidKeys);

        var decrypted = await sut.DecryptIpfsForCurrentUserAsync(new DecryptIpfsPayloadRequestDto
        {
            IpfsCid = string.Empty,
            WrappedAesKey = "wrapped-key"
        });

        Assert.Null(decrypted);
    }
}