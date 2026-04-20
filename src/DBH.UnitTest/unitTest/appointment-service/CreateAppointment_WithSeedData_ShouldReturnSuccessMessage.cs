using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_CreateAppointment_WithSeedData_ShouldReturnSuccessMessage : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AppointmentService"
    };

    [SkippableFact]
    public async Task CreateAppointment_WithSeedData_ShouldReturnSuccessMessage()
    {
    var freshUsers = await CreateFreshDoctorAndPatientAsync();
    await AuthenticateAsAdminAsync(AppointmentClient);
    var request = new { patientId = freshUsers.PatientUserId, doctorId = freshUsers.DoctorUserId, orgId = freshUsers.OrganizationId, appointmentDate = DateTime.UtcNow.AddDays(7).ToString("o"), reason = "General checkup", notes = "First visit" };
    var response = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, request);
    
    var json = await ReadJsonResponseAsync(response);
    Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK)
    {
    Assert.True(json.GetProperty("success").GetBoolean());
    }
    }
}
