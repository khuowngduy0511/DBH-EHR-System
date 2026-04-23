using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_CreateAppointment_WithInvalidPatient_ShouldReturnBadRequest : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService",
        "OrganizationService"
    };

    [SkippableFact]
    public async Task CreateAppointment_WithInvalidPatient_ShouldReturnBadRequest()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var users = await CreateFreshDoctorAndPatientAsync();

        var request = new
        {
            patientId = Guid.NewGuid(),
            doctorId = users.DoctorUserId,
            orgId = users.OrganizationId,
            scheduledAt = DateTime.UtcNow.AddDays(3).ToString("o")
        };

        var response = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}