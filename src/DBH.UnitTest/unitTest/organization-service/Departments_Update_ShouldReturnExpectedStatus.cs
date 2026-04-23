using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_Departments_Update_ShouldReturnExpectedStatus : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "OrganizationService" };

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
}