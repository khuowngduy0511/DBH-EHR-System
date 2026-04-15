using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuditServiceTests_GetAuditLogsByTarget_WithSeedUser_ShouldReturnResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AuditService"
    };

    [SkippableFact]
    public async Task GetAuditLogsByTarget_WithSeedUser_ShouldReturnResult()
    {
    await AuthenticateAsAdminAsync(AuditClient);
    var response = await GetWithRetryAsync(AuditClient, $"{ApiEndpoints.Audit.ByTarget(TestSeedData.DoctorUserId)}?targetType=User&page=1&pageSize=50");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.TryGetProperty("data", out _));
    }
}
