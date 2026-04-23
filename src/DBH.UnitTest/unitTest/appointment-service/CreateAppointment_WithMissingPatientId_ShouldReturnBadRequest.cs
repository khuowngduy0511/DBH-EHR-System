using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_CreateAppointment_WithMissingPatientId_ShouldReturnBadRequest : ApiTestBase
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
            orgId = freshUsers.OrganizationId,
            scheduledAt = DateTime.UtcNow.AddDays(7).ToString("o")
        };

        var response = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, request);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {response.StatusCode}");
    }
}