using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AppointmentServiceTests_UpdateEncounter_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AppointmentService" };

    [SkippableFact]
    public async Task UpdateEncounter_WithFakeId_ShouldReturnError()
    {
        await AuthenticateAsDoctorAsync(AuthClient);

        var fakeEncounterId = Guid.NewGuid();
        var request = new { notes = "Updated encounter notes" };
        var url = ApiEndpoints.Encounters.Update(fakeEncounterId);
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, url, request);
        
        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
