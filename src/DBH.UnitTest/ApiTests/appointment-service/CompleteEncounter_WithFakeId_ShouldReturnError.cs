using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AppointmentServiceTests_CompleteEncounter_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AppointmentService"
    };

    [SkippableFact]
    public async Task CompleteEncounter_WithFakeId_ShouldReturnError()
    {
    await AuthenticateAsAdminAsync(AppointmentClient);
    var request = new { diagnosis = "Cold", treatment = "Rest", prescription = "Paracetamol" };
    var response = await PutAsJsonWithRetryAsync(AppointmentClient, ApiEndpoints.Encounters.Complete(Guid.NewGuid()), request);
    
    Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
