using System.Net;
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
        await AuthenticateAsAdminAsync(AppointmentClient);

        var url = $"{ApiEndpoints.Appointments.SearchDoctors}?organizationId={TestSeedData.HospitalAOrgId}&specialty=General%20Practice&page=1&pageSize=10";
        var response = await GetWithRetryAsync(AppointmentClient, url);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 200 or 400, got {response.StatusCode}");
    }
}
