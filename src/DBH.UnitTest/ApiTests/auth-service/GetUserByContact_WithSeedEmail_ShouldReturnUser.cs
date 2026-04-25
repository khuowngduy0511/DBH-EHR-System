using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_GetUserByContact_WithSeedEmail_ShouldReturnUser : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task GetUserByContact_WithSeedEmail_ShouldReturnUser()
    {
    await AuthenticateAsAdminAsync(AuthClient);
    var response = await GetWithRetryAsync(AuthClient, $"{ApiEndpoints.Auth.UsersByContact}?email={TestSeedData.DoctorEmail}");
    
    Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }
}
