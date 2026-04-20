using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_GetPaymentKeys_WithInternalApiKey_ShouldReturnResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "OrganizationService"
    };

    [SkippableFact]
    public async Task GetPaymentKeys_WithInternalApiKey_ShouldReturnResult()
    {
    OrganizationClient.DefaultRequestHeaders.Add("X-Internal-Api-Key", "dbh-internal-s2s-secret-key-2026!");
    var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Internal.GetPaymentKeys(TestSeedData.HospitalAOrgId));
    
    Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    OrganizationClient.DefaultRequestHeaders.Remove("X-Internal-Api-Key");
    }
}
