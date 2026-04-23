using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_Departments_Delete_ShouldReturnExpectedStatus : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "OrganizationService" };

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
}