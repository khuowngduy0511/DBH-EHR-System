using DBH.Shared.Infrastructure.Ipfs;

namespace DBH.UnitTest.UnitTests;

public class DownloadLatestIpfsRaw_WithValidCid_ShouldReturnContent
{
    [SkippableFact]
    public async Task DownloadLatestIpfsRaw_WithValidCid_ShouldReturnContent_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService());

        var ehrId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var originalContent = $"{{\"downloadSuccess\":true,\"ehrId\":\"{ehrId}\"}}";

        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, originalContent);

        IpfsUploadResponse? uploadResult;
        try
        {
            uploadResult = await IpfsClientService.UploadAsync(tempFile);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Skip.If(true, "Local IPFS is not available, so download-success cannot be executed in this environment.");
            return;
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }

        Skip.If(uploadResult == null || string.IsNullOrWhiteSpace(uploadResult.Hash), "Local IPFS upload did not return a CID.");

        repo.SeedRecord(ehrId, patientId, orgId);
        repo.SeedVersion(ehrId, 1, uploadResult!.Hash);

        var result = await sut.DownloadLatestIpfsRawByEhrIdAsync(ehrId);

        Assert.NotNull(result);
        Assert.Equal(uploadResult.Hash, result!.IpfsCid);
        Assert.Equal(originalContent, result.EncryptedData);
    }
}