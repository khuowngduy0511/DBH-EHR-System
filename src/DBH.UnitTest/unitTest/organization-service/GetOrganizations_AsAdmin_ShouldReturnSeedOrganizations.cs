using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_GetOrganizations_AsAdmin_ShouldReturnSeedOrganizations : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "OrganizationService"
    };

    [SkippableFact]
    public async Task GetOrganizations_AsAdmin_ShouldReturnSeedOrganizations()
    {
    await AuthenticateAsAdminAsync(OrganizationClient);
    var response = await GetWithRetryAsync(OrganizationClient, $"{ApiEndpoints.Organizations.GetAll}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.TryGetProperty("data", out var dataElement));
    var data = dataElement;
    Assert.True(data.GetArrayLength() >= 3, "Should contain at least 3 seed organizations");
    }
}
