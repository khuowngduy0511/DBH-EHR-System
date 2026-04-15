using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceTests_UpdatePatient_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AuthService" };

    [SkippableFact]
    public async Task UpdatePatient_WithFakeId_ShouldReturnError()
    {
        var fakePatientId = Guid.NewGuid();
        var request = new { dob = "1990-01-01", bloodType = "O+" };
        var url = ApiEndpoints.Patients.Update(fakePatientId);
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, url, request);
        
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
