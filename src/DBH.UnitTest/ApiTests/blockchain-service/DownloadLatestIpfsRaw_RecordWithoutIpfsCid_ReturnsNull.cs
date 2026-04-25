namespace DBH.UnitTest.ApiTests;

public class DownloadLatestIpfsRaw_RecordWithoutIpfsCid_ReturnsNull
{
    [Fact]
    public async Task DownloadLatestIpfsRaw_RecordWithoutIpfsCid_ReturnsNull_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService());
        var ehrId = Guid.NewGuid();

        repo.SeedRecord(ehrId, Guid.NewGuid(), Guid.NewGuid());
        repo.SeedVersion(ehrId, 1, null);

        var payload = await sut.DownloadLatestIpfsRawByEhrIdAsync(ehrId);

        Assert.Null(payload);
    }
}
