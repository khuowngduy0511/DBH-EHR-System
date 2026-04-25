using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_GetUserProfile_WithKnownAdminId_ShouldReturnMatchingProfile : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task GetUserProfile_WithKnownAdminId_ShouldReturnMatchingProfile()
    {
    await AuthenticateAsAdminAsync(AuthClient);
    var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Auth.UserProfile(TestSeedData.AdminUserId));
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.Equal(TestSeedData.AdminEmail, json.GetProperty("email").GetString());
    }
}
