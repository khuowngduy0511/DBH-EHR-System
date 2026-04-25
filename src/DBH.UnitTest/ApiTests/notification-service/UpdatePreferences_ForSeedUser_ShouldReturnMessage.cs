using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class NotificationServiceTests_UpdatePreferences_ForSeedUser_ShouldReturnMessage : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "NotificationService"
    };

    [SkippableFact]
    public async Task UpdatePreferences_ForSeedUser_ShouldReturnMessage()
    {
    await AuthenticateAsAdminAsync(NotificationClient);
    var userDid = $"did:dbh:user:{TestSeedData.AdminUserId}";
    var request = new { emailEnabled = true, pushEnabled = true, smsEnabled = false };
    var response = await PutAsJsonWithRetryAsync(NotificationClient, ApiEndpoints.Preferences.Update(userDid), request);
    
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
