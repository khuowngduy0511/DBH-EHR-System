using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuditServiceTests_SearchAuditLogs_AsAdmin_ShouldReturnPagedResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AuditService"
    };

    [SkippableFact]
    public async Task SearchAuditLogs_AsAdmin_ShouldReturnPagedResult()
    {
    await AuthenticateAsAdminAsync(AuditClient);
    var response = await GetWithRetryAsync(AuditClient, $"{ApiEndpoints.Audit.Search}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.TryGetProperty("data", out _));
    }
}
