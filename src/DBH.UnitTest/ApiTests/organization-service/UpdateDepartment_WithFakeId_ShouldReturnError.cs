using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class OrganizationServiceTests_UpdateDepartment_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "OrganizationService" };

    [SkippableFact]
    public async Task UpdateDepartment_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(AuthClient);

        var fakeDeptId = Guid.NewGuid();
        var request = new { name = "Updated Department", description = "Updated Desc" };
        var url = ApiEndpoints.Departments.Update(fakeDeptId);
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, url, request);
        
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
