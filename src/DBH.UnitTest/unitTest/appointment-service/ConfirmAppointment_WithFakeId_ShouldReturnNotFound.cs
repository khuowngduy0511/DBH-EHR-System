using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Bad case: Try to confirm non-existent appointment
/// Expected: 404 Not Found or 400 Bad Request
/// </summary>
public class AppointmentServiceTests_ConfirmAppointment_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService"
    };

    [SkippableFact]
    public async Task ConfirmAppointment_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsDoctorAsync(AppointmentClient);

        var fakeAppointmentId = Guid.NewGuid();
        
        var response = await PutAsJsonWithRetryAsync(
            AppointmentClient, 
            ApiEndpoints.Appointments.Confirm(fakeAppointmentId), 
            new { });

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 404 or 400, got {response.StatusCode}");
    }
}
