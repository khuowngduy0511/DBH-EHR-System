using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class EhrServiceTests_GetEhrVersions_WithFakeId_ShouldReturnEmptyOrNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "EhrService"
    };

    [SkippableFact]
    public async Task GetEhrVersions_WithFakeId_ShouldReturnEmptyOrNotFound()
    {
    await AuthenticateAsAdminAsync(EhrClient);
    var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.Versions(Guid.NewGuid()));
    
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
