using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Bad case: Try to delete non-existent file
/// Expected: 404 Not Found or 204 No Content (idempotent)
/// </summary>
public class EhrServiceTests_DeleteEhrFile_WithNonExistentFile_ShouldHandleNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService"
    };

    [SkippableFact]
    public async Task DeleteEhrFile_WithNonExistentFile_ShouldHandleNotFound()
    {
        await AuthenticateAsDoctorAsync(EhrClient);

        var fakeEhrId = Guid.NewGuid();
        var fakeFileId = Guid.NewGuid();
        
        var response = await DeleteWithRetryAsync(
            EhrClient, 
            ApiEndpoints.Ehr.DeleteFile(fakeEhrId, fakeFileId));

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound || 
            response.StatusCode == HttpStatusCode.NoContent,
            $"Expected 404 or 204, got {response.StatusCode}");
    }
}
