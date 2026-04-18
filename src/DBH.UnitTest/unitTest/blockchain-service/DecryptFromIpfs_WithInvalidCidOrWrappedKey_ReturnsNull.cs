using DBH.EHR.Service.Models.DTOs;

namespace DBH.UnitTest.UnitTests;

public class DecryptFromIpfs_WithInvalidCidOrWrappedKey_ReturnsNull
{
    [Fact]
    public async Task DecryptFromIpfs_WithInvalidCidOrWrappedKey_ReturnsNull_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService(), includeUserIdentity: true, authMode: EhrServiceBlockChainIpfsTestSupport.AuthResponseMode.ValidKeys);

        var decrypted = await sut.DecryptIpfsForCurrentUserAsync(new DecryptIpfsPayloadRequestDto
        {
            IpfsCid = "QmThisCidShouldNotExistInUnitTest",
            WrappedAesKey = "invalid-wrapped-key"
        });

        Assert.Null(decrypted);
    }
}