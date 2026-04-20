namespace DBH.UnitTest.UnitTests;

public class DownloadIpfsRaw_EmptyCid_ReturnsNull
{
    [Fact]
    public async Task DownloadIpfsRaw_EmptyCid_ReturnsNull_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService());

        var payload = await sut.DownloadIpfsRawAsync(string.Empty);

        Assert.Null(payload);
    }
}