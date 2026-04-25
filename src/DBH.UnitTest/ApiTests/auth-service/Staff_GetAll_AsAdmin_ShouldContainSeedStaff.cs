using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_Staff_GetAll_AsAdmin_ShouldContainSeedStaff : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task Staff_GetAll_AsAdmin_ShouldContainSeedStaff()
    {
    await AuthenticateAsAdminAsync(AuthClient);
    var response = await GetWithRetryAsync(AuthClient, $"{ApiEndpoints.Staff.GetAll}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    var data = json.GetProperty("data");
    // Should contain pharmacist, nurse, receptionist staff entries
    Assert.True(data.GetArrayLength() >= 3, "Should contain at least 3 seed staff members");
    }
}
