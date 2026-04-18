using System.Text.Json;
using DBH.EHR.Service.Models.DTOs;

namespace DBH.UnitTest.UnitTests;

public class CreateEhrRecord_BlockchainEnabled_QueuesEhrHash
{
    [Fact]
    public async Task CreateEhrRecord_BlockchainEnabled_QueuesEhrHash_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService());

        using var payload = JsonDocument.Parse("{\"diagnosis\":\"check-enabled\"}");
        var result = await sut.CreateEhrRecordAsync(new CreateEhrRecordDto
        {
            PatientId = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            Data = payload.RootElement
        });

        Assert.NotEqual(Guid.Empty, result.EhrId);
        Assert.Equal(1, sync.EhrHashCount);
        Assert.NotNull(sync.LastEhrHash);
        Assert.Equal(result.EhrId.ToString(), sync.LastEhrHash!.EhrId);
    }
}