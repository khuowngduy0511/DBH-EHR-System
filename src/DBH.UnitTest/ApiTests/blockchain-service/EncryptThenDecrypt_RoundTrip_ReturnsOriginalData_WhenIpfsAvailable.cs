using DBH.EHR.Service.Models.DTOs;

namespace DBH.UnitTest.ApiTests;

public class EncryptThenDecrypt_RoundTrip_ReturnsOriginalData_WhenIpfsAvailable
{
    [SkippableFact]
    public async Task EncryptThenDecrypt_RoundTrip_ReturnsOriginalData_WhenIpfsAvailable_Test()
    {
        var repo = new EhrServiceBlockChainIpfsTestSupport.InMemoryEhrRecordRepository();
        var sync = new EhrServiceBlockChainIpfsTestSupport.RecordingBlockchainSyncService();
        var sut = EhrServiceBlockChainIpfsTestSupport.CreateService(repo, sync, new EhrServiceBlockChainIpfsTestSupport.DummyEhrBlockchainService(), includeUserIdentity: true, authMode: EhrServiceBlockChainIpfsTestSupport.AuthResponseMode.ValidKeys);
        var originalData = "{\"roundtrip\":true,\"value\":2026}";

        var encrypted = await sut.EncryptToIpfsForCurrentUserAsync(new EncryptIpfsPayloadRequestDto
        {
            Data = originalData
        });

        Skip.If(encrypted == null, "IPFS is not reachable in this environment (expected for unit-only runs).");

        var decrypted = await sut.DecryptIpfsForCurrentUserAsync(new DecryptIpfsPayloadRequestDto
        {
            IpfsCid = encrypted!.IpfsCid,
            WrappedAesKey = encrypted.WrappedAesKey
        });

        Skip.If(string.IsNullOrWhiteSpace(decrypted), "Decrypt depends on IPFS download; skipping because local IPFS returned no payload.");
        Assert.Equal(originalData, decrypted);
    }
}
