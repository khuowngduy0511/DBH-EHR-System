using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_GetUserId_WithMissingParams_ShouldReturnBadRequest : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService" };

    [SkippableFact]
    public async Task GetUserId_WithMissingParams_ShouldReturnBadRequest()
    {
        await AuthenticateAsAdminAsync(AuthClient);

        var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Auth.UserId);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
