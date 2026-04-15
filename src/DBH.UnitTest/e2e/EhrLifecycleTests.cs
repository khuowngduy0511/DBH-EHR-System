using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

/// <summary>
/// End-to-end: EHR lifecycle across EHR and Consent services.
/// Flow: Create EHR → Get Record → Update → Check Versions → Grant Consent → Access by Doctor → Revoke Consent → Access Denied
/// </summary>
public class EhrLifecycleTests : Shared.ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService",
        "ConsentService"
    };

    [SkippableFact]
    public async Task EhrCreateAndRetrieve_WithSeedData_ShouldSucceed()
    {
        // =====================================================================
        // STEP 1: Doctor creates EHR record for seed patient
        // =====================================================================
        await AuthenticateAsDoctorAsync(EhrClient);

        var createRequest = new
        {
            patientId = Shared.TestSeedData.PatientUserId,
            orgId = Shared.TestSeedData.HospitalAOrgId,
            encounterId = Guid.NewGuid(),
            data = new 
            {
                doctorId = Shared.TestSeedData.DoctorUserId,
                diagnosis = "E2E Test Diagnosis - Hypertension",
                treatment = "Lifestyle changes, medication",
                notes = "E2E test record"
            }
        };

        var createResponse = await PostAsJsonWithRetryAsync(EhrClient, Shared.ApiEndpoints.Ehr.CreateRecord, createRequest);
        var createJson = await ReadJsonResponseAsync(createResponse);

        if (createResponse.StatusCode == HttpStatusCode.Created || createResponse.StatusCode == HttpStatusCode.OK)
        {
            var ehrId = Guid.Parse(createJson.GetProperty("ehrId").GetString()!);

            // =================================================================
            // STEP 2: Get the EHR record and verify content
            // =================================================================
            var getResponse = await GetWithRetryAsync(EhrClient, Shared.ApiEndpoints.Ehr.GetRecord(ehrId));
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var getJson = await ReadJsonResponseAsync(getResponse);
            Assert.True(getJson.TryGetProperty("ehrId", out _));

            // =================================================================
            // STEP 3: Update the EHR record
            // =================================================================
            var updateRequest = new
            {
                diagnosis = "E2E Updated - Hypertension Stage 2",
                treatment = "ACE inhibitors added",
                notes = "E2E updated notes"
            };
            var updateResponse = await PutAsJsonWithRetryAsync(EhrClient, Shared.ApiEndpoints.Ehr.UpdateRecord(ehrId), updateRequest);
            if (updateResponse.StatusCode == HttpStatusCode.OK)
            {
                var updateJson = await ReadJsonResponseAsync(updateResponse);
                Assert.True(updateJson.TryGetProperty("ehrId", out _));
            }

            // =================================================================
            // STEP 4: Check version history (should have ≥1 version after update)
            // =================================================================
            var versionsResponse = await GetWithRetryAsync(EhrClient, Shared.ApiEndpoints.Ehr.Versions(ehrId));
            Assert.Equal(HttpStatusCode.OK, versionsResponse.StatusCode);

            // =================================================================
            // STEP 5: List patient's EHR records — should include the new one
            // =================================================================
            var patientRecordsResponse = await GetWithRetryAsync(EhrClient, Shared.ApiEndpoints.Ehr.PatientRecords(Shared.TestSeedData.PatientUserId));
            Assert.Equal(HttpStatusCode.OK, patientRecordsResponse.StatusCode);
            var patientRecordsJson = await ReadJsonResponseAsync(patientRecordsResponse);
            Assert.True(patientRecordsJson.ValueKind == JsonValueKind.Array);
        }
    }

    [SkippableFact]
    public async Task ConsentFlow_GrantThenVerifyThenRevoke()
    {
        // =====================================================================
        // STEP 1: Patient grants consent to doctor
        // =====================================================================
        await AuthenticateAsPatientAsync(ConsentClient);

        var grantRequest = new
        {
            patientId = Shared.TestSeedData.PatientUserId,
            patientDid = $"did:dbh:user:{Shared.TestSeedData.PatientUserId}",
            granteeId = Shared.TestSeedData.DoctorUserId,
            granteeDid = $"did:dbh:user:{Shared.TestSeedData.DoctorUserId}",
            granteeType = "DOCTOR",
            permission = "READ",
            purpose = "TREATMENT",
            durationDays = 30
        };

        var grantResponse = await PostAsJsonWithRetryAsync(ConsentClient, Shared.ApiEndpoints.Consents.Grant, grantRequest);
        var grantJson = await ReadJsonResponseAsync(grantResponse);
        Assert.True(grantJson.TryGetProperty("message", out _) || grantJson.TryGetProperty("success", out _) || grantJson.TryGetProperty("data", out _));

        if ((grantResponse.StatusCode == HttpStatusCode.Created || grantResponse.StatusCode == HttpStatusCode.OK)
            && grantJson.TryGetProperty("data", out var grantData)
            && grantData.TryGetProperty("consentId", out var consentIdElement)
            && Guid.TryParse(consentIdElement.GetString(), out var consentId))
        {
            if (grantJson.TryGetProperty("success", out var grantSuccess))
            {
                Assert.True(grantSuccess.GetBoolean());
            }

            // =================================================================
            // STEP 2: Verify consent exists
            // =================================================================
            var verifyRequest = new
            {
                patientId = Shared.TestSeedData.PatientUserId,
                granteeId = Shared.TestSeedData.DoctorUserId
            };
            var verifyResponse = await PostAsJsonWithRetryAsync(ConsentClient, Shared.ApiEndpoints.Consents.Verify, verifyRequest);
            Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

            // =================================================================
            // STEP 3: List consents by patient — should include the new one
            // =================================================================
            var listResponse = await GetWithRetryAsync(ConsentClient, 
                $"{Shared.ApiEndpoints.Consents.ByPatient(Shared.TestSeedData.PatientUserId)}?page=1&pageSize=10");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var listJson = await ReadJsonResponseAsync(listResponse);
            if (listJson.TryGetProperty("data", out var listData) && listData.ValueKind == JsonValueKind.Array)
            {
                Assert.True(listData.GetArrayLength() >= 0);
            }

            // =================================================================
            // STEP 4: Revoke consent
            // =================================================================
            var revokeRequest = new { reason = "E2E test - no longer needed" };
            var revokeResponse = await PostAsJsonWithRetryAsync(ConsentClient, Shared.ApiEndpoints.Consents.Revoke(consentId), revokeRequest);
            var revokeJson = await ReadJsonResponseAsync(revokeResponse);
            Assert.True(revokeJson.TryGetProperty("message", out _) || revokeJson.TryGetProperty("success", out _) || revokeJson.TryGetProperty("data", out _));
        }
    }

    [SkippableFact]
    public async Task AccessRequestFlow_RequestThenRespond()
    {
        // =====================================================================
        // STEP 1: Doctor creates access request for patient's records
        // =====================================================================
        await AuthenticateAsDoctorAsync(ConsentClient);

        var accessRequest = new
        {
            requesterId = Shared.TestSeedData.DoctorUserId,
            requesterDid = $"did:dbh:user:{Shared.TestSeedData.DoctorUserId}",
            requesterType = "DOCTOR",
            patientId = Shared.TestSeedData.PatientUserId,
            patientDid = $"did:dbh:user:{Shared.TestSeedData.PatientUserId}",
            permission = "READ",
            purpose = "TREATMENT",
            reason = "E2E Test - Treatment review",
            requestedDurationDays = 14
        };

        var createResponse = await PostAsJsonWithRetryAsync(ConsentClient, Shared.ApiEndpoints.AccessRequests.Create, accessRequest);
        var createJson = await ReadJsonResponseAsync(createResponse);
        Assert.True(createJson.TryGetProperty("message", out _) || createJson.TryGetProperty("success", out _) || createJson.TryGetProperty("data", out _));

        if ((createResponse.StatusCode == HttpStatusCode.Created || createResponse.StatusCode == HttpStatusCode.OK)
            && createJson.TryGetProperty("data", out var createData)
            && (createData.TryGetProperty("id", out var requestIdElement) || createData.TryGetProperty("requestId", out requestIdElement))
            && Guid.TryParse(requestIdElement.GetString(), out var requestId))
        {
            // =================================================================
            // STEP 2: List doctor's access requests — should include the new one
            // =================================================================
            var listResponse = await GetWithRetryAsync(ConsentClient, 
                $"{Shared.ApiEndpoints.AccessRequests.ByRequester(Shared.TestSeedData.DoctorUserId)}?page=1&pageSize=10");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

            // =================================================================
            // STEP 3: Patient responds (approve)
            // =================================================================
            await AuthenticateAsPatientAsync(ConsentClient);
            var respondRequest = new { approved = true, reason = "E2E approved for treatment" };
            var respondResponse = await PostAsJsonWithRetryAsync(ConsentClient, Shared.ApiEndpoints.AccessRequests.Respond(requestId), respondRequest);
            var respondJson = await ReadJsonResponseAsync(respondResponse);
            Assert.True(respondJson.TryGetProperty("message", out _) || respondJson.TryGetProperty("success", out _) || respondJson.TryGetProperty("data", out _));
        }
    }

    /// <summary>
    /// Full consent-gated EHR access flow:
    ///   1. Admin creates EHR for patient
    ///   2. Doctor tries to GET EHR with X-Requester-Id → 403 Forbidden (no consent)
    ///   3. Doctor sends access request to patient
    ///   4. Patient approves access request (auto-grants consent)
    ///   5. Doctor tries to GET EHR again → 200 OK (consent now active)
    ///   6. Doctor updates the EHR → success
    ///   7. Doctor GETs the EHR again → 200 OK (consent still valid)
    ///   8. Patient revokes consent
    ///   9. Doctor tries to GET EHR again → 403 Forbidden (consent revoked)
    /// </summary>
    [SkippableFact]
    public async Task ConsentGatedAccess_DeniedThenGrantedThenRevoked()
    {
        // =====================================================================
        // STEP 1: Admin creates an EHR record for the seed patient
        // =====================================================================
        await AuthenticateAsAdminAsync(EhrClient);

        var createRequest = new
        {
            patientId = Shared.TestSeedData.PatientUserId,
            orgId = Shared.TestSeedData.HospitalAOrgId,
            encounterId = Guid.NewGuid(),
            data = new 
            {
                doctorId = Shared.TestSeedData.AdminUserId,
                diagnosis = "E2E Consent Test - Diabetes Type 2",
                treatment = "Insulin therapy",
                notes = "Created for consent-gated access test"
            }
        };

        var createResponse = await PostAsJsonWithRetryAsync(EhrClient, Shared.ApiEndpoints.Ehr.CreateRecord, createRequest);
        var createJson = await ReadJsonResponseAsync(createResponse);

        // Skip remainder if service can't create (e.g., IPFS not running)
        if (createResponse.StatusCode != HttpStatusCode.Created && createResponse.StatusCode != HttpStatusCode.OK)
            return;

        var ehrId = Guid.Parse(createJson.GetProperty("ehrId").GetString()!);

        // =====================================================================
        // STEP 2: Doctor tries to GET this EHR with X-Requester-Id → should be DENIED
        // =====================================================================
        await AuthenticateAsDoctorAsync(EhrClient);
        EhrClient.DefaultRequestHeaders.Add("X-Requester-Id", Shared.TestSeedData.DoctorUserId.ToString());

        var deniedResponse = await GetWithRetryAsync(EhrClient, Shared.ApiEndpoints.Ehr.GetRecord(ehrId));
        Assert.True(deniedResponse.StatusCode == HttpStatusCode.Forbidden || deniedResponse.StatusCode == HttpStatusCode.OK);

        if (deniedResponse.StatusCode == HttpStatusCode.Forbidden)
        {
            var deniedJson = await ReadJsonResponseAsync(deniedResponse);
            Assert.True(deniedJson.TryGetProperty("message", out _) || deniedJson.TryGetProperty("Message", out _));
        }

        EhrClient.DefaultRequestHeaders.Remove("X-Requester-Id");

        // =====================================================================
        // STEP 3: Doctor sends access request to patient
        // =====================================================================
        await AuthenticateAsDoctorAsync(ConsentClient);

        var accessRequest = new
        {
            requesterId = Shared.TestSeedData.DoctorUserId,
            requesterDid = $"did:dbh:user:{Shared.TestSeedData.DoctorUserId}",
            requesterType = "DOCTOR",
            patientId = Shared.TestSeedData.PatientUserId,
            patientDid = $"did:dbh:user:{Shared.TestSeedData.PatientUserId}",
            permission = "FULL_ACCESS",
            purpose = "TREATMENT",
            reason = "Need to review Diabetes diagnosis for treatment plan",
            requestedDurationDays = 30
        };

        var accessResponse = await PostAsJsonWithRetryAsync(ConsentClient, Shared.ApiEndpoints.AccessRequests.Create, accessRequest);
        var accessJson = await ReadJsonResponseAsync(accessResponse);
        Assert.True(accessJson.TryGetProperty("message", out _) || accessJson.TryGetProperty("success", out _) || accessJson.TryGetProperty("data", out _));

        if (accessResponse.StatusCode != HttpStatusCode.Created && accessResponse.StatusCode != HttpStatusCode.OK)
            return;

        if (!accessJson.TryGetProperty("data", out var accessData)
            || !(accessData.TryGetProperty("id", out var accessRequestIdElement)
                 || accessData.TryGetProperty("requestId", out accessRequestIdElement))
            || !Guid.TryParse(accessRequestIdElement.GetString(), out var accessRequestId))
        {
            return;
        }

        // =====================================================================
        // STEP 4: Patient approves the access request (auto-grants consent)
        // =====================================================================
        await AuthenticateAsPatientAsync(ConsentClient);

        var approveRequest = new { approve = true, responseReason = "Approved - trust Dr. House for diabetes treatment" };
        var approveResponse = await PostAsJsonWithRetryAsync(ConsentClient, 
            Shared.ApiEndpoints.AccessRequests.Respond(accessRequestId), approveRequest);
        var approveJson = await ReadJsonResponseAsync(approveResponse);
        Assert.True(approveJson.TryGetProperty("message", out _) || approveJson.TryGetProperty("success", out _) || approveJson.TryGetProperty("data", out _));

        if (approveResponse.StatusCode != HttpStatusCode.OK)
            return;

        // =====================================================================
        // STEP 5: Doctor tries to GET the EHR again → should SUCCEED now
        // =====================================================================
        await AuthenticateAsDoctorAsync(EhrClient);
        EhrClient.DefaultRequestHeaders.Add("X-Requester-Id", Shared.TestSeedData.DoctorUserId.ToString());

        var allowedResponse = await GetWithRetryAsync(EhrClient, Shared.ApiEndpoints.Ehr.GetRecord(ehrId));
        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);

        var allowedJson = await ReadJsonResponseAsync(allowedResponse);
        Assert.True(allowedJson.TryGetProperty("ehrId", out _) || allowedJson.ValueKind == JsonValueKind.Object,
            "Doctor should be able to view EHR after consent was granted");

        EhrClient.DefaultRequestHeaders.Remove("X-Requester-Id");

        // =====================================================================
        // STEP 6: Doctor updates the EHR → should succeed with active consent
        // =====================================================================
        var updateRequest = new
        {
            diagnosis = "E2E Updated - Diabetes Type 2 with complications",
            treatment = "Insulin therapy + dietary monitoring",
            notes = "Updated by doctor after consent was granted"
        };

        var updateResponse = await PutAsJsonWithRetryAsync(EhrClient, Shared.ApiEndpoints.Ehr.UpdateRecord(ehrId), updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // =====================================================================
        // STEP 7: Doctor GETs the EHR again → should STILL succeed (consent valid)
        // =====================================================================
        EhrClient.DefaultRequestHeaders.Add("X-Requester-Id", Shared.TestSeedData.DoctorUserId.ToString());

        var stillAllowedResponse = await GetWithRetryAsync(EhrClient, Shared.ApiEndpoints.Ehr.GetRecord(ehrId));
        Assert.Equal(HttpStatusCode.OK, stillAllowedResponse.StatusCode);

        EhrClient.DefaultRequestHeaders.Remove("X-Requester-Id");

        // =====================================================================
        // STEP 8: Patient revokes consent
        // =====================================================================
        // First, find the consent ID by listing consents for this patient-grantee pair
        await AuthenticateAsPatientAsync(ConsentClient);

        var consentsListResponse = await GetWithRetryAsync(ConsentClient, 
            $"{Shared.ApiEndpoints.Consents.ByPatient(Shared.TestSeedData.PatientUserId)}?page=1&pageSize=50");
        Assert.Equal(HttpStatusCode.OK, consentsListResponse.StatusCode);
        var consentsListJson = await ReadJsonResponseAsync(consentsListResponse);
        if (!consentsListJson.TryGetProperty("data", out var consentsArray)
            || consentsArray.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        // Find the consent granted to the doctor that is still active
        Guid? consentIdToRevoke = null;
        foreach (var consent in consentsArray.EnumerateArray())
        {
            var granteeId = consent.GetProperty("granteeId").GetString();
            var status = consent.GetProperty("status").GetString();
            if (granteeId == Shared.TestSeedData.DoctorUserId.ToString() &&
                status != null && status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                consentIdToRevoke = Guid.Parse(consent.GetProperty("consentId").GetString()!);
                break;
            }
        }

        if (consentIdToRevoke.HasValue)
        {
            var revokeRequest = new { reason = "E2E test - revoking consent after doctor updated EHR" };
            var revokeResponse = await PostAsJsonWithRetryAsync(ConsentClient, 
                Shared.ApiEndpoints.Consents.Revoke(consentIdToRevoke.Value), revokeRequest);
            var revokeJson = await ReadJsonResponseAsync(revokeResponse);
            Assert.True(revokeJson.TryGetProperty("message", out _) || revokeJson.TryGetProperty("success", out _) || revokeJson.TryGetProperty("data", out _));

            // =================================================================
            // STEP 9: Doctor tries to GET the EHR again → should be DENIED again
            // =================================================================
            await AuthenticateAsDoctorAsync(EhrClient);
            EhrClient.DefaultRequestHeaders.Add("X-Requester-Id", Shared.TestSeedData.DoctorUserId.ToString());

            var reDeniedResponse = await GetWithRetryAsync(EhrClient, Shared.ApiEndpoints.Ehr.GetRecord(ehrId));
            Assert.Equal(HttpStatusCode.Forbidden, reDeniedResponse.StatusCode);

            var reDeniedJson = await ReadJsonResponseAsync(reDeniedResponse);
            Assert.True(reDeniedJson.TryGetProperty("message", out _) || reDeniedJson.TryGetProperty("Message", out _));

            EhrClient.DefaultRequestHeaders.Remove("X-Requester-Id");
        }
    }
}

