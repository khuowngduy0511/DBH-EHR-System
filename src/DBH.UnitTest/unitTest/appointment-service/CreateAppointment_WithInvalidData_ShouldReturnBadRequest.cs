using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Bad case: Try to create appointment with invalid data (missing required fields)
/// Expected: 400 Bad Request
/// </summary>
public class AppointmentServiceTests_CreateAppointment_WithInvalidData_ShouldReturnBadRequest : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService"
    };

    [SkippableFact]
    public async Task CreateAppointment_WithMissingPatientId_ShouldReturnBadRequest()
    {
        var freshUsers = await AuthenticateAsFreshPatientAsync(AppointmentClient);

        var request = new 
        { 
            doctorId = freshUsers.DoctorUserId,
            organizationId = freshUsers.OrganizationId,
            appointmentDate = DateTime.UtcNow.AddDays(7), 
            reason = "General checkup" 
        };

        var response = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, request);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {response.StatusCode}");
    }

    [SkippableFact]
    public async Task CreateAppointment_WithPastDate_ShouldReturnBadRequest()
    {
        var freshUsers = await AuthenticateAsFreshPatientAsync(AppointmentClient);

        var request = new 
        { 
            patientId = freshUsers.PatientUserId,
            doctorId = freshUsers.DoctorUserId,
            organizationId = freshUsers.OrganizationId,
            appointmentDate = DateTime.UtcNow.AddDays(-1), // Past date
            reason = "General checkup" 
        };

        var response = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, request);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {response.StatusCode}");
    }
}
