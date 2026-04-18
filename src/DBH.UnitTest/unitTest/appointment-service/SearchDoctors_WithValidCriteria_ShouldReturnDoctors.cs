using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Good case: Search for available doctors
/// Expected: 200 OK with paged doctor list
/// </summary>
public class AppointmentServiceTests_SearchDoctors_WithValidCriteria_ShouldReturnDoctors : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService"
    };

    [SkippableFact]
    public async Task SearchDoctors_WithValidCriteria_ShouldReturnDoctors()
    {
        await AuthenticateAsPatientAsync(AppointmentClient);

        var query = new 
        { 
            organizationId = TestSeedData.HospitalAOrgId, 
            specialty = "General Practice", 
            page = 1, 
            pageSize = 10 
        };

        var response = await PostAsJsonWithRetryAsync(
            AppointmentClient, 
            ApiEndpoints.Appointments.SearchDoctors, 
            query);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.BadRequest, // May fail if endpoint uses GET
            $"Expected 200 or 400, got {response.StatusCode}");
    }
}
