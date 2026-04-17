using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_MissingCoverage_OrganizationEndpoints_ShouldBeCallable : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "OrganizationService"
    };

    [SkippableFact]
    public async Task Organizations_Delete_ShouldReturnExpectedStatus()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await DeleteWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.Delete(Guid.NewGuid()));

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadRequest);
    }

    [SkippableFact]
    public async Task Departments_Update_ShouldReturnExpectedStatus()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var payload = new { name = "Updated Department Name" };
        var response = await PutAsJsonWithRetryAsync(OrganizationClient, ApiEndpoints.Departments.Update(Guid.NewGuid()), payload);

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Departments_Delete_ShouldReturnExpectedStatus()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await DeleteWithRetryAsync(OrganizationClient, ApiEndpoints.Departments.Delete(Guid.NewGuid()));

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Memberships_SearchDoctors_ShouldReturnExpectedStatus()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var payload = new
        {
            organizationId = TestSeedData.HospitalAOrgId,
            page = 1,
            pageSize = 10
        };

        var response = await PostAsJsonWithRetryAsync(OrganizationClient, ApiEndpoints.Memberships.SearchDoctors, payload);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task Memberships_Update_ShouldReturnExpectedStatus()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var payload = new
        {
            role = "Doctor",
            departmentId = TestSeedData.CardiologyDeptId,
            isPrimary = true
        };

        var response = await PutAsJsonWithRetryAsync(OrganizationClient, ApiEndpoints.Memberships.Update(Guid.NewGuid()), payload);

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task Memberships_Delete_ShouldReturnExpectedStatus()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);
        var response = await DeleteWithRetryAsync(OrganizationClient, ApiEndpoints.Memberships.Delete(Guid.NewGuid()));

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.OK);
    }
}
