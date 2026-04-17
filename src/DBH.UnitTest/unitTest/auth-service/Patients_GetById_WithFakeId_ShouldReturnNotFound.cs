using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_Patients_GetById_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService"
    };

    [SkippableFact]
    public async Task Patients_GetById_WithFakeId_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(AuthClient);
    var response = await GetWithRetryAsync(AuthClient, ApiEndpoints.Patients.GetById(Guid.NewGuid()));
    
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
