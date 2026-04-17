using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_GetMembershipsByOrg_HospitalA_ShouldReturnSeedMemberships : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "OrganizationService"
    };

    [SkippableFact]
    public async Task GetMembershipsByOrg_HospitalA_ShouldReturnSeedMemberships()
    {
    await AuthenticateAsAdminAsync(OrganizationClient);
    var response = await GetWithRetryAsync(OrganizationClient, $"{ApiEndpoints.Memberships.ByOrganization(TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.TryGetProperty("data", out var dataElement));
    var data = dataElement;
    // Hospital A has admin, doctor, pharmacist, receptionist memberships
    Assert.True(data.GetArrayLength() >= 4, "Hospital A should have at least 4 memberships");
    }
}
