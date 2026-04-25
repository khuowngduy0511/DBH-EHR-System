using DBH.Appointment.Service.DTOs;

namespace DBH.UnitTest.UnitTests;

public class AuthServiceClientDirectTests
{
    public enum ScenarioKind
    {
        HappyPath,
        NotFound,
        InvalidResponse,
        DependencyFailure
    }

    [Theory]
    [MemberData(nameof(GetUserIdByPatientCases))]
    public async Task GetUserIdByPatientIdAsync_Cases(string caseId, ScenarioKind scenario)
    {
        var fixture = AppointmentServiceTestSupport.CreateFixture();
        var patientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        fixture.AuthResponses.PatientUserIds[patientId] = userId;

        if (scenario == ScenarioKind.DependencyFailure)
        {
            fixture.AuthResponses.ThrowOnLookup = true;
            var result = await fixture.AuthClient.GetUserIdByPatientIdAsync(patientId);
            Assert.Null(result);
            return;
        }

        if (scenario == ScenarioKind.NotFound)
        {
            var result = await fixture.AuthClient.GetUserIdByPatientIdAsync(Guid.NewGuid());
            Assert.Null(result);
            return;
        }

        var happy = await fixture.AuthClient.GetUserIdByPatientIdAsync(patientId);
        Assert.Equal(userId, happy);
    }

    [Theory]
    [MemberData(nameof(GetUserIdByDoctorCases))]
    public async Task GetUserIdByDoctorIdAsync_Cases(string caseId, ScenarioKind scenario)
    {
        var fixture = AppointmentServiceTestSupport.CreateFixture();
        var doctorId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        fixture.AuthResponses.DoctorUserIds[doctorId] = userId;

        if (scenario == ScenarioKind.DependencyFailure)
        {
            fixture.AuthResponses.ThrowOnLookup = true;
            var result = await fixture.AuthClient.GetUserIdByDoctorIdAsync(doctorId);
            Assert.Null(result);
            return;
        }

        if (scenario == ScenarioKind.NotFound)
        {
            var result = await fixture.AuthClient.GetUserIdByDoctorIdAsync(Guid.NewGuid());
            Assert.Null(result);
            return;
        }

        var happy = await fixture.AuthClient.GetUserIdByDoctorIdAsync(doctorId);
        Assert.Equal(userId, happy);
    }

    [Theory]
    [MemberData(nameof(GetUserProfileCases))]
    public async Task GetUserProfileDetailAsync_Cases(string caseId, ScenarioKind scenario)
    {
        var fixture = AppointmentServiceTestSupport.CreateFixture();
        var userId = Guid.NewGuid();
        fixture.AuthResponses.Profiles[userId] = AppointmentServiceTestSupport.BuildProfile(userId, "Direct Profile");

        if (scenario == ScenarioKind.DependencyFailure)
        {
            fixture.AuthResponses.ThrowOnLookup = true;
            var result = await fixture.AuthClient.GetUserProfileDetailAsync(userId);
            Assert.Null(result);
            return;
        }

        if (scenario == ScenarioKind.NotFound)
        {
            var result = await fixture.AuthClient.GetUserProfileDetailAsync(Guid.NewGuid());
            Assert.Null(result);
            return;
        }

        var happy = await fixture.AuthClient.GetUserProfileDetailAsync(userId);
        Assert.NotNull(happy);
        Assert.Equal(userId, happy!.UserId);
        Assert.Equal("Direct Profile", happy.FullName);
    }

    public static IEnumerable<object[]> GetUserIdByPatientCases()
        => Cases(
            (new[] { "GetUserIdByPatientIdAsync-01" }, ScenarioKind.HappyPath),
            (new[] { "GetUserIdByPatientIdAsync-02" }, ScenarioKind.InvalidResponse),
            (new[] { "GetUserIdByPatientIdAsync-03" }, ScenarioKind.NotFound),
            (new[] { "GetUserIdByPatientIdAsync-04" }, ScenarioKind.NotFound),
            (new[] { "GetUserIdByPatientIdAsync-05" }, ScenarioKind.DependencyFailure));

    public static IEnumerable<object[]> GetUserIdByDoctorCases()
        => Cases(
            (new[] { "GetUserIdByDoctorIdAsync-01" }, ScenarioKind.HappyPath),
            (new[] { "GetUserIdByDoctorIdAsync-02" }, ScenarioKind.InvalidResponse),
            (new[] { "GetUserIdByDoctorIdAsync-03" }, ScenarioKind.NotFound),
            (new[] { "GetUserIdByDoctorIdAsync-04" }, ScenarioKind.NotFound),
            (new[] { "GetUserIdByDoctorIdAsync-05" }, ScenarioKind.DependencyFailure));

    public static IEnumerable<object[]> GetUserProfileCases()
        => Cases(
            (new[] { "GetUserProfileDetailAsync-01" }, ScenarioKind.HappyPath),
            (new[] { "GetUserProfileDetailAsync-02" }, ScenarioKind.InvalidResponse),
            (new[] { "GetUserProfileDetailAsync-03" }, ScenarioKind.NotFound),
            (new[] { "GetUserProfileDetailAsync-04" }, ScenarioKind.NotFound),
            (new[] { "GetUserProfileDetailAsync-05" }, ScenarioKind.DependencyFailure));

    private static IEnumerable<object[]> Cases(params (string[] CaseIds, ScenarioKind Scenario)[] groups)
    {
        foreach (var group in groups)
        {
            foreach (var caseId in group.CaseIds)
            {
                yield return new object[] { caseId, group.Scenario };
            }
        }
    }
}