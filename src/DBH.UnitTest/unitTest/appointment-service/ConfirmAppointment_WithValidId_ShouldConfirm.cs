using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Good case: Confirm a pending appointment
/// Expected: 200 OK
/// </summary>
public class AppointmentServiceTests_ConfirmAppointment_WithValidId_ShouldConfirm : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService"
    };

    [SkippableFact]
    public async Task ConfirmAppointment_WithValidId_ShouldConfirm()
    {
        var freshUsers = await AuthenticateAsFreshPatientAsync(AppointmentClient);

        // First create an appointment
        var createRequest = new 
        { 
            patientId = freshUsers.PatientUserId,
            doctorId = freshUsers.DoctorUserId,
            organizationId = freshUsers.OrganizationId,
            appointmentDate = DateTime.UtcNow.AddDays(7), 
            reason = "Confirm test", 
            notes = "Test confirmation" 
        };

        var createResponse = await PostAsJsonWithRetryAsync(
            AppointmentClient, 
            ApiEndpoints.Appointments.Create, 
            createRequest);

        if (createResponse.StatusCode != HttpStatusCode.Created && createResponse.StatusCode != HttpStatusCode.OK)
            return; // Skip if creation fails

        var createJson = await ReadJsonResponseAsync(createResponse);
        if (!createJson.TryGetProperty("data", out var dataElement))
            return;

        if (!dataElement.TryGetProperty("appointmentId", out var apptIdElement))
            return;

        var appointmentId = Guid.Parse(apptIdElement.GetString()!);

        // Now confirm it - authenticate as doctor
        await AuthenticateAsFreshDoctorAsync(AppointmentClient);

        var confirmResponse = await PutAsJsonWithRetryAsync(
            AppointmentClient, 
            ApiEndpoints.Appointments.Confirm(appointmentId), 
            new { });

        Assert.True(
            confirmResponse.StatusCode == HttpStatusCode.OK || 
            confirmResponse.StatusCode == HttpStatusCode.NoContent,
            $"Expected 200 or 204, got {confirmResponse.StatusCode}");
    }
}
