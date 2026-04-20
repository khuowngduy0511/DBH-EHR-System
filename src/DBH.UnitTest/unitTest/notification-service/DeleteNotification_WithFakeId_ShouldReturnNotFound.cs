using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class NotificationServiceTests_DeleteNotification_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "NotificationService"
    };

    [SkippableFact]
    public async Task DeleteNotification_WithFakeId_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(NotificationClient);
    var response = await DeleteWithRetryAsync(NotificationClient, ApiEndpoints.Notifications.Delete(Guid.NewGuid()));
    
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
