using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Security case: A requester doctor without patient consent must not read EHR document.
/// Expected: 403 Forbidden.
/// </summary>
public class EhrServiceTests_GetEhrDocument_WithNoConsent_ShouldReturnForbidden : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService",
        "ConsentService"
    };

    [SkippableFact]
    public async Task GetEhrDocument_WithNoConsent_ShouldReturnForbidden()
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
                diagnosis = "No-consent access test",
                treatment = "N/A",
                notes = "Ensure unauthorized requester is blocked"
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

        // Create a different doctor account that has no consent from the patient.
        var foreignUsers = await CreateFreshDoctorAndPatientAsync();
        var foreignDoctorUserId = foreignUsers.DoctorUserId;

        var getRequest = EhrClient.CreateRequest(HttpMethod.Get, ApiEndpoints.Ehr.GetDocument(ehrId));
        getRequest.Headers.Add("X-Requester-Id", foreignDoctorUserId.ToString());

        var response = await EhrClient.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
