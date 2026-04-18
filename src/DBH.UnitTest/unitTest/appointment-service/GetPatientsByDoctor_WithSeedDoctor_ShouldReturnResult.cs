using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_GetPatientsByDoctor_WithSeedDoctor_ShouldReturnResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AppointmentService"
    };

    [SkippableFact]
    public async Task GetPatientsByDoctor_WithSeedDoctor_ShouldReturnResult()
    {
    var freshUsers = await CreateFreshDoctorAndPatientAsync();
    await AuthenticateAsAdminAsync(AppointmentClient);
    var response = await GetWithRetryAsync(AppointmentClient, $"{ApiEndpoints.Appointments.PatientsByDoctor(freshUsers.DoctorUserId)}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.GetProperty("success").GetBoolean());
    }
}
