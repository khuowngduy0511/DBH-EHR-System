using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_UpdateMembership_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "OrganizationService" };

    [SkippableFact]
    public async Task UpdateMembership_WithFakeId_ShouldReturnError()
    {
        var fakeMembershipId = Guid.NewGuid();
        var request = new { role = "Doctor", departmentId = Guid.NewGuid() };
        var url = ApiEndpoints.Memberships.Update(fakeMembershipId);
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, url, request);
        
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
