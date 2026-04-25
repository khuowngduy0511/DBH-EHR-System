using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class OrganizationServiceTests_VerifyOrganization_WithSeedOrg_ShouldReturnResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "OrganizationService"
    };

    [SkippableFact]
    public async Task VerifyOrganization_WithSeedOrg_ShouldReturnResult()
    {
    await AuthenticateAsAdminAsync(OrganizationClient);
    var response = await PostWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.Verify(TestSeedData.HospitalAOrgId, TestSeedData.AdminUserId), null);
    
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
