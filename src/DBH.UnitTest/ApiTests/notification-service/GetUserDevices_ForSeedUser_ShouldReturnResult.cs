using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class NotificationServiceTests_GetUserDevices_ForSeedUser_ShouldReturnResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "NotificationService"
    };

    [SkippableFact]
    public async Task GetUserDevices_ForSeedUser_ShouldReturnResult()
    {
    await AuthenticateAsAdminAsync(NotificationClient);
    var userDid = $"did:dbh:user:{TestSeedData.AdminUserId}";
    var response = await GetWithRetryAsync(NotificationClient, ApiEndpoints.DeviceTokens.ByUser(userDid));
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
