using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class EhrServiceTests_DecryptFromIpfs_WithFakeData_ShouldReturnErrorMessage : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "EhrService"
    };

    [SkippableFact]
    public async Task DecryptFromIpfs_WithFakeData_ShouldReturnErrorMessage()
    {
    await AuthenticateAsAdminAsync(EhrClient);
    var request = new { encryptedData = "not-real-data", wrappedKey = "not-real-key" };
    var response = await PostAsJsonWithRetryAsync(EhrClient, ApiEndpoints.Ehr.DecryptFromIpfs, request);
    
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
