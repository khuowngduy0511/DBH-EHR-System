using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class NotificationServiceTests_MarkAsRead_WithFakeIds_ShouldReturnMessage : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "NotificationService"
    };

    [SkippableFact]
    public async Task MarkAsRead_WithFakeIds_ShouldReturnMessage()
    {
    await AuthenticateAsAdminAsync(NotificationClient);
    var userDid = $"did:dbh:user:{TestSeedData.AdminUserId}";
    var request = new { notificationIds = new[] { Guid.NewGuid() } };
    var response = await PostAsJsonWithRetryAsync(NotificationClient, ApiEndpoints.Notifications.MarkRead(userDid), request);
    
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
