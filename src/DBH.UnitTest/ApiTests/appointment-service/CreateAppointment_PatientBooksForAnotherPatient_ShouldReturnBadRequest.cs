using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AppointmentServiceTests_CreateAppointment_PatientBooksForAnotherPatient_ShouldReturnBadRequest : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService",
        "OrganizationService"
    };

    [SkippableFact]
    public async Task CreateAppointment_PatientBooksForAnotherPatient_ShouldReturnBadRequest()
    {
        var requester = await AuthenticateAsFreshPatientAsync(AppointmentClient);
        var otherUsers = await CreateFreshDoctorAndPatientAsync($"idor-{Guid.NewGuid():N}");

        var request = new
        {
            patientId = otherUsers.PatientUserId,
            doctorId = requester.DoctorUserId,
            orgId = requester.OrganizationId,
            scheduledAt = DateTime.UtcNow.AddDays(6).ToString("o")
        };

        var response = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
