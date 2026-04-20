using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class PaymentServiceTests_GetInvoicesByOrg_WithSeedOrg_ShouldReturnResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "PaymentService"
    };

    [SkippableFact]
    public async Task GetInvoicesByOrg_WithSeedOrg_ShouldReturnResult()
    {
    await AuthenticateAsAdminAsync(PaymentClient);
    var response = await GetWithRetryAsync(PaymentClient, $"{ApiEndpoints.Invoices.ByOrg(TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.GetProperty("success").GetBoolean());
    }
}
