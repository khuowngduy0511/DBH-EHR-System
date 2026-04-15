using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class ConsentServiceTests_SearchConsents_AsAdmin_ShouldReturnPagedResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "ConsentService"
    };

    [SkippableFact]
    public async Task SearchConsents_AsAdmin_ShouldReturnPagedResult()
    {
    await AuthenticateAsAdminAsync(ConsentClient);
    var response = await GetWithRetryAsync(ConsentClient, $"{ApiEndpoints.Consents.Search}?page=1&pageSize=10");
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.TryGetProperty("data", out _) || json.TryGetProperty("items", out _));
    }
}
