using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class NotificationServiceTests_SendNotification_ToSeedUser_ShouldReturnMessage : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "NotificationService"
    };

    [SkippableFact]
    public async Task SendNotification_ToSeedUser_ShouldReturnMessage()
    {
    await AuthenticateAsAdminAsync(NotificationClient);
    var request = new { recipientDid = $"did:dbh:user:{TestSeedData.PatientUserId}", title = "Test Notification", body = "This is a test", type = "System" };
    var response = await PostAsJsonWithRetryAsync(NotificationClient, ApiEndpoints.Notifications.Send, request);
    
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
