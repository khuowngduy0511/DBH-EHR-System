using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_Login_WithDoctorCredentials_ShouldReturnTokenAndUserData : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task Login_WithDoctorCredentials_ShouldReturnTokenAndUserData()
    {
    var request = new { email = TestSeedData.DoctorEmail, password = TestSeedData.DoctorPassword };
    var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, request);
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.GetProperty("success").GetBoolean());
    Assert.False(string.IsNullOrEmpty(json.GetProperty("token").GetString()));
    }
}
