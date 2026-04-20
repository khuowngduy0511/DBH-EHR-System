using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_GetAppointment_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AppointmentService"
    };

    [SkippableFact]
    public async Task GetAppointment_WithFakeId_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(AppointmentClient);
    var response = await GetWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.GetById(Guid.NewGuid()));
    
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.False(json.GetProperty("success").GetBoolean());
    Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }
}
