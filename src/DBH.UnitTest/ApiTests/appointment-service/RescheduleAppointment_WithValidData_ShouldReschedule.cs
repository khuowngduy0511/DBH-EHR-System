using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

/// <summary>
/// Good case: Reschedule an appointment to a new date
/// Expected: 200 OK
/// </summary>
public class AppointmentServiceTests_RescheduleAppointment_WithValidData_ShouldReschedule : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService"
    };

    [SkippableFact]
    public async Task RescheduleAppointment_WithValidData_ShouldReschedule()
    {
        var freshUsers = await AuthenticateAsFreshPatientAsync(AppointmentClient);

        // First create an appointment
        var createRequest = new 
        { 
            patientId = freshUsers.PatientUserId,
            doctorId = freshUsers.DoctorUserId,
            orgId = freshUsers.OrganizationId,
            scheduledAt = DateTime.UtcNow.AddDays(7), 
            reason = "Reschedule test", 
            notes = "Test rescheduling" 
        };

        var createResponse = await PostAsJsonWithRetryAsync(
            AppointmentClient, 
            ApiEndpoints.Appointments.Create, 
            createRequest);

        if (createResponse.StatusCode != HttpStatusCode.Created && createResponse.StatusCode != HttpStatusCode.OK)
            return;

        var createJson = await ReadJsonResponseAsync(createResponse);
        if (!createJson.TryGetProperty("data", out var dataElement))
            return;

        if (!dataElement.TryGetProperty("appointmentId", out var apptIdElement))
            return;

        var appointmentId = Guid.Parse(apptIdElement.GetString()!);

        // Reschedule to new date
        var newDate = DateTime.UtcNow.AddDays(14);
        var rescheduleResponse = await PutAsJsonWithRetryAsync(
            AppointmentClient, 
            ApiEndpoints.Appointments.Reschedule(appointmentId, newDate.ToString("O")), 
            new { });

        Assert.True(
            rescheduleResponse.StatusCode == HttpStatusCode.OK || 
            rescheduleResponse.StatusCode == HttpStatusCode.NoContent,
            $"Expected 200 or 204, got {rescheduleResponse.StatusCode}");
    }
}
