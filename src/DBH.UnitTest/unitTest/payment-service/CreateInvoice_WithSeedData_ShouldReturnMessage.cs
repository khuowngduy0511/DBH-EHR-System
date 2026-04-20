using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class PaymentServiceTests_CreateInvoice_WithSeedData_ShouldReturnMessage : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "PaymentService"
    };

    [SkippableFact]
    public async Task CreateInvoice_WithSeedData_ShouldReturnMessage()
    {
    await AuthenticateAsAdminAsync(PaymentClient);
    var request = new { patientId = TestSeedData.PatientUserId, organizationId = TestSeedData.HospitalAOrgId, encounterId = Guid.NewGuid(), items = new[] { new { description = "Consultation", amount = 500000m, quantity = 1 } } };
    var response = await PostAsJsonWithRetryAsync(PaymentClient, ApiEndpoints.Invoices.Create, request);
    
    var json = await ReadJsonResponseAsync(response);
    Assert.False(string.IsNullOrEmpty(json.GetProperty("message").GetString()));
    }
}
