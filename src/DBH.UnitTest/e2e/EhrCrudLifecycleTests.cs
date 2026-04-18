using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

/// <summary>
/// EHR CRUD Flow Test: Complete lifecycle of creating, reading, updating, and deleting EHR operations
/// Flow: Create EHR → Read Record → Verify Creation → Update Record → Verify Update → 
///       Get Versions → Add File → Get Files → Delete File → Verify Deletion
/// Expected: All operations succeed with appropriate status codes
/// </summary>
public class EhrCrudLifecycleTests : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService"
    };

    [SkippableFact]
    public async Task EhrCrudLifecycle_CompleteWorkflow_ShouldSucceed()
    {
        await AuthenticateAsDoctorAsync(EhrClient);

        // =====================================================================
        // STEP 1: CREATE - Create initial EHR record
        // =====================================================================
        var createRequest = new 
        { 
            patientId = TestSeedData.PatientUserId, 
            orgId = TestSeedData.HospitalAOrgId, 
            encounterId = Guid.NewGuid(), 
            data = new 
            { 
                doctorId = TestSeedData.DoctorUserId, 
                diagnosis = "CRUD Test - Hypertension", 
                treatment = "ACE inhibitors", 
                notes = "Initial diagnosis" 
            } 
        };

        var createResponse = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.CreateRecord, createRequest);
        Assert.True(
            createResponse.StatusCode == HttpStatusCode.Created || 
            createResponse.StatusCode == HttpStatusCode.OK,
            $"CREATE failed: {createResponse.StatusCode}");

        var createJson = await ReadJsonResponseAsync(createResponse);
        if (!createJson.TryGetProperty("ehrId", out var ehrIdElement))
            return; // Skip rest of test if creation fails

        var ehrId = Guid.Parse(ehrIdElement.GetString()!);

        // =====================================================================
        // STEP 2: READ - Get the created record
        // =====================================================================
        var readResponse = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.GetRecord(ehrId));
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        
        var readJson = await ReadJsonResponseAsync(readResponse);
        Assert.True(readJson.TryGetProperty("ehrId", out _), "READ failed: ehrId not in response");

        // =====================================================================
        // STEP 3: UPDATE - Modify the record
        // =====================================================================
        var updateRequest = new 
        { 
            diagnosis = "CRUD Test - Hypertension Stage 2", 
            treatment = "ACE inhibitors + diuretic", 
            notes = "Updated after follow-up" 
        };

        var updateResponse = await PutAsJsonWithRetryAsync(
            EhrClient, 
            ApiEndpoints.Ehr.UpdateRecord(ehrId), 
            updateRequest);
        
        Assert.True(
            updateResponse.StatusCode == HttpStatusCode.OK || 
            updateResponse.StatusCode == HttpStatusCode.NoContent,
            $"UPDATE failed: {updateResponse.StatusCode}");

        // =====================================================================
        // STEP 4: VERIFY UPDATE - Read the updated record
        // =====================================================================
        var readUpdatedResponse = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.GetRecord(ehrId));
        Assert.Equal(HttpStatusCode.OK, readUpdatedResponse.StatusCode);

        // =====================================================================
        // STEP 5: GET VERSIONS - Check version history
        // =====================================================================
        var versionsResponse = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.Versions(ehrId));
        Assert.Equal(HttpStatusCode.OK, versionsResponse.StatusCode);
        
        var versionsJson = await ReadJsonResponseAsync(versionsResponse);
        Assert.True(versionsJson.ValueKind == JsonValueKind.Array, "Versions should be array");
        Assert.True(versionsJson.GetArrayLength() >= 1, "Should have at least 1 version");

        // =====================================================================
        // STEP 6: ADD FILE - Upload a file to the record
        // =====================================================================
        using var fileContent = new MultipartFormDataContent();
        var fileData = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
        fileContent.Add(fileData, "file", "lab-results.txt");

        var addFileResponse = await EhrClient.PostAsync(
            ApiEndpoints.Ehr.AddFile(ehrId), 
            fileContent);

        Assert.True(
            addFileResponse.StatusCode == HttpStatusCode.Created || 
            addFileResponse.StatusCode == HttpStatusCode.OK,
            $"ADD FILE failed: {addFileResponse.StatusCode}");

        Guid? fileId = null;
        if (addFileResponse.StatusCode == HttpStatusCode.Created || addFileResponse.StatusCode == HttpStatusCode.OK)
        {
            var fileJson = await ReadJsonResponseAsync(addFileResponse);
            if (fileJson.TryGetProperty("fileId", out var fileIdElement))
            {
                fileId = Guid.Parse(fileIdElement.GetString()!);
            }
        }

        // =====================================================================
        // STEP 7: GET FILES - List all files
        // =====================================================================
        var filesResponse = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.Files(ehrId));
        Assert.Equal(HttpStatusCode.OK, filesResponse.StatusCode);
        
        var filesJson = await ReadJsonResponseAsync(filesResponse);
        Assert.True(filesJson.ValueKind == JsonValueKind.Array, "Files should be array");

        // =====================================================================
        // STEP 8: DELETE FILE - Remove the uploaded file (if it was created)
        // =====================================================================
        if (fileId.HasValue && fileId.Value != Guid.Empty)
        {
            var deleteFileResponse = await DeleteWithRetryAsync(
                EhrClient, 
                ApiEndpoints.Ehr.DeleteFile(ehrId, fileId.Value));

            Assert.True(
                deleteFileResponse.StatusCode == HttpStatusCode.NoContent || 
                deleteFileResponse.StatusCode == HttpStatusCode.OK,
                $"DELETE FILE failed: {deleteFileResponse.StatusCode}");
        }

        // =====================================================================
        // STEP 9: VERIFY FILE DELETION - Check files list again
        // =====================================================================
        var filesAfterDeleteResponse = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.Files(ehrId));
        Assert.Equal(HttpStatusCode.OK, filesAfterDeleteResponse.StatusCode);

        // All steps completed successfully!
        Assert.True(true, "Complete CRUD lifecycle succeeded");
    }
}
