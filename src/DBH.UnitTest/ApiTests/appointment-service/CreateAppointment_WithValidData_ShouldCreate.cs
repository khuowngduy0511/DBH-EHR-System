using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

/// <summary>
/// Good case: Create appointment with valid data
/// Expected: 201 Created
/// </summary>
public class AppointmentServiceTests_CreateAppointment_WithValidData_ShouldCreate : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService"
    };

    [SkippableFact]
    public async Task CreateAppointment_WithValidData_ShouldCreate()
    {
        var freshUsers = await AuthenticateAsFreshPatientAsync(AppointmentClient);

        var request = new 
        { 
            patientId = freshUsers.PatientUserId,
            doctorId = freshUsers.DoctorUserId,
            orgId = freshUsers.OrganizationId,
            scheduledAt = DateTime.UtcNow.AddDays(7).ToString("o")
        };

        var response = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, request);

        Assert.True(
            response.StatusCode == HttpStatusCode.Created || 
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 201 or 200, got {response.StatusCode}");
    }
}
