using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_ChangeMyPassword_WithFakePassword_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService" };

    [SkippableFact]
    public async Task ChangeMyPassword_WithFakePassword_ShouldReturnError()
    {
        var request = new { oldPassword = "WrongPassword", newPassword = "NewPass123" };
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.ChangeMyPassword, request);
        
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized);
    }
}
