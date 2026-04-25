using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_GetMyProfile_AsAdmin_ShouldReturnAdminData : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task GetMyProfile_AsAdmin_ShouldReturnAdminData()
    {
    await AuthenticateAsAdminAsync(AuthClient);
    var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Auth.Me);
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.Equal(TestSeedData.AdminEmail, json.GetProperty("email").GetString());
    Assert.Equal(TestSeedData.AdminFullName, json.GetProperty("fullName").GetString());
    }
}
