using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_GetPaymentConfigStatus_ForSeedOrg_ShouldReturnResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "OrganizationService"
    };

    [SkippableFact]
    public async Task GetPaymentConfigStatus_ForSeedOrg_ShouldReturnResult()
    {
    await AuthenticateAsAdminAsync(OrganizationClient);
    var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.PaymentConfig.GetStatus(TestSeedData.HospitalAOrgId));
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
