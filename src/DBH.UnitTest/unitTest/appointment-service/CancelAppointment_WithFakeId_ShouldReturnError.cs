using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_CancelAppointment_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AppointmentService"
    };

    [SkippableFact]
    public async Task CancelAppointment_WithFakeId_ShouldReturnError()
    {
    await AuthenticateAsAdminAsync(AppointmentClient);
    var request = new { reason = "Patient requested" };
    var response = await PutAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Cancel(Guid.NewGuid()), request);
    
    Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
