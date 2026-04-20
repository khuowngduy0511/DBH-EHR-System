using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_GetMembership_DoctorMembership_ShouldReturnCorrectData : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "OrganizationService"
    };

    [SkippableFact]
    public async Task GetMembership_DoctorMembership_ShouldReturnCorrectData()
    {
    await AuthenticateAsAdminAsync(OrganizationClient);
    var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Memberships.GetById(TestSeedData.DoctorMembershipId));
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.TryGetProperty("data", out var dataElement));
    var data = dataElement;
    Assert.True(data.ValueKind == JsonValueKind.Object);
    }
}
