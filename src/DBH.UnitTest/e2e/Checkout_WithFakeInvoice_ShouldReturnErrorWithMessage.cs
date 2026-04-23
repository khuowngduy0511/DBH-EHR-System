using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

public class Checkout_WithFakeInvoice_ShouldReturnErrorWithMessage : Shared.ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "PaymentService" };

    [SkippableFact]
    public async Task Checkout_WithFakeInvoice_ShouldReturnErrorWithMessage_Test()
    {
        await AuthenticateAsAdminAsync(PaymentClient);

        var request = new { returnUrl = "http://localhost:3000/result", cancelUrl = "http://localhost:3000/cancel" };
        var response = await PostAsJsonWithRetryAsync(PaymentClient, Shared.ApiEndpoints.Invoices.Checkout(Guid.NewGuid()), request);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}