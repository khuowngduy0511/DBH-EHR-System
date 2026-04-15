using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class ConsentServiceTests_GetConsent_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "ConsentService"
    };

    [SkippableFact]
    public async Task GetConsent_WithFakeId_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(ConsentClient);
    var response = await GetWithRetryAsync(ConsentClient, ApiEndpoints.Consents.GetById(Guid.NewGuid()));
    
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.False(json.GetProperty("success").GetBoolean());
    }
}
