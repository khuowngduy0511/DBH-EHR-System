using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DBH.UnitTest.Api;

/// <summary>
/// API integration tests for DBH.Payment.Service
/// Covers: InvoicesController, PaymentsController, WebhookController
/// </summary>
public class PaymentServiceTests : ApiTestBase
{
    // =========================================================================
    // INVOICES
    // =========================================================================

    [Fact]
    public async Task CreateInvoice_WithSeedData_ShouldReturnMessage()
    {
        await AuthenticateAsAdminAsync(PaymentClient);
        var request = new { patientId = TestSeedData.PatientUserId, organizationId = TestSeedData.HospitalAOrgId, encounterId = Guid.NewGuid(), items = new[] { new { description = "Consultation", amount = 500000m, quantity = 1 } } };
        var response = await PaymentClient.PostAsJsonAsync(ApiEndpoints.Invoices.Create, request);

        var json = await ReadJsonResponseAsync(response);
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task GetInvoice_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(PaymentClient);
        var response = await PaymentClient.GetAsync(ApiEndpoints.Invoices.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetInvoicesByPatient_WithSeedPatient_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(PaymentClient);
        var response = await PaymentClient.GetAsync($"{ApiEndpoints.Invoices.ByPatient(TestSeedData.PatientUserId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetInvoicesByOrg_WithSeedOrg_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(PaymentClient);
        var response = await PaymentClient.GetAsync($"{ApiEndpoints.Invoices.ByOrg(TestSeedData.HospitalAOrgId)}?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonResponseAsync(response);
        Assert.True(json.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task CancelInvoice_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(PaymentClient);
        var response = await PaymentClient.PostAsync(ApiEndpoints.Invoices.Cancel(Guid.NewGuid()), null);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
    }

    // =========================================================================
    // PAYMENTS
    // =========================================================================

    [Fact]
    public async Task Checkout_WithFakeInvoice_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(PaymentClient);
        var request = new { returnUrl = "http://localhost:3000/payment/result", cancelUrl = "http://localhost:3000/payment/cancel" };
        var response = await PaymentClient.PostAsJsonAsync(ApiEndpoints.Invoices.Checkout(Guid.NewGuid()), request);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task PayCash_WithFakeInvoice_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(PaymentClient);
        var request = new { receivedBy = "Reception Staff" };
        var response = await PaymentClient.PostAsJsonAsync(ApiEndpoints.Invoices.PayCash(Guid.NewGuid()), request);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPayment_WithFakeId_ShouldReturnNotFound()
    {
        await AuthenticateAsAdminAsync(PaymentClient);
        var response = await PaymentClient.GetAsync(ApiEndpoints.Payments.GetById(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VerifyPayment_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(PaymentClient);
        var response = await PaymentClient.PostAsync(ApiEndpoints.Payments.Verify(Guid.NewGuid()), null);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // WEBHOOK
    // =========================================================================

    [Fact]
    public async Task PayOSWebhook_WithTestPayload_ShouldReturnOk()
    {
        var request = new { code = "00", desc = "success", data = new { orderCode = 12345, amount = 500000, description = "Test payment", accountNumber = "123", reference = "ref-001", transactionDateTime = DateTime.UtcNow.ToString("o"), currency = "VND", paymentLinkId = "pl-test-123", code = "00", desc = "success" } };
        var response = await PaymentClient.PostAsJsonAsync(ApiEndpoints.Payments.Webhook, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
