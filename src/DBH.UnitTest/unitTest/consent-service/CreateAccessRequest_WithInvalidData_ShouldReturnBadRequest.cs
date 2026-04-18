using System.Net;
using System.Net.Http.Json;
using DBH.UnitTest.Shared;

namespace DBH.UnitTest.UnitTests;

/// <summary>
/// Bad case: Try to create access request with missing required fields
/// Expected: 400 Bad Request
/// </summary>
public class ConsentServiceTests_CreateAccessRequest_WithInvalidData_ShouldReturnBadRequest : ApiTestBase
{
    protected override IReadOnlyCollection<string> RequiredServices => new[]
    {
        "AuthService",
        "ConsentService"
    };

    [SkippableFact]
    public async Task CreateAccessRequest_WithMissingReason_ShouldReturnBadRequest()
    {
        await AuthenticateAsDoctorAsync(ConsentClient);

        var request = new 
        { 
            patientDid = TestSeedData.PatientUserId.ToString(), 
            requesterDid = TestSeedData.DoctorUserId.ToString(), 
            requestedDurationDays = 7 
        };

        var response = await PostAsJsonWithRetryAsync(ConsentClient, ApiEndpoints.AccessRequests.Create, request);

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest || 
            response.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {response.StatusCode}");
    }
}
