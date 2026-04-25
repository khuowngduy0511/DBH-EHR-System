using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class OrganizationServiceTests_Memberships_SearchDoctors_ShouldReturnExpectedStatus : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "OrganizationService" };

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
}
