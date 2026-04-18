using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Good case: Get audit logs by actor/user
/// Expected: 200 OK with paged list
/// </summary>
public class AuditServiceTests_GetAuditLogsByActor_WithValidActor_ShouldReturnLogs : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AuditService"
    };

    [SkippableFact]
    public async Task GetAuditLogsByActor_WithValidActor_ShouldReturnLogs()
    {
        await AuthenticateAsAdminAsync(AuditClient);

        var response = await GetWithRetryAsync(
            AuditClient, 
            ApiEndpoints.Audit.ByActor(TestSeedData.DoctorUserId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
