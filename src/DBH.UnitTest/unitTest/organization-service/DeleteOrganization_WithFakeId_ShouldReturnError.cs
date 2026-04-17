using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_DeleteOrganization_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "OrganizationService" };

    [SkippableFact]
    public async Task DeleteOrganization_WithFakeId_ShouldReturnError()
    {
        var fakeOrgId = Guid.NewGuid();
        var url = ApiEndpoints.Organizations.Delete(fakeOrgId);
        
        var response = await DeleteWithRetryAsync(AuthClient, url);
        
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
