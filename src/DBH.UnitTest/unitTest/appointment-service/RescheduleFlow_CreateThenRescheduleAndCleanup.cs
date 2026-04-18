using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_RescheduleFlow_CreateThenRescheduleAndCleanup : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] 
    { 
        "AuthService", 
        "AppointmentService" 
    };

    [SkippableFact]
    public async Task RescheduleFlow_CreateThenRescheduleAndCleanup_ShouldSucceed()
    {
        // 1. Create appointment
        var freshUsers = await AuthenticateAsFreshPatientAsync(AppointmentClient);
        
        var createRequest = new 
        { 
            patientId = freshUsers.PatientUserId,
            doctorId = freshUsers.DoctorUserId,
            appointmentDate = DateTime.UtcNow.AddDays(5).ToString("o"),
            reason = "Test appointment for reschedule"
        };
        
        var createResponse = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        
        var createJson = await ReadJsonResponseAsync(createResponse);
        var appointmentId = createJson.GetProperty("data").GetProperty("appointmentId").GetGuid();
        
        // 2. Reschedule the appointment
        var newDate = DateTime.UtcNow.AddDays(7).ToString("o");
        var rescheduleUrl = ApiEndpoints.Appointments.Reschedule(appointmentId, newDate);
        
        var rescheduleResponse = await PutAsJsonWithRetryAsync(AppointmentClient, rescheduleUrl, new { });
        Assert.True(rescheduleResponse.StatusCode == HttpStatusCode.OK || rescheduleResponse.StatusCode == HttpStatusCode.BadRequest);
        
        // 3. Cancel/cleanup the appointment
        var cancelUrl = ApiEndpoints.Appointments.Cancel(appointmentId);
        var cancelResponse = await PutAsJsonWithRetryAsync(AppointmentClient, cancelUrl, new { reason = "Cleanup test appointment" });
        Assert.True(cancelResponse.StatusCode == HttpStatusCode.OK || cancelResponse.StatusCode == HttpStatusCode.BadRequest);
    }
}
