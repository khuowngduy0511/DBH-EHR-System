using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_GetUserKeys_WithKnownDoctorId_ShouldReturnPublicKey : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task GetUserKeys_WithKnownDoctorId_ShouldReturnPublicKey()
    {
    await AuthenticateAsAdminAsync(AuthClient);
    var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Auth.UserKeys(TestSeedData.DoctorUserId));
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.False(string.IsNullOrEmpty(json.GetProperty("publicKey").GetString()));
    }
}
