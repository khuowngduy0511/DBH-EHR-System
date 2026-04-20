using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class PaymentServiceTests_GetPayment_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "PaymentService"
    };

    [SkippableFact]
    public async Task GetPayment_WithFakeId_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(PaymentClient);
    var response = await GetWithRetryAsync(PaymentClient, ApiEndpoints.Payments.GetById(Guid.NewGuid()));
    
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
