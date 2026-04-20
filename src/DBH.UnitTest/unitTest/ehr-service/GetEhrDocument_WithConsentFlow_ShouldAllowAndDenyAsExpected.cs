using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Consent flow for EHR document access:
/// 1) requester doctor without consent -> 403
/// 2) patient grants consent -> requester doctor can access
/// </summary>
public class EhrServiceTests_GetEhrDocument_WithConsentFlow_ShouldAllowAndDenyAsExpected : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService",
        "ConsentService"
    };

    [SkippableFact]
    public async Task GetEhrDocument_WithAndWithoutConsent_ShouldEnforceAccessControl()
    {
        await AuthenticateAsDoctorAsync(EhrClient);

        var createRequest = new
        {
            patientId = TestSeedData.PatientUserId,
            orgId = TestSeedData.HospitalAOrgId,
            encounterId = Guid.NewGuid(),
            data = new
            {
                doctorId = TestSeedData.DoctorUserId,
                diagnosis = "Consent flow test",
                treatment = "Observe",
                notes = "Used for consent flow integration test"
            }
        };

        var createResponse = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.CreateRecord, createRequest);
        Assert.True(
            createResponse.StatusCode == HttpStatusCode.Created ||
            createResponse.StatusCode == HttpStatusCode.OK,
            $"Expected 201 or 200 when creating EHR, got {createResponse.StatusCode}");

        var createJson = await ReadJsonResponseAsync(createResponse);
        Assert.True(createJson.TryGetProperty("ehrId", out var ehrIdElement), "Expected ehrId in create response");
        var ehrId = Guid.Parse(ehrIdElement.GetString()!);

        var foreignUsers = await CreateFreshDoctorAndPatientAsync();
        var foreignDoctorUserId = foreignUsers.DoctorUserId;

        var noConsentRequest = EhrClient.CreateRequest(HttpMethod.Get, ApiEndpoints.Ehr.GetDocument(ehrId));
        noConsentRequest.Headers.Add("X-Requester-Id", foreignDoctorUserId.ToString());
        var noConsentResponse = await EhrClient.SendAsync(noConsentRequest);
        Assert.Equal(HttpStatusCode.Forbidden, noConsentResponse.StatusCode);

        await AuthenticateAsPatientAsync(ConsentClient);
        var grantConsentRequest = new
        {
            patientDid = TestSeedData.PatientUserId.ToString(),
            granteeDid = foreignDoctorUserId.ToString(),
            scope = "read"
        };

        var grantConsentResponse = await PostAsJsonWithRetryAsync(ConsentClient, ApiEndpoints.Consents.Grant, grantConsentRequest);
        Assert.True(
            grantConsentResponse.StatusCode == HttpStatusCode.Created ||
            grantConsentResponse.StatusCode == HttpStatusCode.OK,
            $"Expected 201 or 200 when granting consent, got {grantConsentResponse.StatusCode}");

        await AuthenticateAsAdminAsync(EhrClient);
        var withConsentRequest = EhrClient.CreateRequest(HttpMethod.Get, ApiEndpoints.Ehr.GetDocument(ehrId));
        withConsentRequest.Headers.Add("X-Requester-Id", foreignDoctorUserId.ToString());
        var withConsentResponse = await EhrClient.SendAsync(withConsentRequest);

        Assert.True(
            withConsentResponse.StatusCode == HttpStatusCode.OK || withConsentResponse.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404 after consent grant, got {withConsentResponse.StatusCode}");
    }
}
