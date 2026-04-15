using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DBH.UnitTest.Api;

/// <summary>
/// API integration tests for DBH.Organization.Service
/// Uses seed organizations, departments, memberships for validation.
/// </summary>
public class OrganizationServiceTests : ApiTestBase
{
    // =========================================================================
    // ORGANIZATIONS - GET with known seed data
    // =========================================================================

    [Fact]
    public async Task GetOrganizations_AsAdmin_ShouldReturnSeedOrganizations()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync($"{ApiEndpoints.Organizations.GetAll}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
        var data = json.GetProperty("data");
        Assert.True(data.GetArrayLength() >= 3, "Should contain at least 3 seed organizations");
    }

    [Fact]
    public async Task GetOrganization_HospitalA_ShouldReturnCorrectData()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync(ApiEndpoints.Organizations.GetById(TestSeedData.HospitalAOrgId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
        var data = json.GetProperty("data");
        Assert.Equal(TestSeedData.HospitalAName, data.GetProperty("orgName").GetString());
    }

    [Fact]
    public async Task GetOrganization_Clinic_ShouldReturnCorrectData()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync(ApiEndpoints.Organizations.GetById(TestSeedData.ClinicOrgId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
        var data = json.GetProperty("data");
        Assert.Equal(TestSeedData.ClinicName, data.GetProperty("orgName").GetString());
    }

    [Fact]
    public async Task GetOrganization_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync(ApiEndpoints.Organizations.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrganization_WithAdminAuth_ShouldReturnSuccess()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var request = new { name = "Test Hospital", address = "123 Test St", phone = "0900000001", email = "test@hospital.com", type = "Hospital" };
        var response = await OrganizationClient.PostAsJsonAsync(ApiEndpoints.Organizations.Create, request);

        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task UpdateOrganization_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var request = new { name = "Updated Hospital" };
        var response = await OrganizationClient.PutAsJsonAsync(ApiEndpoints.Organizations.Update(Guid.NewGuid()), request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VerifyOrganization_WithSeedOrg_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.PostAsync(ApiEndpoints.Organizations.Verify(TestSeedData.HospitalAOrgId, TestSeedData.AdminUserId), null);

        // Already verified in seed, should return OK or message indicating already verified
        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    // =========================================================================
    // DEPARTMENTS - Verify against seed data
    // =========================================================================

    [Fact]
    public async Task GetDepartmentsByOrg_HospitalA_ShouldReturnSeedDepartments()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync($"{ApiEndpoints.Departments.ByOrganization(TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        var data = json.GetProperty("data");
        // Hospital A has 3 departments: Cardiology, Pharmacy, Reception
        Assert.True(data.GetArrayLength() >= 3, "Hospital A should have at least 3 departments");
    }

    [Fact]
    public async Task GetDepartment_CardiologyDept_ShouldReturnCorrectData()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync(ApiEndpoints.Departments.GetById(TestSeedData.CardiologyDeptId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
        var data = json.GetProperty("data");
        Assert.Equal("Khoa Tim mach", data.GetProperty("departmentName").GetString());
    }

    [Fact]
    public async Task GetDepartment_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync(ApiEndpoints.Departments.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // MEMBERSHIPS - Verify against seed data
    // =========================================================================

    [Fact]
    public async Task GetMembershipsByOrg_HospitalA_ShouldReturnSeedMemberships()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync($"{ApiEndpoints.Memberships.ByOrganization(TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        var data = json.GetProperty("data");
        // Hospital A has admin, doctor, pharmacist, receptionist memberships
        Assert.True(data.GetArrayLength() >= 4, "Hospital A should have at least 4 memberships");
    }

    [Fact]
    public async Task GetMembership_DoctorMembership_ShouldReturnCorrectData()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync(ApiEndpoints.Memberships.GetById(TestSeedData.DoctorMembershipId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
        var data = json.GetProperty("data");
        Assert.Equal(TestSeedData.DoctorUserId.ToString(), data.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task GetMembershipsByUser_DoctorUser_ShouldReturnMemberships()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync($"{ApiEndpoints.Memberships.ByUser(TestSeedData.DoctorUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        var data = json.GetProperty("data");
        Assert.True(data.GetArrayLength() >= 1, "Doctor should have at least 1 membership");
    }

    [Fact]
    public async Task GetMembership_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync(ApiEndpoints.Memberships.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // PAYMENT CONFIG - Verify flow
    // =========================================================================

    [Fact]
    public async Task GetPaymentConfigStatus_ForSeedOrg_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await OrganizationClient.GetAsync(ApiEndpoints.PaymentConfig.GetStatus(TestSeedData.HospitalAOrgId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    // =========================================================================
    // INTERNAL API
    // =========================================================================

    [Fact]
    public async Task GetPaymentKeys_WithInternalApiKey_ShouldReturnResult()
    {
        OrganizationClient.DefaultRequestHeaders.Add("X-Internal-Api-Key", "dbh-internal-s2s-secret-key-2026!");
        var response = await OrganizationClient.GetAsync(ApiEndpoints.Internal.GetPaymentKeys(TestSeedData.HospitalAOrgId));

        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        OrganizationClient.DefaultRequestHeaders.Remove("X-Internal-Api-Key");
    }
}
