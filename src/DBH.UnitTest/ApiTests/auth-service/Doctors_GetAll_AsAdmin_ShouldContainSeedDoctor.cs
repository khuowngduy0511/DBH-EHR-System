using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_Doctors_GetAll_AsAdmin_ShouldContainSeedDoctor : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task Doctors_GetAll_AsAdmin_ShouldContainSeedDoctor()
    {
    await AuthenticateAsAdminAsync(AuthClient);
    var response = await GetWithRetryAsync(AuthClient, $"{ApiEndpoints.Doctors.GetAll}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    var data = json.GetProperty("data");
    Assert.True(data.GetArrayLength() >= 1, "Should contain at least the seed doctor");
    }
}
