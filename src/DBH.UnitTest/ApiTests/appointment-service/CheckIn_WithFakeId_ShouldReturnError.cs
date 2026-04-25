using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.ApiTests;

public class AppointmentServiceTests_CheckIn_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AppointmentService"
    };

    [SkippableFact]
    public async Task CheckIn_WithFakeId_ShouldReturnError()
    {
    await AuthenticateAsAdminAsync(AppointmentClient);
    var response = await PutWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.CheckIn(Guid.NewGuid()), null);
    
    Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    }
}
