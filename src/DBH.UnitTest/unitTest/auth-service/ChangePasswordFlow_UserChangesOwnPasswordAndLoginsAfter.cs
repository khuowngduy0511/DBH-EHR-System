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
        // 1. Login as doctor with original password
        var doctorClient = new HttpClient { BaseAddress = AuthClient.BaseAddress };
        var loginJson = await AuthenticateAsDoctorAsync(doctorClient);
        
        // 3. Change own password via ChangeMyPassword
        var newPassword = "NewDoctorPassword" + Guid.NewGuid().ToString().Substring(0, 8);
        var changeRequest = new { oldPassword = TestSeedData.DoctorPassword, newPassword = newPassword };
        
        var changeResponse = await PutAsJsonWithRetryAsync(doctorClient, ApiEndpoints.Auth.ChangeMyPassword, changeRequest);
        Assert.True(changeResponse.StatusCode == HttpStatusCode.OK || changeResponse.StatusCode == HttpStatusCode.BadRequest);
        
        // 4. Try login with old password (should fail)
        var loginRequest = new { email = TestSeedData.DoctorEmail, password = TestSeedData.DoctorPassword };
        var loginOldResponse = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, loginRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, loginOldResponse.StatusCode);
        
        // 5. Try login with new password (should succeed)
        var loginNewRequest = new { email = TestSeedData.DoctorEmail, password = newPassword };
        var loginNewResponse = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, loginNewRequest);
        Assert.Equal(HttpStatusCode.OK, loginNewResponse.StatusCode);
        
        // 6. Restore password for other tests
        var restoreClient = new HttpClient { BaseAddress = AuthClient.BaseAddress };
        var restoreJson = await AuthenticateAsync(restoreClient, TestSeedData.DoctorEmail, newPassword);
        
        var restoreRequest = new { oldPassword = newPassword, newPassword = TestSeedData.DoctorPassword };
        await PutAsJsonWithRetryAsync(restoreClient, ApiEndpoints.Auth.ChangeMyPassword, restoreRequest);
    }
}
