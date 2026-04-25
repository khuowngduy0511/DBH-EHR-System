using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class PaymentServiceTests_PayOSWebhook_WithTestPayload_ShouldReturnOk : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "PaymentService"
    };

    [SkippableFact]
    public async Task PayOSWebhook_WithTestPayload_ShouldReturnOk()
    {
    var request = new { code = "00", desc = "success", data = new { orderCode = 12345, amount = 500000, description = "Test payment", accountNumber = "123", reference = "ref-001", transactionDateTime = DateTime.UtcNow.ToString("o"), currency = "VND", paymentLinkId = "pl-test-123", code = "00", desc = "success" } };
    var response = await PostAsJsonWithRetryAsync(PaymentClient, ApiEndpoints.Payments.Webhook, request);
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
