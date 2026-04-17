using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class EhrServiceTests_EncryptToIpfs_WithTestPayload_ShouldReturnMessage : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "EhrService"
    };

    [SkippableFact]
    public async Task EncryptToIpfs_WithTestPayload_ShouldReturnMessage()
    {
    await AuthenticateAsAdminAsync(EhrClient);
    var request = new { payload = "{\"test\": \"data\"}" };
    var response = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.EncryptToIpfs, request);
    
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
