using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_AdminChangePasswordFlow_AdminChangesUserThenVerifyOldFailNewSuccess : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] 
    { 
        "AuthService" 
    };

    [SkippableFact]
    public async Task AdminChangePasswordFlow_AdminChangesUserThenVerifyOldFailNewSuccess_ShouldSucceed()
    {
        // 1. Login as admin
        await AuthenticateAsAdminAsync(AuthClient);
        
        // 2. Admin changes doctor's password
        var newPassword = "AdminSetPassword" + Guid.NewGuid().ToString().Substring(0, 8);
        var adminChangeRequest = new { newPassword = newPassword };
        var adminChangeUrl = ApiEndpoints.Auth.AdminChangePassword.Replace("{userId}", TestSeedData.DoctorUserId.ToString());
        
        var adminChangeResponse = await PutAsJsonWithRetryAsync(AuthClient, adminChangeUrl, adminChangeRequest);
        Assert.True(adminChangeResponse.StatusCode == HttpStatusCode.OK || adminChangeResponse.StatusCode == HttpStatusCode.BadRequest);
        
        // 3. Try login doctor with old password (should fail)
        var loginOldRequest = new { email = TestSeedData.DoctorEmail, password = TestSeedData.DoctorPassword };
        var loginOldResponse = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, loginOldRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, loginOldResponse.StatusCode);
        
        // 4. Try login doctor with new password (should succeed)
        var loginNewRequest = new { email = TestSeedData.DoctorEmail, password = newPassword };
        var loginNewResponse = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Auth.Login, loginNewRequest);
        Assert.Equal(HttpStatusCode.OK, loginNewResponse.StatusCode);
        
        // 5. Restore doctor's password to original for other tests
        await AuthenticateAsAdminAsync(AuthClient);
        var restoreRequest = new { newPassword = TestSeedData.DoctorPassword };
        var restoreUrl = ApiEndpoints.Auth.AdminChangePassword.Replace("{userId}", TestSeedData.DoctorUserId.ToString());
        
        await PutAsJsonWithRetryAsync(AuthClient, restoreUrl, restoreRequest);
    }
}
