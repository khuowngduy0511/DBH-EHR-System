using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_RevokeTokenFlow_LoginCallThenRevokeAndVerifyError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] 
    { 
        "AuthService" 
    };

    [SkippableFact]
    public async Task RevokeTokenFlow_LoginCallThenRevokeAndVerifyError_ShouldSucceed()
    {
        // 1. Login to get token via AuthenticateAsDoctorAsync
        var tempClient = new HttpClient { BaseAddress = AuthClient.BaseAddress };
        var loginJson = await AuthenticateAsDoctorAsync(tempClient);
        // Token is already set on tempClient by AuthenticateAsDoctorAsync
        var accessToken = loginJson.GetProperty("token").GetString() ?? 
                         tempClient.DefaultRequestHeaders.Authorization?.Parameter;
        
        // 2. Create authenticated client with token and use the token that was set
        var authenticatedClient = tempClient;
        
        // 3. Call Me endpoint with token (should succeed)
        var callWithTokenResponse = await GetWithRetryAsync(authenticatedClient, ApiEndpoints.Auth.Me);
        Assert.Equal(HttpStatusCode.OK, callWithTokenResponse.StatusCode);
        
        // 4. Revoke the token
        var revokeResponse = await PostWithRetryAsync(authenticatedClient, ApiEndpoints.Auth.RevokeToken, null);
        Assert.True(revokeResponse.StatusCode == HttpStatusCode.OK || revokeResponse.StatusCode == HttpStatusCode.BadRequest);
        
        // 5. Try to call Me again with revoked token (should fail with 401)
        var callWithRevokedTokenResponse = await GetWithRetryAsync(authenticatedClient, ApiEndpoints.Auth.Me);
        Assert.Equal(HttpStatusCode.Unauthorized, callWithRevokedTokenResponse.StatusCode);
    }
}
