using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class NotificationServiceTests_MarkAllNotificationsAsRead_WithFakeUserId_ShouldReturnResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "NotificationService" };

    [SkippableFact]
    public async Task MarkAllNotificationsAsRead_WithFakeUserId_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(AuthClient);

        var fakeUserDid = "did:dbh:user:00000000-0000-0000-0000-000000000000";
        var url = ApiEndpoints.Notifications.MarkAllRead(fakeUserDid);
        
        var response = await PostWithRetryAsync(AuthClient, url, null);
        
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
