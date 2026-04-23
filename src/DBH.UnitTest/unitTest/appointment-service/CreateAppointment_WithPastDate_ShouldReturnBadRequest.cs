using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_CreateAppointment_WithPastDate_ShouldReturnBadRequest : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService",
        "OrganizationService"
    };

    [SkippableFact]
    public async Task CreateAppointment_WithPastDate_ShouldReturnBadRequest()
    {
        var users = await AuthenticateAsFreshPatientAsync(AppointmentClient);

        var request = new
        {
            patientId = users.PatientUserId,
            doctorId = users.DoctorUserId,
            orgId = users.OrganizationId,
            scheduledAt = DateTime.UtcNow.AddHours(-2).ToString("o")
        };

        var response = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}