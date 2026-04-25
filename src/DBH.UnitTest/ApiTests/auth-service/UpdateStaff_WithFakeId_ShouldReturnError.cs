using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_UpdateStaff_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService" };

    [SkippableFact]
    public async Task UpdateStaff_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsAdminAsync(AuthClient);

        var fakeStaffId = Guid.NewGuid();
        var request = new { role = "Nurse", specialty = "General", licenseNumber = "STAFF123", verifiedStatus = true };
        var url = ApiEndpoints.Staff.Update(fakeStaffId);
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, url, request);
        
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
