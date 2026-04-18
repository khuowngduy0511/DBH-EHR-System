using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Bad case: Try to get audit stats as non-admin
/// Expected: 403 Forbidden
/// </summary>
public class AuditServiceTests_GetAuditStats_AsNonAdmin_ShouldReturnForbidden : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AuditService"
    };

    [SkippableFact]
    public async Task GetAuditStats_AsDoctor_ShouldReturnForbidden()
    {
        await AuthenticateAsDoctorAsync(AuditClient);

        var response = await GetWithRetryAsync(AuditClient, ApiEndpoints.Audit.Stats);

        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden || 
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"Expected 403 or 401, got {response.StatusCode}");
    }
}
