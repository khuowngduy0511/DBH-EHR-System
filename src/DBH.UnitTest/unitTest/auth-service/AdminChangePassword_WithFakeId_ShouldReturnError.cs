using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_AdminChangePassword_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService" };

    [SkippableFact]
    public async Task AdminChangePassword_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(AuthClient);
        var fakeUserId = Guid.NewGuid();
        var request = new { oldPassword = "OldPass123", newPassword = "NewPass123" };
        var url = ApiEndpoints.Auth.AdminChangePassword.Replace("{userId}", fakeUserId.ToString());
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, url, request);
        
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Unauthorized);
    }
}
