using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class ConsentServiceTests_VerifyConsent_BetweenSeedUsers_ShouldReturnResult : ApiTestBase
{
    protected override bool UseDynamicDoctorPatientUsers => true;

    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "ConsentService"
    };

    [SkippableFact]
    public async Task VerifyConsent_BetweenSeedUsers_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(ConsentClient);

        var request = new
        {
            patientId = TestSeedData.PatientUserId,
            granteeId = TestSeedData.DoctorUserId
        };

        var response = await PostAsJsonWithRetryAsync(ConsentClient, ApiEndpoints.Consents.Verify, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await ReadJsonResponseAsync(response);

        // Should return whether consent is valid or not
        Assert.True(
            json.TryGetProperty("hasAccess", out _) ||
            json.TryGetProperty("message", out _) ||
            json.TryGetProperty("consentId", out _));
    }
}
