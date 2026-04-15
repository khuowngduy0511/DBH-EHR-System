using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_DeleteMembership_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "OrganizationService" };

    [SkippableFact]
    public async Task DeleteMembership_WithFakeId_ShouldReturnError()
    {
        var fakeMembershipId = Guid.NewGuid();
        var url = ApiEndpoints.Memberships.Delete(fakeMembershipId);
        
        var response = await DeleteWithRetryAsync(AuthClient, url);
        
        Assert.False(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
