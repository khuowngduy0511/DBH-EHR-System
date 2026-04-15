using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_Reschedule_WithFakeId_ShouldReturnError : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AppointmentService" };

    [SkippableFact]
    public async Task Reschedule_WithFakeId_ShouldReturnError()
    {
        var fakeId = Guid.NewGuid();
        var newDate = DateTime.UtcNow.AddDays(1).ToString("o");
        var url = ApiEndpoints.Appointments.Reschedule(fakeId, newDate);
        var request = new { };
        
        var response = await PutAsJsonWithRetryAsync(AuthClient, url, request);
        
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
    }
}
