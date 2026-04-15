using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_Login_WithPatientCredentials_ShouldReturnTokenAndUserData : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task Login_WithPatientCredentials_ShouldReturnTokenAndUserData()
    {
    var request = new { email = TestSeedData.PatientEmail, password = TestSeedData.PatientPassword };
    var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, request);
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.GetProperty("success").GetBoolean());
    }
}
