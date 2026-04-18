using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Bad case: Try to create EHR with missing required fields
/// Expected: 400 Bad Request
/// </summary>
public class EhrServiceTests_CreateEhrRecord_WithInvalidData_ShouldReturnBadRequest : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService"
    };

    [SkippableFact]
    public async Task CreateEhrRecord_WithMissingPatientId_ShouldReturnBadRequest()
    {
        await AuthenticateAsDoctorAsync(EhrClient);

        // Missing patientId
        var request = new 
        { 
            orgId = TestSeedData.HospitalAOrgId, 
            encounterId = Guid.NewGuid(), 
            data = new 
            { 
                diagnosis = "Test", 
                treatment = "Test", 
                notes = "Test" 
            } 
        };

        var response = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.CreateRecord, request);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {response.StatusCode}");
    }

    [SkippableFact]
    public async Task CreateEhrRecord_WithMissingOrgId_ShouldReturnBadRequest()
    {
        await AuthenticateAsDoctorAsync(EhrClient);

        // Missing orgId
        var request = new 
        { 
            patientId = TestSeedData.PatientUserId, 
            encounterId = Guid.NewGuid(), 
            data = new 
            { 
                diagnosis = "Test", 
                treatment = "Test", 
                notes = "Test" 
            } 
        };

        var response = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.CreateRecord, request);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {response.StatusCode}");
    }
}
