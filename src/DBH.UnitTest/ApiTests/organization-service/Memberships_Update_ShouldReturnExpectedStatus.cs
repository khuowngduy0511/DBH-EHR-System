using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class OrganizationServiceTests_Memberships_Update_ShouldReturnExpectedStatus : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "OrganizationService" };

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
}
