using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_UpdateRole_WithFakeData_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService" };

    [SkippableFact]
    public async Task UpdateRole_WithFakeData_ShouldReturnError()
    {
        var request = new { userId = Guid.NewGuid(), newRole = "Doctor" };
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.UpdateRole, request);
        
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized);
    }
}
