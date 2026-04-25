using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_GetAllUsers_AsAdmin_ShouldReturnSeedUsers : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task GetAllUsers_AsAdmin_ShouldReturnSeedUsers()
    {
    await AuthenticateAsAdminAsync(AuthClient);
    var response = await GetWithRetryAsync(AuthClient, $"{ApiEndpoints.Auth.Users}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    // Should contain at least the 6 seeded users
    var data = json.GetProperty("data");
    Assert.True(data.GetArrayLength() >= 6, "Expected at least 6 seed users");
    }
}
