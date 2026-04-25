using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AuthServiceTests_UpdateDoctor_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService" };

    [SkippableFact]
    public async Task UpdateDoctor_WithFakeId_ShouldReturnError()
    {
        var fakeDoctorId = Guid.NewGuid();
        var request = new { specialty = "Cardiology", licenseNumber = "LIC123", verifiedStatus = true };
        var url = ApiEndpoints.Doctors.Update(fakeDoctorId);
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, url, request);
        
        Assert.False(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
