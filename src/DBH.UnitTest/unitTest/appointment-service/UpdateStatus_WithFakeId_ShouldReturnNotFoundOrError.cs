using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_UpdateStatus_WithFakeId_ShouldReturnNotFoundOrError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
    "AuthService",
    "AppointmentService"
    };

    [SkippableFact]
    public async Task UpdateStatus_WithFakeId_ShouldReturnNotFoundOrError()
    {
    await AuthenticateAsAdminAsync(AppointmentClient);
    var response = await PutWithRetryAsync(AppointmentClient, ApiEndpoints.Appointments.UpdateStatus(Guid.NewGuid(), "Confirmed"), null);
    
    Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.BadRequest);
    var json = await ReadJsonResponseAsync(response);
    Assert.False(json.GetProperty("success").GetBoolean());
    }
}
