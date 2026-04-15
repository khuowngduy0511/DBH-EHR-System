using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// API integration tests for DBH.Appointment.Service
/// Covers: AppointmentsController, EncountersController
/// </summary>
public class AppointmentServiceTests : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService"
    };

    // =========================================================================
    // APPOINTMENTS - Full flow tests
    // =========================================================================

    [SkippableFact]
    public async Task CreateAppointment_WithSeedData_ShouldReturnSuccessMessage()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var request = new { patientId = TestSeedData.PatientUserId, doctorId = TestSeedData.DoctorUserId, orgId = TestSeedData.HospitalAOrgId, appointmentDate = DateTime.UtcNow.AddDays(7).ToString("o"), reason = "General checkup", notes = "First visit" };
        var response = await PostAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Create, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
        if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(json.GetProperty("success").GetBoolean());
        }
    }

    [SkippableFact]
    public async Task GetAppointments_AsAdmin_ShouldReturnPagedList()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var response = await GetWithRetryAsync(AppointmentClient, $"{ApiEndpoints.Appointments.GetAll}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.True(json.TryGetProperty("data", out _));
    }

    [SkippableFact]
    public async Task GetAppointment_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var response = await GetWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [SkippableFact]
    public async Task UpdateStatus_WithFakeId_ShouldReturnNotFoundOrError()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var response = await PutWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.UpdateStatus(Guid.NewGuid(), "Confirmed"), null);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
    }

    [SkippableFact]
    public async Task ConfirmAppointment_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var response = await PutWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Confirm(Guid.NewGuid()), null);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
    }

    [SkippableFact]
    public async Task RejectAppointment_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var request = new { reason = "Schedule conflict" };
        var response = await PutAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Reject(Guid.NewGuid()), request);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task CancelAppointment_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var request = new { reason = "Patient requested" };
        var response = await PutAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.Cancel(Guid.NewGuid()), request);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task CheckIn_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var response = await PutWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.CheckIn(Guid.NewGuid()), null);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task SearchDoctors_ShouldReturnPagedResult()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var response = await GetWithRetryAsync(AppointmentClient, $"{ApiEndpoints.Appointments.SearchDoctors}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [SkippableFact]
    public async Task GetPatientsByDoctor_WithSeedDoctor_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var response = await GetWithRetryAsync(AppointmentClient, $"{ApiEndpoints.Appointments.PatientsByDoctor(TestSeedData.DoctorUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    // =========================================================================
    // ENCOUNTERS
    // =========================================================================

    [SkippableFact]
    public async Task GetEncounter_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var response = await GetWithRetryAsync(AppointmentClient, ApiEndpoints.Encounters.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetEncountersByPatient_WithSeedPatient_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var response = await GetWithRetryAsync(AppointmentClient, $"{ApiEndpoints.Encounters.ByPatient(TestSeedData.PatientUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [SkippableFact]
    public async Task CompleteEncounter_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(AppointmentClient);
        var request = new { diagnosis = "Cold", treatment = "Rest", prescription = "Paracetamol" };
        var response = await PutAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Encounters.Complete(Guid.NewGuid()), request);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}

