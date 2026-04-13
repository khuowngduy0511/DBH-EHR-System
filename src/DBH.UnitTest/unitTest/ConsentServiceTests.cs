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
    // =========================================================================
    // CONSENTS - Flow with seed users
    // =========================================================================

    [Fact]
    public async Task GrantConsent_WithSeedUsers_ShouldReturnSuccessMessage()
    {
        await AuthenticateAsPatientAsync(ConsentClient);
        var request = new { patientId = TestSeedData.PatientUserId, granteeId = TestSeedData.DoctorUserId, granteeType = "Doctor", scope = "read", expiresAt = DateTime.UtcNow.AddDays(30).ToString("o") };
        var response = await ConsentClient.PostAsJsonAsync(ApiEndpoints.Consents.Grant, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task GetConsent_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await ConsentClient.GetAsync(ApiEndpoints.Consents.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetConsentsByPatient_WithSeedPatient_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await ConsentClient.GetAsync($"{ApiEndpoints.Consents.ByPatient(TestSeedData.PatientUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetConsentsByGrantee_WithSeedDoctor_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await ConsentClient.GetAsync($"{ApiEndpoints.Consents.ByGrantee(TestSeedData.DoctorUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task SearchConsents_AsAdmin_ShouldReturnPagedResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await ConsentClient.GetAsync($"{ApiEndpoints.Consents.Search}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task RevokeConsent_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var request = new { reason = "No longer needed" };
        var response = await ConsentClient.PostAsJsonAsync(ApiEndpoints.Consents.Revoke(Guid.NewGuid()), request);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task VerifyConsent_BetweenSeedUsers_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var request = new { patientId = TestSeedData.PatientUserId, granteeId = TestSeedData.DoctorUserId };
        var response = await ConsentClient.PostAsJsonAsync(ApiEndpoints.Consents.Verify, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        // Should return whether consent is valid or not
        Assert.True(json.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task SyncFromBlockchain_WithFakeId_ShouldReturnErrorMessage()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await ConsentClient.PostAsync(ApiEndpoints.Consents.SyncFromBlockchain("fake-consent-chain-id"), null);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    // =========================================================================
    // ACCESS REQUESTS - Flow with seed users
    // =========================================================================

    [Fact]
    public async Task CreateAccessRequest_DoctorToPatient_ShouldReturnMessage()
    {
        await AuthenticateAsDoctorAsync(ConsentClient);
        var request = new { requesterId = TestSeedData.DoctorUserId, patientId = TestSeedData.PatientUserId, purpose = "Treatment", scope = "read", requestedDuration = 7 };
        var response = await ConsentClient.PostAsJsonAsync(ApiEndpoints.AccessRequests.Create, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task GetAccessRequest_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await ConsentClient.GetAsync(ApiEndpoints.AccessRequests.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAccessRequestsByPatient_WithSeedPatient_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await ConsentClient.GetAsync($"{ApiEndpoints.AccessRequests.ByPatient(TestSeedData.PatientUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetAccessRequestsByRequester_WithSeedDoctor_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var response = await ConsentClient.GetAsync($"{ApiEndpoints.AccessRequests.ByRequester(TestSeedData.DoctorUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RespondToAccessRequest_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(ConsentClient);
        var request = new { approved = true, reason = "Approved" };
        var response = await ConsentClient.PostAsJsonAsync(ApiEndpoints.AccessRequests.Respond(Guid.NewGuid()), request);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}

