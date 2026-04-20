namespace DBH.UnitTest.UnitTests;

public class DownloadLatestIpfsRaw_NoRecord_ReturnsNull
{
    [Fact]
    public async Task DownloadLatestIpfsRaw_NoRecord_ReturnsNull_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService());

        var payload = await sut.DownloadLatestIpfsRawByEhrIdAsync(Guid.NewGuid());

        Assert.Null(payload);
    }
}