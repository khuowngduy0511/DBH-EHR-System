using System.Text.Json;
using DBH.EHR.Service.Models.DTOs;

namespace DBH.UnitTest.UnitTests;

public class CreateEhrRecord_BlockchainDisabled_DoesNotQueueEhrHash
{
    [Fact]
    public async Task CreateEhrRecord_BlockchainDisabled_DoesNotQueueEhrHash_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, blockchainService: null);

        using var payload = JsonDocument.Parse("{\"diagnosis\":\"check-disabled\"}");
        var result = await sut.CreateEhrRecordAsync(new CreateEhrRecordDto
        {
            PatientId = Guid.NewGuid(),
            OrgId = Guid.NewGuid(),
            Data = payload.RootElement
        });

        Assert.NotEqual(Guid.Empty, result.EhrId);
        Assert.Equal(0, sync.EhrHashCount);
    }
}