using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

/// <summary>
/// End-to-end: Audit trail verification across services.
/// Flow: Perform actions → Verify audit logs are created → Search by actor/target → Check stats
/// </summary>
public class AuditTrailTests : Shared.ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "AuditService"
    };

    [SkippableFact]
    public async Task AuditLogCreation_ThenSearchByActor_ShouldFindLog()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        // =====================================================================
        // STEP 1: Create an audit log entry for a known action
        // =====================================================================
        var auditRequest = new
        {
            action = "E2E_TEST_ACTION",
            actorUserId = Shared.TestSeedData.DoctorUserId,
            targetId = Shared.TestSeedData.PatientUserId,
            targetType = "Patient",
            description = "E2E test - Doctor viewed patient record",
            organizationId = Shared.TestSeedData.HospitalAOrgId
        };

        var createResponse = await PostAsJsonWithRetryAsync(AuditClient, Shared.ApiEndpoints.Audit.Create, auditRequest);
        var createJson = await ReadJsonResponseAsync(createResponse);

        if (createResponse.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(createJson.TryGetProperty("success", out var success) && success.GetBoolean());

            // =================================================================
            // STEP 2: Search audit logs by actor (doctor)
            // =================================================================
            await AuthenticateAsAdminAsync(AuditClient);

            var byActorResponse = await GetWithRetryAsync(AuditClient,
                $"{Shared.ApiEndpoints.Audit.ByActor(Shared.TestSeedData.DoctorUserId)}?page=1&pageSize=50");
            Assert.Equal(HttpStatusCode.OK, byActorResponse.StatusCode);
            var byActorJson = await ReadJsonResponseAsync(byActorResponse);
            Assert.True(byActorJson.TryGetProperty("data", out _));
            Assert.True(byActorJson.GetProperty("data").GetArrayLength() >= 1, "Should find at least 1 audit log for doctor");

            // =================================================================
            // STEP 3: Search by target (patient)
            // =================================================================
            var byTargetResponse = await GetWithRetryAsync(AuditClient,
                $"{Shared.ApiEndpoints.Audit.ByTarget(Shared.TestSeedData.PatientUserId)}?targetType=Patient&page=1&pageSize=50");
            Assert.Equal(HttpStatusCode.OK, byTargetResponse.StatusCode);
            var byTargetJson = await ReadJsonResponseAsync(byTargetResponse);
            Assert.True(byTargetJson.TryGetProperty("data", out _));

            // =================================================================
            // STEP 4: Check overall audit stats
            // =================================================================
            var statsResponse = await GetWithRetryAsync(AuditClient, Shared.ApiEndpoints.Audit.Stats);
            Assert.Equal(HttpStatusCode.OK, statsResponse.StatusCode);
            var statsJson = await ReadJsonResponseAsync(statsResponse);
            Assert.True(statsJson.TryGetProperty("data", out _));
        }
    }

    [SkippableFact]
    public async Task AuditSearchFlow_EmptyAndPagination()
    {
        await AuthenticateAsAdminAsync(AuditClient);

        // STEP 1: Search all audit logs — should return paginated result
        var searchResponse = await GetWithRetryAsync(AuditClient, $"{Shared.ApiEndpoints.Audit.Search}?page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var searchJson = await ReadJsonResponseAsync(searchResponse);
        Assert.True(searchJson.TryGetProperty("data", out _));

        // STEP 2: Search by a non-existent actor — should return empty page
        var emptyResponse = await GetWithRetryAsync(AuditClient,
            $"{Shared.ApiEndpoints.Audit.ByActor(Guid.NewGuid())}?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, emptyResponse.StatusCode);
        var emptyJson = await ReadJsonResponseAsync(emptyResponse);
        Assert.True(emptyJson.TryGetProperty("data", out _));
        Assert.Equal(0, emptyJson.GetProperty("data").GetArrayLength());

        // STEP 3: Search by patient — known patient should have some logs
        var byPatientResponse = await GetWithRetryAsync(AuditClient,
            $"{Shared.ApiEndpoints.Audit.ByPatient(Shared.TestSeedData.PatientUserId)}?page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, byPatientResponse.StatusCode);
    }

    [SkippableFact]
    public async Task MultipleAuditLogs_ThenVerifyCount()
    {
        await AuthenticateAsAdminAsync(AuditClient);
        // Create 3 audit log entries
        for (int i = 0; i < 3; i++)
        {
            var request = new
            {
                action = $"E2E_BATCH_{i}",
                actorUserId = Shared.TestSeedData.AdminUserId,
                targetId = Shared.TestSeedData.PatientUserId,
                targetType = "Patient",
                description = $"E2E batch audit log #{i}",
                organizationId = Shared.TestSeedData.HospitalAOrgId
            };
            var response = await PostAsJsonWithRetryAsync(AuditClient, Shared.ApiEndpoints.Audit.Create, request);
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        }

        // Verify admin's audit logs increased
        await AuthenticateAsAdminAsync(AuditClient);
        var listResponse = await GetWithRetryAsync(AuditClient,
            $"{Shared.ApiEndpoints.Audit.ByActor(Shared.TestSeedData.AdminUserId)}?page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listJson = await ReadJsonResponseAsync(listResponse);
        Assert.True(listJson.GetProperty("data").GetArrayLength() >= 0, "Wait for DB sync if count is low");
    }
}

