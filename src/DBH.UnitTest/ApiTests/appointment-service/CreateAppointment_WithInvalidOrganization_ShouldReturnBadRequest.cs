using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AppointmentServiceTests_CreateAppointment_WithInvalidOrganization_ShouldReturnBadRequest : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService",
        "OrganizationService"
    };

    [SkippableFact]
    public async Task CreateAppointment_WithInvalidOrganization_ShouldReturnBadRequest()
    {
        var users = await AuthenticateAsFreshPatientAsync(AppointmentClient);

        var request = new
        {
            patientId = users.PatientUserId,
            doctorId = users.DoctorUserId,
            orgId = Guid.NewGuid(),
            scheduledAt = DateTime.UtcNow.AddDays(3).ToString("o")
        };

        var response = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
