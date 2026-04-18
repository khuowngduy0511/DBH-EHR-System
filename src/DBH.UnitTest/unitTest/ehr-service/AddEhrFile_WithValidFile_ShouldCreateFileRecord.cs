using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Good case: Upload a file to an existing EHR record
/// Expected: 201 Created with file DTO
/// </summary>
public class EhrServiceTests_AddEhrFile_WithValidFile_ShouldCreateFileRecord : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService"
    };

    [SkippableFact]
    public async Task AddEhrFile_WithValidFile_ShouldCreateFileRecord()
    {
        await AuthenticateAsDoctorAsync(EhrClient);

        // Step 1: Create an EHR record first
        var createRequest = new 
        { 
            patientId = TestSeedData.PatientUserId, 
            orgId = TestSeedData.HospitalAOrgId, 
            encounterId = Guid.NewGuid(), 
            data = new 
            { 
                doctorId = TestSeedData.DoctorUserId, 
                diagnosis = "Hypertension", 
                treatment = "Medication", 
                notes = "File upload test" 
            } 
        };

        var createResponse = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.CreateRecord, createRequest);
        var createJson = await ReadJsonResponseAsync(createResponse);
        
        if (!createJson.TryGetProperty("ehrId", out var ehrIdElement))
            throw new InvalidOperationException("EHR creation failed: no ehrId in response");

        var ehrId = Guid.Parse(ehrIdElement.GetString()!);

        // Step 2: Upload a file to the created EHR
        using var fileContent = new MultipartFormDataContent();
        var fileData = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
        fileContent.Add(fileData, "file", "test-file.txt");

        var uploadResponse = await EhrClient.PostAsync(
            ApiEndpoints.Ehr.AddFile(ehrId), 
            fileContent);

        // Verify response
        Assert.True(
            uploadResponse.StatusCode == HttpStatusCode.Created || 
            uploadResponse.StatusCode == HttpStatusCode.OK,
            $"Expected 201 or 200, got {uploadResponse.StatusCode}");

        var uploadJson = await ReadJsonResponseAsync(uploadResponse);
        Assert.True(uploadJson.TryGetProperty("fileId", out _), "Response should contain fileId");
    }
}
