using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AppointmentServiceTests_GetAppointments_AsAdmin_ShouldReturnPagedList : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AppointmentService"
    };

    [SkippableFact]
    public async Task GetAppointments_AsAdmin_ShouldReturnPagedList()
    {
    await AuthenticateAsAdminAsync(AppointmentClient);
    var response = await GetWithRetryAsync(AppointmentClient, $"{ApiEndpoints.Appointments.GetAll}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.GetProperty("success").GetBoolean());
    Assert.True(json.TryGetProperty("data", out _));
    }
}
