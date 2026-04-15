using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class PaymentServiceTests_CancelInvoice_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "PaymentService"
    };

    [SkippableFact]
    public async Task CancelInvoice_WithFakeId_ShouldReturnError()
    {
    await AuthenticateAsAdminAsync(PaymentClient);
    var response = await PostWithRetryAsync(PaymentClient, ApiEndpoints.Invoices.Cancel(Guid.NewGuid()), null);
    
    Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    var json = await ReadJsonResponseAsync(response);
    Assert.False(json.GetProperty("success").GetBoolean());
    }
}
