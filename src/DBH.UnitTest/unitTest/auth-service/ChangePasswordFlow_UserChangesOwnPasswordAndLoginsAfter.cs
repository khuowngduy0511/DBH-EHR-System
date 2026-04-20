using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_ChangePasswordFlow_UpdateThenLoginWithOldFailNewSuccess : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] 
    { 
        "AuthService" 
    };

    [SkippableFact]
    public async Task ChangePasswordFlow_UpdateThenLoginWithOldFailNewSuccess_ShouldSucceed()
    {
        // 1. Use a fresh doctor account to avoid password races with other tests.
        var freshUsers = await CreateFreshDoctorAndPatientAsync();
        var doctorClient = new HttpClient { BaseAddress = AuthClient.BaseAddress };
        await AuthenticateAsync(doctorClient, freshUsers.DoctorEmail, freshUsers.DoctorPassword);
        
        // 2. Change own password via ChangeMyPassword
        var newPassword = $"NewDoc@{Guid.NewGuid().ToString("N")[..8]}";
        var changeRequest = new { oldPassword = freshUsers.DoctorPassword, newPassword = newPassword };
        
        var changeResponse = await PutAsJsonWithRetryAsync(doctorClient, ApiEndpoints.Auth.ChangeMyPassword, changeRequest);
        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);
        
        // 3. Try login with old password (should fail)
        var loginRequest = new { email = freshUsers.DoctorEmail, password = freshUsers.DoctorPassword };
        var loginOldResponse = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, loginRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, loginOldResponse.StatusCode);
        
        // 4. Try login with new password (should succeed)
        var loginNewRequest = new { email = freshUsers.DoctorEmail, password = newPassword };
        var loginNewResponse = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, loginNewRequest);
        Assert.Equal(HttpStatusCode.OK, loginNewResponse.StatusCode);
    }
}
