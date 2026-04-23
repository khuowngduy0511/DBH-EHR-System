using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

public class LoginAllSeedUsers_ShouldSucceed : Shared.ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "AppointmentService" };

    [SkippableFact]
    public async Task LoginAllSeedUsers_ShouldSucceed_Test()
    {
        var credentials = new[]
        {
            (Shared.TestSeedData.AdminEmail, Shared.TestSeedData.AdminPassword),
            (Shared.TestSeedData.DoctorEmail, Shared.TestSeedData.DoctorPassword),
            (Shared.TestSeedData.PatientEmail, Shared.TestSeedData.PatientPassword),
            (Shared.TestSeedData.NurseEmail, Shared.TestSeedData.NursePassword),
            (Shared.TestSeedData.PharmacistEmail, Shared.TestSeedData.PharmacistPassword),
            (Shared.TestSeedData.ReceptionistEmail, Shared.TestSeedData.ReceptionistPassword)
        };

        foreach (var (email, password) in credentials)
        {
            var response = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Login, new { email, password });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}