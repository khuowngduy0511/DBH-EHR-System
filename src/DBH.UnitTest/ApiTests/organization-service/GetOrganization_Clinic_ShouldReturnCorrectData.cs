using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class OrganizationServiceTests_GetOrganization_Clinic_ShouldReturnCorrectData : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "OrganizationService"
    };

    [SkippableFact]
    public async Task GetOrganization_Clinic_ShouldReturnCorrectData()
    {
    await AuthenticateAsAdminAsync(OrganizationClient);
    var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.GetById(TestSeedData.ClinicOrgId));
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.TryGetProperty("data", out var dataElement));
    var data = dataElement;
    Assert.Equal(TestSeedData.ClinicName, data.GetProperty("orgName").GetString());
    }
}
