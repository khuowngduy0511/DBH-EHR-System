using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_GetOrganization_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "OrganizationService"
    };

    [SkippableFact]
    public async Task GetOrganization_WithFakeId_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(OrganizationClient);
    var response = await GetWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.GetById(Guid.NewGuid()));
    
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
