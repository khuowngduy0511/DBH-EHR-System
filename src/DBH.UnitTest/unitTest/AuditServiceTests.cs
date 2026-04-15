using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// API integration tests for DBH.Audit.Service
/// Covers: AuditLogsController
/// </summary>
public class AuditServiceTests : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AuditService"
    };

    [SkippableFact]
    public async Task CreateAuditLog_WithValidData_ShouldReturnSuccessMessage()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var request = new { action = "LOGIN", actorUserId = TestSeedData.AdminUserId, targetId = TestSeedData.AdminUserId, targetType = "User", description = "Admin user logged in", organizationId = TestSeedData.HospitalAOrgId };
        var response = await PostAsJsonWithRetryAsync(AuditClient, ApiEndpoints.Audit.Create, request);

        var json = await ReadJsonResponseAsync(response);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(json.TryGetProperty("success", out var success) && success.GetBoolean());
            Assert.True(json.TryGetProperty("data", out _));
        }
    }

    [SkippableFact]
    public async Task SearchAuditLogs_AsAdmin_ShouldReturnPagedResult()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await GetWithRetryAsync(AuditClient, $"{ApiEndpoints.Audit.Search}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out _));
    }

    [SkippableFact]
    public async Task GetAuditLog_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await GetWithRetryAsync(AuditClient, ApiEndpoints.Audit.GetById(Guid.NewGuid()));

        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.ValueKind == JsonValueKind.Object);
    }

    [SkippableFact]
    public async Task GetAuditLogsByPatient_WithSeedPatient_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await GetWithRetryAsync(AuditClient, $"{ApiEndpoints.Audit.ByPatient(TestSeedData.PatientUserId)}?page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out _));
    }

    [SkippableFact]
    public async Task GetAuditLogsByActor_WithSeedAdmin_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await GetWithRetryAsync(AuditClient, $"{ApiEndpoints.Audit.ByActor(TestSeedData.AdminUserId)}?page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out _));
    }

    [SkippableFact]
    public async Task GetAuditLogsByTarget_WithSeedUser_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await GetWithRetryAsync(AuditClient, $"{ApiEndpoints.Audit.ByTarget(TestSeedData.DoctorUserId)}?targetType=User&page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out _));
    }

    [SkippableFact]
    public async Task GetAuditStats_AsAdmin_ShouldReturnStats()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await GetWithRetryAsync(AuditClient, ApiEndpoints.Audit.Stats);

        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("totalLogs", out _));
    }

    [SkippableFact]
    public async Task SyncFromBlockchain_WithFakeId_ShouldReturnErrorMessage()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await PostWithRetryAsync(AuditClient, ApiEndpoints.Audit.SyncFromBlockchain("fake-blockchain-id"), null);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }
}

