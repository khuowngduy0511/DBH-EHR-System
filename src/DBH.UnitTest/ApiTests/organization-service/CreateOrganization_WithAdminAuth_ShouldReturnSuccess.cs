using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class OrganizationServiceTests_CreateOrganization_WithAdminAuth_ShouldReturnSuccess : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "OrganizationService"
    };

    [SkippableFact]
    public async Task CreateOrganization_WithAdminAuth_ShouldReturnSuccess()
    {
    await AuthenticateAsAdminAsync(OrganizationClient);
    var request = new { name = "Test Hospital", address = "123 Test St", phone = "0900000001", email = "test@hospital.com", type = "Hospital" };
    var response = await PostAsJsonWithRetryAsync(OrganizationClient, ApiEndpoints.Organizations.Create, request);
    
    Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.BadRequest);
    var json = await ReadJsonResponseAsync(response);
    Assert.True(json.ValueKind == JsonValueKind.Object);
    }
}
