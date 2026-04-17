using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_GetDepartmentsByOrg_HospitalA_ShouldReturnSeedDepartments : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "OrganizationService"
    };

    [SkippableFact]
    public async Task GetDepartmentsByOrg_HospitalA_ShouldReturnSeedDepartments()
    {
    await AuthenticateAsAdminAsync(OrganizationClient);
    var response = await GetWithRetryAsync(OrganizationClient, $"{ApiEndpoints.Departments.ByOrganization(TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.TryGetProperty("data", out var dataElement));
    var data = dataElement;
    // Hospital A has 3 departments: Cardiology, Pharmacy, Reception
    Assert.True(data.GetArrayLength() >= 3, "Hospital A should have at least 3 departments");
    }
}
