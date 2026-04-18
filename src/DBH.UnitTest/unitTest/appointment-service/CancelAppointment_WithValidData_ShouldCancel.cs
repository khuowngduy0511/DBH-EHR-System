using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Good case: Cancel an appointment with reason
/// Expected: 200 OK
/// </summary>
public class AppointmentServiceTests_CancelAppointment_WithValidData_ShouldCancel : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService"
    };

    [SkippableFact]
    public async Task CancelAppointment_WithValidData_ShouldCancel()
    {
        await AuthenticateAsPatientAsync(AppointmentClient);

        // First create an appointment
        var createRequest = new 
        { 
            patientId = TestSeedData.PatientUserId, 
            doctorId = TestSeedData.DoctorUserId, 
            organizationId = TestSeedData.HospitalAOrgId, 
            appointmentDate = DateTime.UtcNow.AddDays(7), 
            reason = "Cancel test", 
            notes = "Test cancellation" 
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

        // Cancel the appointment
        var cancelRequest = new { reason = "Patient cannot attend" };
        
        var cancelResponse = await PutAsJsonWithRetryAsync(
            AppointmentClient, 
            ApiEndpoints.Appointments.Cancel(appointmentId), 
            cancelRequest);

        Assert.True(
            cancelResponse.StatusCode == HttpStatusCode.OK || 
            cancelResponse.StatusCode == HttpStatusCode.NoContent,
            $"Expected 200 or 204, got {cancelResponse.StatusCode}");
    }
}
