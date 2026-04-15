using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class PaymentServiceTests_Checkout_WithFakeInvoice_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "PaymentService"
    };

    [SkippableFact]
    public async Task Checkout_WithFakeInvoice_ShouldReturnError()
    {
    await AuthenticateAsAdminAsync(PaymentClient);
    var request = new { returnUrl = "http://localhost:3000/payment/result", cancelUrl = "http://localhost:3000/payment/cancel" };
    var response = await PostAsJsonWithRetryAsync(PaymentClient, ApiEndpoints.Invoices.Checkout(Guid.NewGuid()), request);
    
    Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    var json = await ReadJsonResponseAsync(response);
    Assert.False(json.GetProperty("success").GetBoolean());
    Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }
}
