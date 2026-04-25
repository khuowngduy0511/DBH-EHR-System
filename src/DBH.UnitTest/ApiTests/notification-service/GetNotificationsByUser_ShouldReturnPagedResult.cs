using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class NotificationServiceTests_GetNotificationsByUser_ShouldReturnPagedResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "NotificationService"
    };

    [SkippableFact]
    public async Task GetNotificationsByUser_ShouldReturnPagedResult()
    {
    await AuthenticateAsAdminAsync(NotificationClient);
    var userDid = $"did:dbh:user:{TestSeedData.PatientUserId}";
    var response = await GetWithRetryAsync(NotificationClient, $"{ApiEndpoints.Notifications.ByUser(userDid)}?page=1&pageSize=20");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.TryGetProperty("data", out _));
    }
}
