using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class NotificationServiceTests_RegisterDevice_ForSeedUser_ShouldReturnMessage : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "NotificationService"
    };

    [SkippableFact]
    public async Task RegisterDevice_ForSeedUser_ShouldReturnMessage()
    {
    await AuthenticateAsAdminAsync(NotificationClient);
    var request = new { userDid = $"did:dbh:user:{TestSeedData.AdminUserId}", token = $"test-fcm-token-{Guid.NewGuid():N}", platform = "Android", deviceName = "Test Phone" };
    var response = await PostAsJsonWithRetryAsync(NotificationClient, ApiEndpoints.DeviceTokens.Register, request);
    
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
