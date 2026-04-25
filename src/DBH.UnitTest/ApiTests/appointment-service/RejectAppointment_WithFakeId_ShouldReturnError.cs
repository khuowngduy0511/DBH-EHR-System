using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AppointmentServiceTests_RejectAppointment_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AppointmentService"
    };

    [SkippableFact]
    public async Task RejectAppointment_WithFakeId_ShouldReturnError()
    {
    await AuthenticateAsAdminAsync(AppointmentClient);
    var request = new { reason = "Schedule conflict" };
    var response = await PutAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Reject(Guid.NewGuid()), request);
    
    Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
