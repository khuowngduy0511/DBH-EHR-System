using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

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
        await AuthenticateAsPatientAsync(AppointmentClient);

        var request = new 
        { 
            patientId = TestSeedData.PatientUserId, 
            doctorId = TestSeedData.DoctorUserId, 
            organizationId = TestSeedData.HospitalAOrgId, 
            appointmentDate = DateTime.UtcNow.AddDays(7), 
            reason = "General checkup", 
            notes = "Patient has flu-like symptoms" 
        };

        var response = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, request);

        Assert.True(
            response.StatusCode == HttpStatusCode.Created || 
            response.StatusCode == HttpStatusCode.OK,
            $"Expected 201 or 200, got {response.StatusCode}");
    }
}
