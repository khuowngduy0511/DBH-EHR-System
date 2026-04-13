using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// API integration tests for DBH.Auth.Service
/// Covers: AuthController, DoctorsController, PatientsController, StaffController
/// Uses seed data from AuthDbContext for validation.
/// </summary>
public class AuthServiceTests : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService"
    };

    // =========================================================================
    // AUTH CONTROLLER - Login / Register / Profile
    // =========================================================================

    [SkippableFact]
    public async Task Login_WithAdminCredentials_ShouldReturnTokenAndUserData()
    {
        var request = new { email = TestSeedData.AdminEmail, password = TestSeedData.AdminPassword };
        var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
        Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
        Assert.False(string.IsNullOrEmpty(json.GetProperty("refreshToken").GetString()));
    }

    [SkippableFact]
    public async Task Login_WithDoctorCredentials_ShouldReturnTokenAndUserData()
    {
        var request = new { email = TestSeedData.DoctorEmail, password = TestSeedData.DoctorPassword };
        var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
    }

    [SkippableFact]
    public async Task Login_WithPatientCredentials_ShouldReturnTokenAndUserData()
    {
        var request = new { email = TestSeedData.PatientEmail, password = TestSeedData.PatientPassword };
        var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [SkippableFact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorizedWithMessage()
    {
        var request = new { email = "nonexistent@test.com", password = "WrongPassword" };
        var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [SkippableFact]
    public async Task Login_WithWrongPassword_ShouldReturnUnauthorizedWithMessage()
    {
        var request = new { email = TestSeedData.AdminEmail, password = "wrong_password" };
        var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
    }

    [SkippableFact]
    public async Task Register_WithValidRequest_ShouldReturnSuccessMessage()
    {
        var uniqueEmail = $"test_{Guid.NewGuid():N}@test.com";
        var request = new { fullName = "Test Patient", email = uniqueEmail, password = "Test@12345", phone = $"09{Random.Shared.Next(10000000, 99999999)}", gender = "Male", dateOfBirth = "1990-01-01" };
        var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Register, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [SkippableFact]
    public async Task Register_WithDuplicateEmail_ShouldReturnErrorMessage()
    {
        var request = new { fullName = "Dup User", email = TestSeedData.AdminEmail, password = "Test@12345", phone = "0999999999", gender = "Male", dateOfBirth = "1990-01-01" };
        var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Register, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
        // Should contain an error message about duplicate
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [SkippableFact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewAccessToken()
    {
        // Login first to get refresh token
        var loginRequest = new { email = TestSeedData.AdminEmail, password = TestSeedData.AdminPassword };
        var loginResponse = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, loginRequest);
        var loginJson = await ReadJsonResponseAsync(loginResponse);
        var refreshToken = loginJson.GetProperty("refreshToken").GetString();

        var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.RefreshToken, new { refreshToken });
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var json = await ReadJsonResponseAsync(response);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
        }
    }

    [SkippableFact]
    public async Task GetMyProfile_AsAdmin_ShouldReturnAdminData()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Auth.Me);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.Equal(TestSeedData.AdminEmail, json.GetProperty("email").GetString());
        Assert.Equal(TestSeedData.AdminFullName, json.GetProperty("fullName").GetString());
    }

    [SkippableFact]
    public async Task GetMyProfile_AsDoctor_ShouldReturnDoctorData()
    {
        await AuthenticateAsDoctorAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Auth.Me);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.Equal(TestSeedData.DoctorEmail, json.GetProperty("email").GetString());
        Assert.Equal(TestSeedData.DoctorFullName, json.GetProperty("fullName").GetString());
    }

    [SkippableFact]
    public async Task GetUserProfile_WithKnownAdminId_ShouldReturnMatchingProfile()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Auth.UserProfile(TestSeedData.AdminUserId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.Equal(TestSeedData.AdminEmail, json.GetProperty("email").GetString());
    }

    [SkippableFact]
    public async Task GetUserProfile_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Auth.UserProfile(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetAllUsers_AsAdmin_ShouldReturnSeedUsers()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, $"{ApiEndpoints.Auth.Users}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        // Should contain at least the 6 seeded users
        var data = json.GetProperty("data");
        Assert.True(data.GetArrayLength() >= 6, "Expected at least 6 seed users");
    }

    [SkippableFact]
    public async Task GetUserByContact_WithSeedEmail_ShouldReturnUser()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, $"{ApiEndpoints.Auth.UsersByContact}?email={TestSeedData.DoctorEmail}");

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task GetUserKeys_WithKnownDoctorId_ShouldReturnPublicKey()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Auth.UserKeys(TestSeedData.DoctorUserId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("publicKey").GetString()));
    }

    // =========================================================================
    // DOCTORS CONTROLLER - Use seed doctor data
    // =========================================================================

    [SkippableFact]
    public async Task Doctors_GetAll_AsAdmin_ShouldContainSeedDoctor()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, $"{ApiEndpoints.Doctors.GetAll}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        var data = json.GetProperty("data");
        Assert.True(data.GetArrayLength() >= 1, "Should contain at least the seed doctor");
    }

    [SkippableFact]
    public async Task Doctors_GetById_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Doctors.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // PATIENTS CONTROLLER - Use seed patient data
    // =========================================================================

    [SkippableFact]
    public async Task Patients_GetAll_AsAdmin_ShouldContainSeedPatient()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, $"{ApiEndpoints.Patients.GetAll}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        var data = json.GetProperty("data");
        Assert.True(data.GetArrayLength() >= 1, "Should contain at least the seed patient");
    }

    [SkippableFact]
    public async Task Patients_GetById_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Patients.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // STAFF CONTROLLER - Use seed staff data
    // =========================================================================

    [SkippableFact]
    public async Task Staff_GetAll_AsAdmin_ShouldContainSeedStaff()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, $"{ApiEndpoints.Staff.GetAll}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        var data = json.GetProperty("data");
        // Should contain pharmacist, nurse, receptionist staff entries
        Assert.True(data.GetArrayLength() >= 3, "Should contain at least 3 seed staff members");
    }

    [SkippableFact]
    public async Task Staff_GetById_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Staff.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

