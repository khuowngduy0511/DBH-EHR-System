using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

public class VerifySeedOrganization_ThenListDepartmentsAndMemberships : Shared.ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "OrganizationService" };

    [SkippableFact]
    public async Task VerifySeedOrganization_ThenListDepartmentsAndMemberships_Test()
    {
        await AuthenticateAsAdminAsync(OrganizationClient);

        var orgResponse = await GetWithRetryAsync(OrganizationClient, Shared.ApiEndpoints.Organizations.GetById(Shared.TestSeedData.HospitalAOrgId));
        Assert.Equal(HttpStatusCode.OK, orgResponse.StatusCode);

        var deptResponse = await GetWithRetryAsync(OrganizationClient, $"{Shared.ApiEndpoints.Departments.ByOrganization(Shared.TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, deptResponse.StatusCode);

        var memberResponse = await GetWithRetryAsync(OrganizationClient, $"{Shared.ApiEndpoints.Memberships.ByOrganization(Shared.TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, memberResponse.StatusCode);
    }
}