using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_UpdateUser_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService" };

    [SkippableFact]
    public async Task UpdateUser_WithFakeId_ShouldReturnError()
    {
        var fakeUserId = Guid.NewGuid();
        var request = new { fullName = "Updated User", email = "updated@test.com" };
        var url = ApiEndpoints.Auth.UpdateUser.Replace("{userId}", fakeUserId.ToString());
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, url, request);
        
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Unauthorized);
    }
}
