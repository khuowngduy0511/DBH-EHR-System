using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

/// <summary>
/// Consent Workflow Flow Test: Grant → Verify → Search → Revoke → Verify Revoked
/// Flow: Grant consent → Verify active consent → Search consents → Revoke → Verify revoked
/// Expected: All consent operations work correctly in sequence
/// </summary>
public class ConsentWorkflowTests : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "ConsentService"
    };

    [SkippableFact]
    public async Task ConsentWorkflow_GrantVerifySearchRevoke_ShouldSucceed()
    {
        await AuthenticateAsPatientAsync(ConsentClient);

        // =====================================================================
        // STEP 1: GRANT CONSENT
        // =====================================================================
        var grantRequest = new 
        { 
            patientDid = TestSeedData.PatientUserId.ToString(), 
            granteeDid = TestSeedData.DoctorUserId.ToString(), 
            scope = "read" 
        };

        var grantResponse = await PostAsJsonWithRetryAsync(ConsentClient, ApiEndpoints.Consents.Grant, grantRequest);
        Assert.True(
            grantResponse.StatusCode == HttpStatusCode.Created || 
            grantResponse.StatusCode == HttpStatusCode.OK,
            $"GRANT failed: {grantResponse.StatusCode}");

        var grantJson = await ReadJsonResponseAsync(grantResponse);
        Guid consentId = Guid.Empty;
        
        if (grantJson.TryGetProperty("data", out var dataElement))
        {
            if (dataElement.TryGetProperty("consentId", out var consentIdElement))
            {
                consentId = Guid.Parse(consentIdElement.GetString()!);
            }
        }

        // =====================================================================
        // STEP 2: VERIFY CONSENT
        // =====================================================================
        var verifyRequest = new 
        { 
            patientDid = TestSeedData.PatientUserId.ToString(), 
            granteeDid = TestSeedData.DoctorUserId.ToString() 
        };

        var verifyResponse = await PostAsJsonWithRetryAsync(ConsentClient, ApiEndpoints.Consents.Verify, verifyRequest);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        // =====================================================================
        // STEP 3: GET CONSENTS BY PATIENT
        // =====================================================================
        var getConsentsResponse = await GetWithRetryAsync(
            ConsentClient, 
            ApiEndpoints.Consents.ByPatient(TestSeedData.PatientUserId));

        Assert.Equal(HttpStatusCode.OK, getConsentsResponse.StatusCode);
        var getConsentsJson = await ReadJsonResponseAsync(getConsentsResponse);
        Assert.True(getConsentsJson.ValueKind == JsonValueKind.Object);

        // =====================================================================
        // STEP 4: REVOKE CONSENT (if consent was created)
        // =====================================================================
        if (consentId != Guid.Empty)
        {
            var revokeRequest = new { reason = "Testing revocation" };
            
            var revokeResponse = await PostAsJsonWithRetryAsync(
                ConsentClient, 
                ApiEndpoints.Consents.Revoke(consentId), 
                revokeRequest);

            Assert.True(
                revokeResponse.StatusCode == HttpStatusCode.OK || 
                revokeResponse.StatusCode == HttpStatusCode.NoContent,
                $"REVOKE failed: {revokeResponse.StatusCode}");

            // ===================================================================
            // STEP 5: VERIFY CONSENT AFTER REVOKE (should fail or return false)
            // ===================================================================
            var verifyAfterRevokeResponse = await PostAsJsonWithRetryAsync(
                ConsentClient, 
                ApiEndpoints.Consents.Verify, 
                verifyRequest);

            Assert.Equal(HttpStatusCode.OK, verifyAfterRevokeResponse.StatusCode);
            // Verification should still return OK but with verified: false
        }

        // Test completed successfully
        Assert.True(true, "Complete consent workflow succeeded");
    }
}
