using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

/// <summary>
/// Bad case: Try to reschedule non-existent appointment
/// Expected: 404 Not Found or 400 Bad Request
/// </summary>
public class AppointmentServiceTests_RescheduleAppointment_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService"
    };

    [SkippableFact]
    public async Task RescheduleAppointment_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsPatientAsync(AppointmentClient);

        var fakeAppointmentId = Guid.NewGuid();
        var newDate = DateTime.UtcNow.AddDays(14);
        
        var response = await PutAsJsonWithRetryAsync(
            AppointmentClient, 
            ApiEndpoints.Appointments.Reschedule(fakeAppointmentId, newDate.ToString("O")), 
            new { });

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 404 or 400, got {response.StatusCode}");
    }
}
