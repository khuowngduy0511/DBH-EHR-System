using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class OrganizationServiceTests_UpdateOrganization_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "OrganizationService"
    };

    [SkippableFact]
    public async Task UpdateOrganization_WithFakeId_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(OrganizationClient);
    var request = new { name = "Updated Hospital" };
    var response = await PutAsJsonWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.Update(Guid.NewGuid()), request);
    
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
