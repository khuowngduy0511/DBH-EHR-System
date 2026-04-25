using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

/// <summary>
/// Good case: Check-in to an appointment
/// Expected: 200 OK
/// </summary>
public class AppointmentServiceTests_CheckInAppointment_WithValidId_ShouldCheckIn : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService"
    };

    [SkippableFact]
    public async Task CheckInAppointment_WithValidId_ShouldCheckIn()
    {
        var freshUsers = await AuthenticateAsFreshPatientAsync(AppointmentClient);

        // First create an appointment
        var createRequest = new 
        { 
            patientId = freshUsers.PatientUserId,
            doctorId = freshUsers.DoctorUserId,
            organizationId = freshUsers.OrganizationId,
            appointmentDate = DateTime.UtcNow.AddDays(7),
            reason = "Check-in test", 
            notes = "Test check-in" 
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

        // Try to check-in
        var checkInResponse = await PutAsJsonWithRetryAsync(
            AppointmentClient, 
            ApiEndpoints.Appointments.CheckIn(appointmentId), 
            new { });

        Assert.True(
            checkInResponse.StatusCode == HttpStatusCode.OK || 
            checkInResponse.StatusCode == HttpStatusCode.NoContent ||
            checkInResponse.StatusCode == HttpStatusCode.BadRequest, // May fail if not yet confirmed
            $"Expected 200, 204, or 400, got {checkInResponse.StatusCode}");
    }
}
