using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class PaymentServiceTests_PayCash_WithFakeInvoice_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "PaymentService"
    };

    [SkippableFact]
    public async Task PayCash_WithFakeInvoice_ShouldReturnError()
    {
    await AuthenticateAsAdminAsync(PaymentClient);
    var request = new { receivedBy = "Reception Staff" };
    var response = await PostAsJsonWithRetryAsync(PaymentClient, ApiEndpoints.Invoices.PayCash(Guid.NewGuid()), request);
    
    Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
