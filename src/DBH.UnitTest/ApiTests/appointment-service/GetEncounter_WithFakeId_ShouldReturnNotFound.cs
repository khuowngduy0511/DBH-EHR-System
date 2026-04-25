using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AppointmentServiceTests_GetEncounter_WithFakeId_ShouldReturnNotFound : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AppointmentService"
    };

    [SkippableFact]
    public async Task GetEncounter_WithFakeId_ShouldReturnNotFound()
    {
    await AuthenticateAsAdminAsync(AppointmentClient);
    var response = await GetWithRetryAsync(AppointmentClient, ApiEndpoints.Encounters.GetById(Guid.NewGuid()));
    
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
