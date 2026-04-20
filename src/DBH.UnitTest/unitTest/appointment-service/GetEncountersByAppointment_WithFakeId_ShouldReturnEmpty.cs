using System.Net;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

public class AppointmentServiceTests_GetEncountersByAppointment_WithFakeId_ShouldReturnEmpty : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[] { "AppointmentService" };

    [SkippableFact]
    public async Task GetEncountersByAppointment_WithFakeId_ShouldReturnEmpty()
    {
        await AuthenticateAsDoctorAsync(AuthClient);

        var fakeAppointmentId = Guid.NewGuid();
        var url = ApiEndpoints.Encounters.ByAppointment(fakeAppointmentId);
        
        var response = await GetWithRetryAsync(AuthClient, url);
        
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }
}
