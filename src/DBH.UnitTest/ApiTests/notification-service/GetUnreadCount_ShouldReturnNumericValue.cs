using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class NotificationServiceTests_GetUnreadCount_ShouldReturnNumericValue : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "NotificationService"
    };

    [SkippableFact]
    public async Task GetUnreadCount_ShouldReturnNumericValue()
    {
    await AuthenticateAsAdminAsync(NotificationClient);
    var userDid = $"did:dbh:user:{TestSeedData.AdminUserId}";
    var response = await GetWithRetryAsync(NotificationClient, ApiEndpoints.Notifications.UnreadCount(userDid));
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.TryGetProperty("count", out _));
    }
}
