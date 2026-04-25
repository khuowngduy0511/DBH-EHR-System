using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_RefreshToken_WithValidToken_ShouldReturnNewAccessToken : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task RefreshToken_WithValidToken_ShouldReturnNewAccessToken()
    {
    // Login first to get refresh token
    var loginRequest = new { email = TestSeedData.AdminEmail, password = TestSeedData.AdminPassword };
    var loginResponse = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, loginRequest);
    var loginJson = await ReadJsonResponseAsync(loginResponse);
    var refreshToken = loginJson.GetProperty("refreshToken").GetString();
    
    var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.RefreshToken, new { refreshToken });
    if (response.StatusCode == HttpStatusCode.OK)
    {
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.GetProperty("success").GetBoolean());
    Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
    }
    }
}
