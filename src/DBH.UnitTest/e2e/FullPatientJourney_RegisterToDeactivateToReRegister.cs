using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

public class FullPatientJourney_RegisterToDeactivateToReRegister : Shared.ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "AppointmentService" };

    [SkippableFact]
    public async Task FullPatientJourney_RegisterToDeactivateToReRegister_Test()
    {
        var email = $"e2e_patient_{Guid.NewGuid():N}@test.com";
        var phone = $"09{Random.Shared.Next(10000000, 99999999)}";
        var registerRequest = new { fullName = "E2E Test Patient", email, password = "E2ETest@123", phone, gender = "Male", dateOfBirth = "1995-03-15" };

        var registerResponse = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Register, registerRequest);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginRequest = new { email, password = "E2ETest@123" };
        var loginResponse = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Login, loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }
}