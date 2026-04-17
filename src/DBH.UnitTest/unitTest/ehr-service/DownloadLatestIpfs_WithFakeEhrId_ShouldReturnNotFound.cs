using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class EhrServiceTests_DownloadLatestIpfs_WithFakeEhrId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "EhrService"
    };

    [SkippableFact]
    public async Task DownloadLatestIpfs_WithFakeEhrId_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(EhrClient);
    var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.DownloadLatestIpfs(Guid.NewGuid()));
    
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
