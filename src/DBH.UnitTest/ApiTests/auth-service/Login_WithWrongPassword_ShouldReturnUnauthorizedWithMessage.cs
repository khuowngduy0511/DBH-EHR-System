using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_Login_WithWrongPassword_ShouldReturnUnauthorizedWithMessage : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task Login_WithWrongPassword_ShouldReturnUnauthorizedWithMessage()
    {
    var request = new { email = TestSeedData.AdminEmail, password = "wrong_password" };
    var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, request);
    
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.False(json.GetProperty("success").GetBoolean());
    }
}
