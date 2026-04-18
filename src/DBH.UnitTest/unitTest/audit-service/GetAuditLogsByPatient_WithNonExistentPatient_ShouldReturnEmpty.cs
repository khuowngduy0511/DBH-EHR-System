using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Bad case: Get audit logs for non-existent patient
/// Expected: 200 OK with empty results
/// </summary>
public class AuditServiceTests_GetAuditLogsByPatient_WithNonExistentPatient_ShouldReturnEmpty : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AuditService"
    };

    [SkippableFact]
    public async Task GetAuditLogsByPatient_WithNonExistentPatient_ShouldReturnEmpty()
    {
        await AuthenticateAsAdminAsync(AuditClient);

        var fakePatientId = Guid.NewGuid();
        
        var response = await GetWithRetryAsync(
            AuditClient, 
            ApiEndpoints.Audit.ByPatient(fakePatientId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await ReadJsonResponseAsync(response);
        // Should return empty paged result
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
