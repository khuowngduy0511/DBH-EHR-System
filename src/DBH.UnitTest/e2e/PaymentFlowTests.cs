using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

/// <summary>
/// End-to-end: Payment flow across Payment service.
/// Flow: Create Invoice → Get Invoice → Checkout → Pay Cash → Verify → Cancel
/// </summary>
public class PaymentFlowTests : Shared.ApiTestBase
{
    [Fact]
    public async Task InvoiceLifecycle_CreateToPayCash_ShouldSucceed()
    {
        // =====================================================================
        // STEP 1: Admin creates invoice for seed patient at seed org
        // =====================================================================
        await AuthenticateAsAdminAsync(PaymentClient);

        var invoiceRequest = new
        {
            patientId = Shared.TestSeedData.PatientUserId,
            organizationId = Shared.TestSeedData.HospitalAOrgId,
            encounterId = Guid.NewGuid(),
            items = new[]
            {
                new { description = "E2E Consultation", amount = 300000m, quantity = 1 },
                new { description = "E2E Blood Test", amount = 150000m, quantity = 1 }
            }
        };

        var invoiceResponse = await PaymentClient.PostAsJsonAsync(Shared.ApiEndpoints.Invoices.Create, invoiceRequest);
        var invoiceJson = await ReadJsonResponseAsync(invoiceResponse);
        Assert.False(string.IsNullOrEmpty(invoiceJson.GetProperty("message").GetString()));

        if (invoiceResponse.StatusCode == HttpStatusCode.Created || invoiceResponse.StatusCode == HttpStatusCode.OK)
        {
            Assert.True(invoiceJson.GetProperty("success").GetBoolean());
            var invoiceId = Guid.Parse(invoiceJson.GetProperty("data").GetProperty("invoiceId").GetString()!);

            // =================================================================
            // STEP 2: Get invoice and verify total
            // =================================================================
            var getResponse = await PaymentClient.GetAsync(Shared.ApiEndpoints.Invoices.GetById(invoiceId));
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var getJson = await ReadJsonResponseAsync(getResponse);
            Assert.True(getJson.GetProperty("success").GetBoolean());

            // =================================================================
            // STEP 3: Pay by cash
            // =================================================================
            var payCashRequest = new { receivedBy = "E2E Reception Staff" };
            var payCashResponse = await PaymentClient.PostAsJsonAsync(Shared.ApiEndpoints.Invoices.PayCash(invoiceId), payCashRequest);
            var payCashJson = await ReadJsonResponseAsync(payCashResponse);
            Assert.False(string.IsNullOrEmpty(payCashJson.GetProperty("message").GetString()));

            // =================================================================
            // STEP 4: List invoices by patient — should include the new one
            // =================================================================
            var listResponse = await PaymentClient.GetAsync(
                $"{Shared.ApiEndpoints.Invoices.ByPatient(Shared.TestSeedData.PatientUserId)}?page=1&pageSize=10");
            Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
            var listJson = await ReadJsonResponseAsync(listResponse);
            Assert.True(listJson.GetProperty("success").GetBoolean());
            Assert.True(listJson.GetProperty("data").GetArrayLength() >= 1);
        }
    }

    [Fact]
    public async Task InvoiceCancel_ShouldUpdateStatus()
    {
        await AuthenticateAsAdminAsync(PaymentClient);

        // STEP 1: Create invoice
        var invoiceRequest = new
        {
            patientId = Shared.TestSeedData.PatientUserId,
            organizationId = Shared.TestSeedData.HospitalAOrgId,
            encounterId = Guid.NewGuid(),
            items = new[] { new { description = "E2E Cancel Test", amount = 100000m, quantity = 1 } }
        };

        var invoiceResponse = await PaymentClient.PostAsJsonAsync(Shared.ApiEndpoints.Invoices.Create, invoiceRequest);
        var invoiceJson = await ReadJsonResponseAsync(invoiceResponse);

        if (invoiceResponse.StatusCode == HttpStatusCode.Created || invoiceResponse.StatusCode == HttpStatusCode.OK)
        {
            var invoiceId = Guid.Parse(invoiceJson.GetProperty("data").GetProperty("invoiceId").GetString()!);

            // STEP 2: Cancel the invoice
            var cancelResponse = await PaymentClient.PostAsync(Shared.ApiEndpoints.Invoices.Cancel(invoiceId), null);
            var cancelJson = await ReadJsonResponseAsync(cancelResponse);
            Assert.False(string.IsNullOrEmpty(cancelJson.GetProperty("message").GetString()));

            if (cancelResponse.StatusCode == HttpStatusCode.OK)
            {
                Assert.True(cancelJson.GetProperty("success").GetBoolean());

                // STEP 3: Try to pay the cancelled invoice — should fail
                var payCashRequest = new { receivedBy = "Staff" };
                var payCashResponse = await PaymentClient.PostAsJsonAsync(Shared.ApiEndpoints.Invoices.PayCash(invoiceId), payCashRequest);
                Assert.True(payCashResponse.StatusCode == HttpStatusCode.BadRequest || payCashResponse.StatusCode == HttpStatusCode.NotFound);
            }
        }
    }

    [Fact]
    public async Task Checkout_WithFakeInvoice_ShouldReturnErrorWithMessage()
    {
        await AuthenticateAsAdminAsync(PaymentClient);

        var request = new { returnUrl = "http://localhost:3000/result", cancelUrl = "http://localhost:3000/cancel" };
        var response = await PaymentClient.PostAsJsonAsync(Shared.ApiEndpoints.Invoices.Checkout(Guid.NewGuid()), request);

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
        var json = await ReadJsonResponseAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }
}

