using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// API integration tests for DBH.EHR.Service
/// Covers: EhrController (Records, Versions, Files, IPFS)
/// </summary>
public class EhrServiceTests : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService"
    };

    // =========================================================================
    // EHR RECORDS - Using seed user/org IDs
    // =========================================================================

    [SkippableFact]
    public async Task CreateEhrRecord_WithSeedData_ShouldReturnMessage()
    {
        await AuthenticateAsDoctorAsync(EhrClient);
        var request = new { patientId = TestSeedData.PatientUserId, orgId = TestSeedData.HospitalAOrgId, encounterId = Guid.NewGuid(), data = new { doctorId = TestSeedData.DoctorUserId, diagnosis = "Common cold", treatment = "Rest", notes = "Follow-up in 7 days" } };
        var response = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.CreateRecord, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }

    [SkippableFact]
    public async Task GetEhrRecord_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.GetRecord(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetPatientEhrRecords_WithSeedPatient_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.PatientRecords(TestSeedData.PatientUserId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [SkippableFact]
    public async Task GetOrgEhrRecords_WithSeedOrg_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.OrgRecords(TestSeedData.HospitalAOrgId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [SkippableFact]
    public async Task UpdateEhrRecord_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var request = new { diagnosis = "Updated", treatment = "Updated", notes = "Updated" };
        var response = await PutAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.UpdateRecord(Guid.NewGuid()), request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetEhrDocument_WithoutRequesterId_ShouldReturnBadRequest()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.GetDocument(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetEhrDocument_WithRequesterId_FakeEhr_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        EhrClient.DefaultRequestHeaders.Add("X-Requester-Id", TestSeedData.DoctorUserId.ToString());
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.GetDocument(Guid.NewGuid()));

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden);
        EhrClient.DefaultRequestHeaders.Remove("X-Requester-Id");
    }

    [SkippableFact]
    public async Task GetEhrDocumentSelf_WithFakeEhr_ShouldReturnNotFound()
    {
        await AuthenticateAsPatientAsync(EhrClient);
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.GetDocumentSelf(Guid.NewGuid()));

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden);
    }

    // =========================================================================
    // EHR VERSIONS
    // =========================================================================

    [SkippableFact]
    public async Task GetEhrVersions_WithFakeId_ShouldReturnEmptyOrNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.Versions(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetEhrVersionById_WithFakeIds_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.VersionById(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // EHR FILES
    // =========================================================================

    [SkippableFact]
    public async Task GetEhrFiles_WithFakeId_ShouldReturnEmptyList()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.Files(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [SkippableFact]
    public async Task DeleteEhrFile_WithFakeIds_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await DeleteWithRetryAsync(EhrClient, ApiEndpoints.Ehr.DeleteFile(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound);
    }

    // =========================================================================
    // IPFS
    // =========================================================================

    [SkippableFact]
    public async Task DownloadIpfsRaw_WithFakeCid_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.DownloadIpfsRaw("QmFakeCid123"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task DownloadLatestIpfs_WithFakeEhrId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.DownloadLatestIpfs(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task EncryptToIpfs_WithTestPayload_ShouldReturnMessage()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var request = new { payload = "{\"test\": \"data\"}" };
        var response = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.EncryptToIpfs, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }

    [SkippableFact]
    public async Task DecryptFromIpfs_WithFakeData_ShouldReturnErrorMessage()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var request = new { encryptedData = "not-real-data", wrappedKey = "not-real-key" };
        var response = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.DecryptFromIpfs, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}

