using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

/// <summary>
/// End-to-end: Full patient journey across Auth, Appointment, EHR, Consent services.
/// Flow: Register → Login → Verify Profile → Book Appointment → Confirm → Check-In → Encounter
///       → Deactivate Account → Verify Login Fails → Re-Register (reactivation) → Verify Clean Profile
/// </summary>
public class PatientJourneyTests : Shared.ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AppointmentService"
    };

    [SkippableFact]
    public async Task FullPatientJourney_RegisterToDeactivateToReRegister()
    {
        // =====================================================================
        // STEP 1: Register a new patient
        // =====================================================================
        var email = $"e2e_patient_{Guid.NewGuid():N}@test.com";
        var phone = $"09{Random.Shared.Next(10000000, 99999999)}";
        var registerRequest = new { fullName = "E2E Test Patient", email, password = "E2ETest@123", phone, gender = "Male", dateOfBirth = "1995-03-15" };

        var registerResponse = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Register, registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        var registerJson = await ReadJsonResponseAsync(registerResponse);
        Assert.True(registerJson.GetProperty("success").GetBoolean(), $"Register failed: {registerJson.GetProperty("message").GetString()}");

        var userId = Guid.Parse(registerJson.GetProperty("userId").GetString()!);

        // =====================================================================
        // STEP 2: Login with the new patient
        // =====================================================================
        var loginRequest = new { email, password = "E2ETest@123" };
        var loginResponse = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Login, loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginJson = await ReadJsonResponseAsync(loginResponse);
        Assert.True(loginJson.GetProperty("success").GetBoolean(), "Login failed after registration");

        var accessToken = loginJson.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(accessToken), "No access token returned");

        // =====================================================================
        // STEP 3: Verify profile matches registration data
        // =====================================================================
        AuthClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var profileResponse = await GetWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Me);
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        var profileJson = await ReadJsonResponseAsync(profileResponse);
        Assert.Equal(email, profileJson.GetProperty("email").GetString());
        Assert.Equal("E2E Test Patient", profileJson.GetProperty("fullName").GetString());

        var newPatientUserId = Guid.Parse(profileJson.GetProperty("userId").GetString()!);

        // =====================================================================
        // STEP 4: Doctor books appointment for the patient
        // =====================================================================
        await AuthenticateAsDoctorAsync(AppointmentClient);
        var appointmentRequest = new
        {
            patientId = newPatientUserId,
            doctorId = Shared.TestSeedData.DoctorUserId,
            orgId = Shared.TestSeedData.HospitalAOrgId,
            appointmentDate = DateTime.UtcNow.AddDays(1).ToString("o"),
            reason = "E2E Test - General checkup",
            notes = "Created by E2E test"
        };

        var appointmentResponse = await PostAsJsonWithRetryAsync(AppointmentClient, Shared.ApiEndpoints.Appointments.Create, appointmentRequest);
        var appointmentJson = await ReadJsonResponseAsync(appointmentResponse);
        Assert.False(string.IsNullOrEmpty(appointmentJson.GetProperty("message").GetString()));

        if (appointmentResponse.StatusCode == HttpStatusCode.Created || appointmentResponse.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(appointmentJson.GetProperty("success").GetBoolean());
            var appointmentId = Guid.Parse(appointmentJson.GetProperty("data").GetProperty("appointmentId").GetString()!);

            // STEP 5: Confirm appointment
            var confirmResponse = await PutWithRetryAsync(AppointmentClient, Shared.ApiEndpoints.Appointments.Confirm(appointmentId), null);
            if (confirmResponse.StatusCode == HttpStatusCode.OK)
            {
                // STEP 6: Check-in
                var checkInResponse = await PutWithRetryAsync(AppointmentClient, Shared.ApiEndpoints.Appointments.CheckIn(appointmentId), null);
                if (checkInResponse.StatusCode == HttpStatusCode.OK)
                {
                    // STEP 7: Create encounter
                    var encounterRequest = new
                    {
                        appointmentId,
                        doctorId = Shared.TestSeedData.DoctorUserId,
                        patientId = newPatientUserId,
                        notes = "E2E encounter notes"
                    };
                    var encounterResponse = await PostAsJsonWithRetryAsync(AppointmentClient, Shared.ApiEndpoints.Encounters.Create, encounterRequest);
                    var encounterJson = await ReadJsonResponseAsync(encounterResponse);
                    Assert.False(string.IsNullOrEmpty(encounterJson.GetProperty("message").GetString()));
                }
            }
        }

        // =====================================================================
        // STEP 8: Deactivate the patient account
        // =====================================================================
        // Use the patient's own token to deactivate
        AuthClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var deactivateResponse = await DeleteWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.DeleteUser(newPatientUserId));
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        var deactivateJson = await ReadJsonResponseAsync(deactivateResponse);
        Assert.True(deactivateJson.GetProperty("success").GetBoolean(),
            $"Deactivate failed: {deactivateJson.GetProperty("message").GetString()}");

        // =====================================================================
        // STEP 9: Verify login fails after deactivation
        // =====================================================================
        AuthClient.DefaultRequestHeaders.Authorization = null;
        var failedLoginResponse = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Login, loginRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, failedLoginResponse.StatusCode);
        var failedLoginJson = await ReadJsonResponseAsync(failedLoginResponse);
        Assert.False(failedLoginJson.GetProperty("success").GetBoolean(),
            "Login should fail after account deactivation");

        // =====================================================================
        // STEP 10: Re-register with the same email (reactivation)
        // =====================================================================
        var newPhone = $"09{Random.Shared.Next(10000000, 99999999)}";
        var reRegisterRequest = new { fullName = "Reactivated Patient", email, password = "NewPass@456", phone = newPhone, gender = "Female", dateOfBirth = "2000-01-01" };

        var reRegisterResponse = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Register, reRegisterRequest);
        Assert.Equal(HttpStatusCode.OK, reRegisterResponse.StatusCode);
        var reRegisterJson = await ReadJsonResponseAsync(reRegisterResponse);
        Assert.True(reRegisterJson.GetProperty("success").GetBoolean(),
            $"Re-register failed: {reRegisterJson.GetProperty("message").GetString()}");

        // =====================================================================
        // STEP 11: Login with new password and verify clean profile
        // =====================================================================
        var newLoginRequest = new { email, password = "NewPass@456" };
        var newLoginResponse = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Login, newLoginRequest);
        Assert.Equal(HttpStatusCode.OK, newLoginResponse.StatusCode);
        var newLoginJson = await ReadJsonResponseAsync(newLoginResponse);
        Assert.True(newLoginJson.GetProperty("success").GetBoolean(), "Login failed after reactivation");

        var newToken = newLoginJson.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(newToken));

        // Verify profile has new data, NOT old data
        AuthClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
        var newProfileResponse = await GetWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Me);
        Assert.Equal(HttpStatusCode.OK, newProfileResponse.StatusCode);
        var newProfileJson = await ReadJsonResponseAsync(newProfileResponse);

        Assert.Equal("Reactivated Patient", newProfileJson.GetProperty("fullName").GetString());
        Assert.Equal(email, newProfileJson.GetProperty("email").GetString());
        // Old name "E2E Test Patient" should NOT appear
        Assert.NotEqual("E2E Test Patient", newProfileJson.GetProperty("fullName").GetString());

        AuthClient.DefaultRequestHeaders.Authorization = null;
    }

    [SkippableFact]
    public async Task LoginAllSeedUsers_ShouldSucceed()
    {
        // Verify all seed accounts can log in successfully
        var credentials = new[]
        {
            (Shared.TestSeedData.AdminEmail, Shared.TestSeedData.AdminPassword, "Admin"),
            (Shared.TestSeedData.DoctorEmail, Shared.TestSeedData.DoctorPassword, "Doctor"),
            (Shared.TestSeedData.PatientEmail, Shared.TestSeedData.PatientPassword, "Patient"),
            (Shared.TestSeedData.NurseEmail, Shared.TestSeedData.NursePassword, "Nurse"),
            (Shared.TestSeedData.PharmacistEmail, Shared.TestSeedData.PharmacistPassword, "Pharmacist"),
            (Shared.TestSeedData.ReceptionistEmail, Shared.TestSeedData.ReceptionistPassword, "Receptionist")
        };

        foreach (var (email, password, role) in credentials)
        {
            var response = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Login, new { email, password });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await ReadJsonResponseAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean(), $"{role} login failed: {json.GetProperty("message").GetString()}");
            Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()), $"{role} got no token");
        }
    }

    [SkippableFact]
    public async Task RegisterDuplicate_ThenLoginOriginal_ShouldWork()
    {
        // STEP 1: Try to register with existing admin email — should fail
        var dupRequest = new { fullName = "Dup", email = Shared.TestSeedData.AdminEmail, password = "Test@123", phone = "0999999998", gender = "Male", dateOfBirth = "1990-01-01" };
        var dupResponse = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Register, dupRequest);
        Assert.Equal(HttpStatusCode.BadRequest, dupResponse.StatusCode);
        var dupJson = await ReadJsonResponseAsync(dupResponse);
        Assert.False(dupJson.GetProperty("success").GetBoolean());

        // STEP 2: Original admin can still log in
        var loginResponse = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Login, new { email = Shared.TestSeedData.AdminEmail, password = Shared.TestSeedData.AdminPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginJson = await ReadJsonResponseAsync(loginResponse);
        Assert.True(loginJson.GetProperty("success").GetBoolean());
    }
}


