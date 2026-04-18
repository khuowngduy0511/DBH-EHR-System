using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Bad case: Try to access EHR document without the required X-Requester-Id header
/// Expected: 400 Bad Request
/// </summary>
public class EhrServiceTests_GetEhrDocument_WithoutRequesterIdHeader_ShouldReturnBadRequest : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "EhrService"
    };

    [SkippableFact]
    public async Task GetEhrDocument_WithoutRequesterIdHeader_ShouldReturnBadRequest()
    {
        await AuthenticateAsDoctorAsync(EhrClient);

        var ehrId = Guid.NewGuid();
        
        // Try to get document without X-Requester-Id header
        var response = await GetWithRetryAsync(EhrClient, ApiEndpoints.Ehr.GetDocument(ehrId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
