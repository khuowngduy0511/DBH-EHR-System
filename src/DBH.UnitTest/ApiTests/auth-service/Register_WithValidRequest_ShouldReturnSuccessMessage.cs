using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_Register_WithValidRequest_ShouldReturnSuccessMessage : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task Register_WithValidRequest_ShouldReturnSuccessMessage()
    {
    var uniqueEmail = $"test_{Guid.NewGuid():N}@test.com";
    var request = new { fullName = "Test Patient", email = uniqueEmail, password = "Test@12345", phone = $"09{Random.Shared.Next(10000000, 99999999)}", gender = "Male", dateOfBirth = "1990-01-01" };
    var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Register, request);
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.GetProperty("success").GetBoolean());
    }
}
