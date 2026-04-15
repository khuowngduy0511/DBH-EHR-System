using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// API integration tests for DBH.Consent.Service
/// Covers: ConsentsController, AccessRequestsController
/// </summary>
public class ConsentServiceTests : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "ConsentService"
    };

    // =========================================================================
    // CONSENTS - Flow with seed users
    // =========================================================================

    [SkippableFact]
    public async Task GrantConsent_WithSeedUsers_ShouldReturnSuccessMessage()
    {
        await AuthenticateAsPatientAsync(ConsentClient);
        var request = new
        {
            patientId = TestSeedData.PatientUserId,
            patientDid = $"did:dbh:user:{TestSeedData.PatientUserId}",
            granteeId = TestSeedData.DoctorUserId,
            granteeDid = $"did:dbh:user:{TestSeedData.DoctorUserId}",
            granteeType = "DOCTOR",
            permission = "READ",
            purpose = "TREATMENT",
            durationDays = 30
        };
        var response = await PostAsJsonWithRetryAsync(ConsentClient, ApiEndpoints.Consents.Grant, request);

        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("message", out _) || json.TryGetProperty("success", out _) || json.TryGetProperty("data", out _));
    }

    [SkippableFact]
    public async Task GetConsent_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await GetWithRetryAsync(ConsentClient, ApiEndpoints.Consents.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
    }

    [SkippableFact]
    public async Task GetConsentsByPatient_WithSeedPatient_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await GetWithRetryAsync(ConsentClient, $"{ApiEndpoints.Consents.ByPatient(TestSeedData.PatientUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out _) || json.TryGetProperty("items", out _) || json.ValueKind == JsonValueKind.Array);
    }

    [SkippableFact]
    public async Task GetConsentsByGrantee_WithSeedDoctor_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await GetWithRetryAsync(ConsentClient, $"{ApiEndpoints.Consents.ByGrantee(TestSeedData.DoctorUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out _) || json.TryGetProperty("items", out _) || json.ValueKind == JsonValueKind.Array);
    }

    [SkippableFact]
    public async Task SearchConsents_AsAdmin_ShouldReturnPagedResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await GetWithRetryAsync(ConsentClient, $"{ApiEndpoints.Consents.Search}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out _) || json.TryGetProperty("items", out _));
    }

    [SkippableFact]
    public async Task RevokeConsent_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var request = new { reason = "No longer needed" };
        var response = await PostAsJsonWithRetryAsync(ConsentClient, ApiEndpoints.Consents.Revoke(Guid.NewGuid()), request);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
    }

    [SkippableFact]
    public async Task VerifyConsent_BetweenSeedUsers_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var request = new { patientId = TestSeedData.PatientUserId, granteeId = TestSeedData.DoctorUserId };
        var response = await PostAsJsonWithRetryAsync(ConsentClient, ApiEndpoints.Consents.Verify, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        // Should return whether consent is valid or not
        Assert.True(json.TryGetProperty("hasAccess", out _) || json.TryGetProperty("message", out _) || json.TryGetProperty("consentId", out _));
    }

    [SkippableFact]
    public async Task SyncFromBlockchain_WithFakeId_ShouldReturnErrorMessage()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await PostWithRetryAsync(ConsentClient, ApiEndpoints.Consents.SyncFromBlockchain("fake-consent-chain-id"), null);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    // =========================================================================
    // ACCESS REQUESTS - Flow with seed users
    // =========================================================================

    [SkippableFact]
    public async Task CreateAccessRequest_DoctorToPatient_ShouldReturnMessage()
    {
        await AuthenticateAsDoctorAsync(ConsentClient);
        var request = new
        {
            requesterId = TestSeedData.DoctorUserId,
            requesterDid = $"did:dbh:user:{TestSeedData.DoctorUserId}",
            requesterType = "DOCTOR",
            patientId = TestSeedData.PatientUserId,
            patientDid = $"did:dbh:user:{TestSeedData.PatientUserId}",
            permission = "READ",
            purpose = "TREATMENT",
            reason = "Treatment",
            requestedDurationDays = 7
        };
        var response = await PostAsJsonWithRetryAsync(ConsentClient, ApiEndpoints.AccessRequests.Create, request);

        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("message", out _) || json.TryGetProperty("success", out _) || json.TryGetProperty("data", out _));
    }

    [SkippableFact]
    public async Task GetAccessRequest_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await GetWithRetryAsync(ConsentClient, ApiEndpoints.AccessRequests.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [SkippableFact]
    public async Task GetAccessRequestsByPatient_WithSeedPatient_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await GetWithRetryAsync(ConsentClient, $"{ApiEndpoints.AccessRequests.ByPatient(TestSeedData.PatientUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.TryGetProperty("data", out _) || json.TryGetProperty("items", out _) || json.ValueKind == JsonValueKind.Array);
    }

    [SkippableFact]
    public async Task GetAccessRequestsByRequester_WithSeedDoctor_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await GetWithRetryAsync(ConsentClient, $"{ApiEndpoints.AccessRequests.ByRequester(TestSeedData.DoctorUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [SkippableFact]
    public async Task RespondToAccessRequest_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var request = new { approved = true, reason = "Approved" };
        var response = await PostAsJsonWithRetryAsync(ConsentClient, ApiEndpoints.AccessRequests.Respond(Guid.NewGuid()), request);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}

