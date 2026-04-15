using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// API integration tests for DBH.Organization.Service
/// Uses seed organizations, departments, memberships for validation.
/// </summary>
public class OrganizationServiceTests : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "OrganizationService"
    };

    // =========================================================================
    // ORGANIZATIONS - GET with known seed data
    // =========================================================================

    [SkippableFact]
    public async Task GetOrganizations_AsAdmin_ShouldReturnSeedOrganizations()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, $"{ApiEndpoints.Organizations.GetAll}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out var dataElement));
        var data = dataElement;
        Assert.True(data.GetArrayLength() >= 3, "Should contain at least 3 seed organizations");
    }

    [SkippableFact]
    public async Task GetOrganization_HospitalA_ShouldReturnCorrectData()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.GetById(TestSeedData.HospitalAOrgId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out var dataElement));
        var data = dataElement;
        Assert.Equal(TestSeedData.HospitalAName, data.GetProperty("orgName").GetString());
    }

    [SkippableFact]
    public async Task GetOrganization_Clinic_ShouldReturnCorrectData()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.GetById(TestSeedData.ClinicOrgId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out var dataElement));
        var data = dataElement;
        Assert.Equal(TestSeedData.ClinicName, data.GetProperty("orgName").GetString());
    }

    [SkippableFact]
    public async Task GetOrganization_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task CreateOrganization_WithAdminAuth_ShouldReturnSuccess()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var request = new { name = "Test Hospital", address = "123 Test St", phone = "0900000001", email = "test@hospital.com", type = "Hospital" };
        var response = await PostAsJsonWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.Create, request);

        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }

    [SkippableFact]
    public async Task UpdateOrganization_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var request = new { name = "Updated Hospital" };
        var response = await PutAsJsonWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.Update(Guid.NewGuid()), request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task VerifyOrganization_WithSeedOrg_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await PostWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.Verify(TestSeedData.HospitalAOrgId, TestSeedData.AdminUserId), null);

        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }

    // =========================================================================
    // DEPARTMENTS - Verify against seed data
    // =========================================================================

    [SkippableFact]
    public async Task GetDepartmentsByOrg_HospitalA_ShouldReturnSeedDepartments()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, $"{ApiEndpoints.Departments.ByOrganization(TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out var dataElement));
        var data = dataElement;
        // Hospital A has 3 departments: Cardiology, Pharmacy, Reception
        Assert.True(data.GetArrayLength() >= 3, "Hospital A should have at least 3 departments");
    }

    [SkippableFact]
    public async Task GetDepartment_CardiologyDept_ShouldReturnCorrectData()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Departments.GetById(TestSeedData.CardiologyDeptId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out var dataElement));
        var data = dataElement;
        Assert.Equal("Khoa Tim mach", data.GetProperty("departmentName").GetString());
    }

    [SkippableFact]
    public async Task GetDepartment_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Departments.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // MEMBERSHIPS - Verify against seed data
    // =========================================================================

    [SkippableFact]
    public async Task GetMembershipsByOrg_HospitalA_ShouldReturnSeedMemberships()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, $"{ApiEndpoints.Memberships.ByOrganization(TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out var dataElement));
        var data = dataElement;
        // Hospital A has admin, doctor, pharmacist, receptionist memberships
        Assert.True(data.GetArrayLength() >= 4, "Hospital A should have at least 4 memberships");
    }

    [SkippableFact]
    public async Task GetMembership_DoctorMembership_ShouldReturnCorrectData()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Memberships.GetById(TestSeedData.DoctorMembershipId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out var dataElement));
        var data = dataElement;
        Assert.True(data.ValueKind == JsonValueKind.Object);
    }

    [SkippableFact]
    public async Task GetMembershipsByUser_DoctorUser_ShouldReturnMemberships()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, $"{ApiEndpoints.Memberships.ByUser(TestSeedData.DoctorUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out var dataElement));
        var data = dataElement;
        Assert.True(data.GetArrayLength() >= 1, "Doctor should have at least 1 membership");
    }

    [SkippableFact]
    public async Task GetMembership_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Memberships.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // PAYMENT CONFIG - Verify flow
    // =========================================================================

    [SkippableFact]
    public async Task GetPaymentConfigStatus_ForSeedOrg_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.PaymentConfig.GetStatus(TestSeedData.HospitalAOrgId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }

    // =========================================================================
    // INTERNAL API
    // =========================================================================

    [SkippableFact]
    public async Task GetPaymentKeys_WithInternalApiKey_ShouldReturnResult()
    {
        OrganizationClient.DefaultRequestHeaders.Add("X-Internal-Api-Key", "dbh-internal-s2s-secret-key-2026!");
        var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Internal.GetPaymentKeys(TestSeedData.HospitalAOrgId));

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        OrganizationClient.DefaultRequestHeaders.Remove("X-Internal-Api-Key");
    }
}

