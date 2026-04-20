namespace DBH.UnitTest.Shared;

internal static class TestRuntimeContext
{
    private static readonly System.Threading.AsyncLocal<ApiTestBase.FreshDoctorPatientUsers?> CurrentUsers = new();

    public static ApiTestBase.FreshDoctorPatientUsers? Get() => CurrentUsers.Value;

    public static void Set(ApiTestBase.FreshDoctorPatientUsers users)
    {
        CurrentUsers.Value = users;
    }

    public static void Clear()
    {
        CurrentUsers.Value = null;
    }
}
