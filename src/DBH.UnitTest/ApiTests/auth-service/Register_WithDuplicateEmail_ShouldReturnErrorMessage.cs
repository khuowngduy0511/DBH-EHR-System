using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_Register_WithDuplicateEmail_ShouldReturnErrorMessage : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task Register_WithDuplicateEmail_ShouldReturnErrorMessage()
    {
    var request = new { fullName = "Dup User", email = TestSeedData.AdminEmail, password = "Test@12345", phone = "0999999999", gender = "Male", dateOfBirth = "1990-01-01" };
    var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Register, request);
    
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.False(json.GetProperty("success").GetBoolean());
    // Should contain an error message about duplicate
    Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }
}
