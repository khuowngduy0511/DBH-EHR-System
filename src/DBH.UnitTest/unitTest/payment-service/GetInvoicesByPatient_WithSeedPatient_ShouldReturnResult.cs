using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class PaymentServiceTests_GetInvoicesByPatient_WithSeedPatient_ShouldReturnResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "PaymentService"
    };

    [SkippableFact]
    public async Task GetInvoicesByPatient_WithSeedPatient_ShouldReturnResult()
    {
    await AuthenticateAsAdminAsync(PaymentClient);
    var response = await GetWithRetryAsync(PaymentClient, $"{ApiEndpoints.Invoices.ByPatient(TestSeedData.PatientUserId)}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.GetProperty("success").GetBoolean());
    }
}
