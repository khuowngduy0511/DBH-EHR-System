using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_DeleteDepartment_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "OrganizationService" };

    [SkippableFact]
    public async Task DeleteDepartment_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(AuthClient);

        var fakeDeptId = Guid.NewGuid();
        var url = ApiEndpoints.Departments.Delete(fakeDeptId);
        
        var response = await DeleteWithRetryAsync(AuthClient, url);
        
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
