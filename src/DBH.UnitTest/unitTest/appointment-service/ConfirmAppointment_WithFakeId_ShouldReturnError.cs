using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_ConfirmAppointment_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AppointmentService"
    };

    [SkippableFact]
    public async Task ConfirmAppointment_WithFakeId_ShouldReturnError()
    {
    await AuthenticateAsAdminAsync(AppointmentClient);
    var response = await PutWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Confirm(Guid.NewGuid()), null);
    
    Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    var json = await ReadJsonResponseAsync(response);
    Assert.False(json.GetProperty("success").GetBoolean());
    }
}
