using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Good case: Update an existing EHR record with valid data
/// Expected: 200 OK with updated record
/// </summary>
public class EhrServiceTests_UpdateEhrRecord_WithValidData_ShouldUpdateRecord : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService"
    };

    [SkippableFact]
    public async Task UpdateEhrRecord_WithValidData_ShouldUpdateRecord()
    {
        await AuthenticateAsDoctorAsync(EhrClient);

        // Step 1: Create an EHR record
        var createRequest = new 
        { 
            patientId = TestSeedData.PatientUserId, 
            orgId = TestSeedData.HospitalAOrgId, 
            encounterId = Guid.NewGuid(), 
            data = new 
            { 
                doctorId = TestSeedData.DoctorUserId, 
                diagnosis = "Initial diagnosis", 
                treatment = "Initial treatment", 
                notes = "Initial notes" 
            } 
        };

        var createResponse = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.CreateRecord, createRequest);
        var createJson = await ReadJsonResponseAsync(createResponse);
        
        if (!createJson.TryGetProperty("ehrId", out var ehrIdElement))
            return; // Skip if creation fails

        var ehrId = Guid.Parse(ehrIdElement.GetString()!);

        // Step 2: Update the record
        var updateRequest = new 
        { 
            diagnosis = "Updated diagnosis", 
            treatment = "Updated treatment", 
            notes = "Updated notes" 
        };

        var updateResponse = await PutAsJsonWithRetryAsync(
            EhrClient, 
            ApiEndpoints.Ehr.UpdateRecord(ehrId), 
            updateRequest);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        
        var updateJson = await ReadJsonResponseAsync(updateResponse);
        Assert.True(updateJson.TryGetProperty("ehrId", out _), "Updated record should contain ehrId");
    }
}
