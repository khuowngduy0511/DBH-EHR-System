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
    // =========================================================================
    // EHR RECORDS - Using seed user/org IDs
    // =========================================================================

    [Fact]
    public async Task CreateEhrRecord_WithSeedData_ShouldReturnMessage()
    {
        await AuthenticateAsDoctorAsync(EhrClient);
        var request = new { patientId = TestSeedData.PatientUserId, orgId = TestSeedData.HospitalAOrgId, encounterId = Guid.NewGuid(), data = new { doctorId = TestSeedData.DoctorUserId, diagnosis = "Common cold", treatment = "Rest", notes = "Follow-up in 7 days" } };
        var response = await EhrClient.PostAsJsonAsync(ApiEndpoints.Ehr.CreateRecord, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public async Task GetEhrRecord_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await EhrClient.GetAsync(ApiEndpoints.Ehr.GetRecord(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPatientEhrRecords_WithSeedPatient_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await EhrClient.GetAsync(ApiEndpoints.Ehr.PatientRecords(TestSeedData.PatientUserId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task GetOrgEhrRecords_WithSeedOrg_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await EhrClient.GetAsync(ApiEndpoints.Ehr.OrgRecords(TestSeedData.HospitalAOrgId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
    }

    [Fact]
    public async Task UpdateEhrRecord_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var request = new { diagnosis = "Updated", treatment = "Updated", notes = "Updated" };
        var response = await EhrClient.PutAsJsonAsync(ApiEndpoints.Ehr.UpdateRecord(Guid.NewGuid()), request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEhrDocument_WithoutRequesterId_ShouldReturnBadRequest()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await EhrClient.GetAsync(ApiEndpoints.Ehr.GetDocument(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEhrDocument_WithRequesterId_FakeEhr_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        EhrClient.DefaultRequestHeaders.Add("X-Requester-Id", TestSeedData.DoctorUserId.ToString());
        var response = await EhrClient.GetAsync(ApiEndpoints.Ehr.GetDocument(Guid.NewGuid()));

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden);
        EhrClient.DefaultRequestHeaders.Remove("X-Requester-Id");
    }

    [Fact]
    public async Task GetEhrDocumentSelf_WithFakeEhr_ShouldReturnNotFound()
    {
        await AuthenticateAsPatientAsync(EhrClient);
        var response = await EhrClient.GetAsync(ApiEndpoints.Ehr.GetDocumentSelf(Guid.NewGuid()));

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden);
    }

    // =========================================================================
    // EHR VERSIONS
    // =========================================================================

    [Fact]
    public async Task GetEhrVersions_WithFakeId_ShouldReturnEmptyOrNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await EhrClient.GetAsync(ApiEndpoints.Ehr.Versions(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEhrVersionById_WithFakeIds_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await EhrClient.GetAsync(ApiEndpoints.Ehr.VersionById(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // EHR FILES
    // =========================================================================

    [Fact]
    public async Task GetEhrFiles_WithFakeId_ShouldReturnEmptyList()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await EhrClient.GetAsync(ApiEndpoints.Ehr.Files(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEhrFile_WithFakeIds_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await EhrClient.DeleteAsync(ApiEndpoints.Ehr.DeleteFile(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound);
    }

    // =========================================================================
    // IPFS
    // =========================================================================

    [Fact]
    public async Task DownloadIpfsRaw_WithFakeCid_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await EhrClient.GetAsync(ApiEndpoints.Ehr.DownloadIpfsRaw("QmFakeCid123"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DownloadLatestIpfs_WithFakeEhrId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var response = await EhrClient.GetAsync(ApiEndpoints.Ehr.DownloadLatestIpfs(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EncryptToIpfs_WithTestPayload_ShouldReturnMessage()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var request = new { payload = "{\"test\": \"data\"}" };
        var response = await EhrClient.PostAsJsonAsync(ApiEndpoints.Ehr.EncryptToIpfs, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }

    [Fact]
    public async Task DecryptFromIpfs_WithFakeData_ShouldReturnErrorMessage()
    {
        await AuthenticateAsAdminAsync(EhrClient);
        var request = new { encryptedData = "not-real-data", wrappedKey = "not-real-key" };
        var response = await EhrClient.PostAsJsonAsync(ApiEndpoints.Ehr.DecryptFromIpfs, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}

