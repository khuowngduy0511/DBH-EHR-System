namespace DBH.UnitTest.Api;

/// <summary>
/// Known seed data IDs and values from database migrations.
/// These match the SeedData in AuthDbContext and OrganizationDbContext.
/// </summary>
public static class TestSeedData
{
    // =========================================================================
    // USER IDS
    // =========================================================================
    public static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid DoctorUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid PharmacistUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid NurseUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public static readonly Guid PatientUserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    public static readonly Guid ReceptionistUserId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    // =========================================================================
    // CREDENTIALS (email / password)
    // =========================================================================
    public const string AdminEmail = "admin@dbh.com";
    public const string AdminPassword = "admin123";

    public const string DoctorEmail = "doctor@dbh.com";
    public const string DoctorPassword = "doctor123";

    public const string PatientEmail = "patient@dbh.com";
    public const string PatientPassword = "patient123";

    public const string NurseEmail = "nurse@dbh.com";
    public const string NursePassword = "nurse123";

    public const string PharmacistEmail = "pharmacist@dbh.com";
    public const string PharmacistPassword = "pharma123";

    public const string ReceptionistEmail = "receptionist@dbh.com";
    public const string ReceptionistPassword = "receptionist123";

    // =========================================================================
    // USER PROFILE DATA
    // =========================================================================
    public const string AdminFullName = "Admin User";
    public const string DoctorFullName = "Dr. House";
    public const string PatientFullName = "John Doe";
    public const string NurseFullName = "Nurse Joy";

    // =========================================================================
    // ORGANIZATION IDS
    // =========================================================================
    public static readonly Guid HospitalAOrgId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid HospitalBOrgId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid ClinicOrgId = Guid.Parse("11111111-1111-1111-1111-111111111103");

    // Organization names
    public const string HospitalAName = "Benh vien Da khoa Trung uong";
    public const string HospitalBName = "Benh vien Nhi Dong 1";
    public const string ClinicName = "Phong kham Da lieu Sai Gon";

    // =========================================================================
    // DEPARTMENT IDS
    // =========================================================================
    public static readonly Guid CardiologyDeptId = Guid.Parse("22222222-2222-2222-2222-222222222201");
    public static readonly Guid EmergencyDeptId = Guid.Parse("22222222-2222-2222-2222-222222222202");
    public static readonly Guid PediatricsDeptId = Guid.Parse("22222222-2222-2222-2222-222222222203");
    public static readonly Guid PharmacyDeptId = Guid.Parse("22222222-2222-2222-2222-222222222204");
    public static readonly Guid ReceptionDeptId = Guid.Parse("22222222-2222-2222-2222-222222222205");

    // =========================================================================
    // MEMBERSHIP IDS
    // =========================================================================
    public static readonly Guid AdminMembershipId = Guid.Parse("33333333-3333-3333-3333-333333333301");
    public static readonly Guid DoctorMembershipId = Guid.Parse("33333333-3333-3333-3333-333333333302");
    public static readonly Guid PharmacistMembershipId = Guid.Parse("33333333-3333-3333-3333-333333333303");
    public static readonly Guid NurseMembershipId = Guid.Parse("33333333-3333-3333-3333-333333333304");
    public static readonly Guid PatientMembershipId = Guid.Parse("33333333-3333-3333-3333-333333333305");
    public static readonly Guid ReceptionistMembershipId = Guid.Parse("33333333-3333-3333-3333-333333333306");
}
