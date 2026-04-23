using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_Memberships_Delete_ShouldReturnExpectedStatus : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "OrganizationService" };

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