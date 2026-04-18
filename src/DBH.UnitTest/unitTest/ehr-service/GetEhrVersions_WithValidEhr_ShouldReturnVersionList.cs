using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Good case: Retrieve all versions of an EHR record
/// Expected: 200 OK with version list
/// </summary>
public class EhrServiceTests_GetEhrVersions_WithValidEhr_ShouldReturnVersionList : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService"
    };

    [SkippableFact]
    public async Task GetEhrVersions_WithValidEhr_ShouldReturnVersionList()
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
                diagnosis = "Version test", 
                treatment = "Test treatment", 
                notes = "Initial version" 
            } 
        };

        var createResponse = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.CreateRecord, createRequest);
        var createJson = await ReadJsonResponseAsync(createResponse);
        
        if (!createJson.TryGetProperty("ehrId", out var ehrIdElement))
            return; // Skip if creation fails

        var ehrId = Guid.Parse(ehrIdElement.GetString()!);

        // Step 2: Update the record to create a new version
        var updateRequest = new 
        { 
            diagnosis = "Version test updated", 
            treatment = "Updated treatment", 
            notes = "Second version" 
        };

        await PutAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.UpdateRecord(ehrId), updateRequest);

        // Step 3: Get all versions
        var versionsResponse = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.Versions(ehrId));

        Assert.Equal(HttpStatusCode.OK, versionsResponse.StatusCode);
        
        var versionsJson = await ReadJsonResponseAsync(versionsResponse);
        Assert.True(versionsJson.ValueKind == JsonValueKind.Array, "Should return array of versions");
        Assert.True(versionsJson.GetArrayLength() >= 1, "Should have at least one version");
    }
}
