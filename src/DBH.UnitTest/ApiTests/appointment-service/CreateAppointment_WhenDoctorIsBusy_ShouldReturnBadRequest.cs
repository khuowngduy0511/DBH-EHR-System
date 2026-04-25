using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AppointmentServiceTests_CreateAppointment_WhenDoctorIsBusy_ShouldReturnBadRequest : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService",
        "OrganizationService"
    };

    [SkippableFact]
    public async Task CreateAppointment_WhenDoctorIsBusy_ShouldReturnBadRequest()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);

        var usersA = await CreateFreshDoctorAndPatientAsync();
        var usersB = await CreateFreshDoctorAndPatientAsync($"doctor-busy-{Guid.NewGuid():N}");
        var slot = DateTime.UtcNow.AddDays(4).ToString("o");

        var firstRequest = new
        {
            patientId = usersA.PatientUserId,
            doctorId = usersA.DoctorUserId,
            orgId = usersA.OrganizationId,
            scheduledAt = slot
        };

        var secondRequest = new
        {
            patientId = usersB.PatientUserId,
            doctorId = usersA.DoctorUserId,
            orgId = usersA.OrganizationId,
            scheduledAt = slot
        };

        var firstResponse = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, firstRequest);
        Assert.True(firstResponse.StatusCode == HttpStatusCode.Created || firstResponse.StatusCode == HttpStatusCode.OK);

        var secondResponse = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, secondRequest);
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }
}
