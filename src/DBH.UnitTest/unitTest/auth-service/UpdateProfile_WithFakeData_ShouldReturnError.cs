using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_UpdateProfile_WithFakeData_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService" };

    [SkippableFact]
    public async Task UpdateProfile_WithFakeData_ShouldReturnError()
    {
        var request = new { fullName = "Test User", dateOfBirth = "1990-01-01", gender = "Male" };
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.UpdateProfile, request);
        
        Assert.False(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.OK);
    }
}
