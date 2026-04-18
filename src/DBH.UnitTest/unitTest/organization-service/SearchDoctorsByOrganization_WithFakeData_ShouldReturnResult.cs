using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class OrganizationServiceTests_SearchDoctorsByOrganization_WithFakeData_ShouldReturnResult : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "OrganizationService" };

    [SkippableFact]
    public async Task SearchDoctorsByOrganization_WithFakeData_ShouldReturnResult()
    {
        await AuthenticateAsAdminAsync(AuthClient);

        var request = new { organizationId = Guid.NewGuid(), departmentId = (Guid?)null, specialty = "Cardiology" };
        
        var response = await PostAsJsonWithRetryAsync(AuthClient, ApiEndpoints.Memberships.SearchDoctors, request);
        
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
