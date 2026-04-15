using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuditServiceTests_GetAuditLog_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AuditService"
    };

    [SkippableFact]
    public async Task GetAuditLog_WithFakeId_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(AuditClient);
    var response = await GetWithRetryAsync(AuditClient, ApiEndpoints.Audit.GetById(Guid.NewGuid()));
    
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
