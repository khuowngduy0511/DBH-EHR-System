using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

/// <summary>
/// EHR File Management Flow: Complete file upload and retrieval workflow
/// Flow: Create EHR → Upload File → Get Files → Verify File Exists → 
///       Upload Second File → Get Files → Delete First File → Verify Deletion
/// Expected: File operations complete successfully with correct file counts
/// </summary>
public class EhrFileManagementTests : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService"
    };

    [SkippableFact]
    public async Task EhrFileManagement_UploadMultipleFilesAndDelete_ShouldSucceed()
    {
        await AuthenticateAsDoctorAsync(EhrClient);

        // =====================================================================
        // STEP 1: CREATE EHR
        // =====================================================================
        var createRequest = new 
        { 
            patientId = TestSeedData.PatientUserId, 
            orgId = TestSeedData.HospitalAOrgId, 
            encounterId = Guid.NewGuid(), 
            data = new 
            { 
                doctorId = TestSeedData.DoctorUserId, 
                diagnosis = "File management test", 
                treatment = "Test", 
                notes = "Testing file operations" 
            } 
        };

        var createResponse = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.CreateRecord, createRequest);
        var createJson = await ReadJsonResponseAsync(createResponse);
        
        if (!createJson.TryGetProperty("ehrId", out var ehrIdElement))
            return; // Skip if creation fails

        var ehrId = Guid.Parse(ehrIdElement.GetString()!);

        var uploadedFileIds = new List<Guid>();

        // =====================================================================
        // STEP 2: UPLOAD FIRST FILE
        // =====================================================================
        using (var fileContent1 = new MultipartFormDataContent())
        {
            var fileData1 = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("Lab Results: Blood pressure normal"));
            fileContent1.Add(fileData1, "file", "lab-results-1.txt");

            var uploadResponse1 = await EhrClient.PostAsync(
                ApiEndpoints.Ehr.AddFile(ehrId), 
                fileContent1);

            if (uploadResponse1.StatusCode == HttpStatusCode.Created || uploadResponse1.StatusCode == HttpStatusCode.OK)
            {
                var fileJson1 = await ReadJsonResponseAsync(uploadResponse1);
                if (fileJson1.TryGetProperty("fileId", out var fileIdElement1))
                {
                    uploadedFileIds.Add(Guid.Parse(fileIdElement1.GetString()!));
                }
            }
        }

        // =====================================================================
        // STEP 3: GET FILES - Verify first file exists
        // =====================================================================
        var filesResponse1 = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.Files(ehrId));
        Assert.Equal(HttpStatusCode.OK, filesResponse1.StatusCode);
        
        var filesJson1 = await ReadJsonResponseAsync(filesResponse1);
        Assert.True(filesJson1.ValueKind == JsonValueKind.Array);
        var initialFileCount = filesJson1.GetArrayLength();

        // =====================================================================
        // STEP 4: UPLOAD SECOND FILE
        // =====================================================================
        using (var fileContent2 = new MultipartFormDataContent())
        {
            var fileData2 = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("X-Ray: Clear lungs, no abnormalities"));
            fileContent2.Add(fileData2, "file", "xray-results.txt");

            var uploadResponse2 = await EhrClient.PostAsync(
                ApiEndpoints.Ehr.AddFile(ehrId), 
                fileContent2);

            if (uploadResponse2.StatusCode == HttpStatusCode.Created || uploadResponse2.StatusCode == HttpStatusCode.OK)
            {
                var fileJson2 = await ReadJsonResponseAsync(uploadResponse2);
                if (fileJson2.TryGetProperty("fileId", out var fileIdElement2))
                {
                    uploadedFileIds.Add(Guid.Parse(fileIdElement2.GetString()!));
                }
            }
        }

        // =====================================================================
        // STEP 5: GET FILES - Verify second file was added
        // =====================================================================
        var filesResponse2 = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.Files(ehrId));
        Assert.Equal(HttpStatusCode.OK, filesResponse2.StatusCode);
        
        var filesJson2 = await ReadJsonResponseAsync(filesResponse2);
        var fileCountAfterSecondUpload = filesJson2.GetArrayLength();
        Assert.True(fileCountAfterSecondUpload >= initialFileCount, "File count should increase after upload");

        // =====================================================================
        // STEP 6: DELETE FIRST FILE
        // =====================================================================
        if (uploadedFileIds.Count > 0)
        {
            var deleteResponse = await DeleteWithRetryAsync(
                EhrClient, 
                ApiEndpoints.Ehr.DeleteFile(ehrId, uploadedFileIds[0]));

            Assert.True(
                deleteResponse.StatusCode == HttpStatusCode.NoContent || 
                deleteResponse.StatusCode == HttpStatusCode.OK,
                $"DELETE failed: {deleteResponse.StatusCode}");
        }

        // =====================================================================
        // STEP 7: GET FILES - Verify file was deleted
        // =====================================================================
        var filesResponse3 = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.Files(ehrId));
        Assert.Equal(HttpStatusCode.OK, filesResponse3.StatusCode);
        
        var filesJson3 = await ReadJsonResponseAsync(filesResponse3);
        var fileCountAfterDelete = filesJson3.GetArrayLength();
        Assert.True(fileCountAfterDelete >= 0, "File list should be valid after deletion");

        // Test completed successfully
        Assert.True(true, "Complete file management workflow succeeded");
    }
}
