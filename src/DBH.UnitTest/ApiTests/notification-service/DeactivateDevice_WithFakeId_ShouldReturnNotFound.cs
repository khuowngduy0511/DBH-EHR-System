using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class NotificationServiceTests_DeactivateDevice_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "NotificationService"
    };

    [SkippableFact]
    public async Task DeactivateDevice_WithFakeId_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(NotificationClient);
    var response = await DeleteWithRetryAsync(NotificationClient, ApiEndpoints.DeviceTokens.Deactivate(Guid.NewGuid()));
    
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
