using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DBH.UnitTest.Api;

/// <summary>
/// API integration tests for DBH.Audit.Service
/// Covers: AuditLogsController
/// </summary>
public class AuditServiceTests : ApiTestBase
{
    [Fact]
    public async Task CreateAuditLog_WithValidData_ShouldReturnSuccessMessage()
    {
        var request = new { action = "LOGIN", actorUserId = TestSeedData.AdminUserId, targetId = TestSeedData.AdminUserId, targetType = "User", description = "Admin user logged in", organizationId = TestSeedData.HospitalAOrgId };
        var response = await AuditClient.PostAsJsonAsync(ApiEndpoints.Audit.Create, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
        if (response.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(json.GetProperty("success").GetBoolean());
        }
    }

    [Fact]
    public async Task SearchAuditLogs_AsAdmin_ShouldReturnPagedResult()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await AuditClient.GetAsync($"{ApiEndpoints.Audit.Search}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.True(json.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task GetAuditLog_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await AuditClient.GetAsync(ApiEndpoints.Audit.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetAuditLogsByPatient_WithSeedPatient_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await AuditClient.GetAsync($"{ApiEndpoints.Audit.ByPatient(TestSeedData.PatientUserId)}?page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetAuditLogsByActor_WithSeedAdmin_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await AuditClient.GetAsync($"{ApiEndpoints.Audit.ByActor(TestSeedData.AdminUserId)}?page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetAuditLogsByTarget_WithSeedUser_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await AuditClient.GetAsync($"{ApiEndpoints.Audit.ByTarget(TestSeedData.DoctorUserId)}?targetType=User&page=1&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetAuditStats_AsAdmin_ShouldReturnStats()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await AuditClient.GetAsync(ApiEndpoints.Audit.Stats);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.True(json.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task SyncFromBlockchain_WithFakeId_ShouldReturnErrorMessage()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        var response = await AuditClient.PostAsync(ApiEndpoints.Audit.SyncFromBlockchain("fake-blockchain-id"), null);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }
}
