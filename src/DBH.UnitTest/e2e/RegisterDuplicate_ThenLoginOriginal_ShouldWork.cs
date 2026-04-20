using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

public class RegisterDuplicate_ThenLoginOriginal_ShouldWork : Shared.ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "AppointmentService" };

    [SkippableFact]
    public async Task RegisterDuplicate_ThenLoginOriginal_ShouldWork_Test()
    {
        var dupRequest = new { fullName = "Dup", email = Shared.TestSeedData.AdminEmail, password = "Test@123", phone = "0999999998", gender = "Male", dateOfBirth = "1990-01-01" };
        var dupResponse = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Register, dupRequest);
        Assert.Equal(HttpStatusCode.BadRequest, dupResponse.StatusCode);

        var loginResponse = await PostAsJsonWithRetryAsync(AuthClient, Shared.ApiEndpoints.Auth.Login, new { email = Shared.TestSeedData.AdminEmail, password = Shared.TestSeedData.AdminPassword });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }
}