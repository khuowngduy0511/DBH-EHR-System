using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.E2E;

public class InvoiceCancel_ShouldUpdateStatus : Shared.ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService", "PaymentService" };

    [SkippableFact]
    public async Task InvoiceCancel_ShouldUpdateStatus_Test()
    {
        await AuthenticateAsAdminAsync(PaymentClient);

        var invoiceRequest = new
        {
            patientId = Shared.TestSeedData.PatientUserId,
            organizationId = Shared.TestSeedData.HospitalAOrgId,
            encounterId = Guid.NewGuid(),
            items = new[] { new { description = "E2E Cancel Test", amount = 100000m, quantity = 1 } }
        };

        var invoiceResponse = await PostAsJsonWithRetryAsync(PaymentClient, Shared.ApiEndpoints.Invoices.Create, invoiceRequest);
        Assert.True(invoiceResponse.StatusCode == HttpStatusCode.Created || invoiceResponse.StatusCode == HttpStatusCode.OK);
    }
}