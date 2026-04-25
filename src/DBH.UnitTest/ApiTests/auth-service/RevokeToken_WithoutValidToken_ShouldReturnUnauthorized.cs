using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_RevokeToken_WithoutValidToken_ShouldReturnUnauthorized : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService" };

    [SkippableFact]
    public async Task RevokeToken_WithoutValidToken_ShouldReturnUnauthorized()
    {
        var response = await PostWithRetryAsync(AuthClient, ApiEndpoints.Auth.RevokeToken, null);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
