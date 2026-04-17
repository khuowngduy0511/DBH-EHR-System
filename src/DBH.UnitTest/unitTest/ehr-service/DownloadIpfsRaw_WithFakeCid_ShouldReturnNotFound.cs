using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class EhrServiceTests_DownloadIpfsRaw_WithFakeCid_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "EhrService"
    };

    [SkippableFact]
    public async Task DownloadIpfsRaw_WithFakeCid_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(EhrClient);
    var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.DownloadIpfsRaw("QmFakeCid123"));
    
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
