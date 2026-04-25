using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class PaymentServiceTests_VerifyPayment_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "PaymentService"
    };

    [SkippableFact]
    public async Task VerifyPayment_WithFakeId_ShouldReturnError()
    {
    await AuthenticateAsAdminAsync(PaymentClient);
    var response = await PostWithRetryAsync(PaymentClient, ApiEndpoints.Payments.Verify(Guid.NewGuid()), null);
    
    Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
