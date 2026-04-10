using DBH.Organization.Service.Models.Entities;
using DBH.Organization.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DBH.Organization.Service.DbContext;

public class OrganizationDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Models.Entities.Organization> Organizations { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Membership> Memberships { get; set; } = null!;
    public DbSet<PaymentConfig> PaymentConfigs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>()
            .HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Membership>()
            .HasOne(m => m.Organization)
            .WithMany(o => o.Memberships)
            .HasForeignKey(m => m.OrgId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Membership>()
            .HasOne(m => m.Department)
            .WithMany(d => d.Memberships)
            .HasForeignKey(m => m.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
            
        modelBuilder.Entity<Department>()
            .HasOne(d => d.Organization)
            .WithMany(o => o.Departments)
            .HasForeignKey(d => d.OrgId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentConfig>()
            .HasOne(pc => pc.Organization)
            .WithOne(o => o.PaymentConfig)
            .HasForeignKey<PaymentConfig>(pc => pc.OrgId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PaymentConfig>()
            .HasIndex(pc => pc.OrgId)
            .IsUnique();

        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var hospitalAOrgId = Guid.Parse("11111111-1111-1111-1111-111111111101");
        var hospitalBOrgId = Guid.Parse("11111111-1111-1111-1111-111111111102");
        var clinicOrgId = Guid.Parse("11111111-1111-1111-1111-111111111103");

        var adminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var doctorUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var pharmacistUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var nurseUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var patientUserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var receptionistUserId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        var seedTime = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc);

        modelBuilder.Entity<Models.Entities.Organization>().HasData(
            new Models.Entities.Organization
            {
                OrgId = hospitalAOrgId,
                OrgDid = "did:dbh:org:hospital-a",
                OrgName = "Benh vien Da khoa Trung uong",
                OrgCode = "BVDKTU",
                OrgType = OrganizationType.HOSPITAL,
                LicenseNumber = "BV-HCM-001",
                TaxId = "0301234567",
                Address = "{\"line\":[\"215 Hong Bang\"],\"city\":\"Ho Chi Minh\",\"district\":\"Quan 5\",\"country\":\"VN\",\"postalCode\":\"700000\"}",
                ContactInfo = "{\"phone\":\"028-3855-4269\",\"fax\":\"028-3855-4270\",\"email\":\"contact@bvdktu.vn\",\"hotline\":\"1900-1234\"}",
                Website = "https://bvdktu.vn",
                FabricMspId = "Hospital1MSP",
                FabricChannelPeers = "[\"peer0.hospital1.ehr.com\"]",
                FabricCaUrl = "http://ca_hospital1:7054",
                Status = OrganizationStatus.ACTIVE,
                VerifiedAt = seedTime,
                VerifiedBy = adminUserId,
                Timezone = "Asia/Ho_Chi_Minh",
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Models.Entities.Organization
            {
                OrgId = hospitalBOrgId,
                OrgDid = "did:dbh:org:hospital-b",
                OrgName = "Benh vien Nhi Dong 1",
                OrgCode = "BVND1",
                OrgType = OrganizationType.HOSPITAL,
                LicenseNumber = "BV-HCM-002",
                TaxId = "0301234568",
                Address = "{\"line\":[\"341 Su Van Hanh\"],\"city\":\"Ho Chi Minh\",\"district\":\"Quan 10\",\"country\":\"VN\",\"postalCode\":\"700000\"}",
                ContactInfo = "{\"phone\":\"028-3927-1119\",\"fax\":\"028-3927-1120\",\"email\":\"contact@bvnd1.vn\",\"hotline\":\"1900-5678\"}",
                Website = "https://bvnd1.vn",
                FabricMspId = "Hospital2MSP",
                FabricChannelPeers = "[\"peer0.hospital2.ehr.com\"]",
                FabricCaUrl = "http://ca_hospital2:8054",
                Status = OrganizationStatus.ACTIVE,
                VerifiedAt = seedTime,
                VerifiedBy = adminUserId,
                Timezone = "Asia/Ho_Chi_Minh",
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Models.Entities.Organization
            {
                OrgId = clinicOrgId,
                OrgDid = "did:dbh:org:clinic-a",
                OrgName = "Phong kham Da lieu Sai Gon",
                OrgCode = "PKDLSG",
                OrgType = OrganizationType.CLINIC,
                LicenseNumber = "PK-HCM-001",
                TaxId = "0301234569",
                Address = "{\"line\":[\"123 Nguyen Hue\"],\"city\":\"Ho Chi Minh\",\"district\":\"Quan 1\",\"country\":\"VN\",\"postalCode\":\"700000\"}",
                ContactInfo = "{\"phone\":\"028-3821-0000\",\"fax\":\"028-3821-0001\",\"email\":\"contact@pkdlsg.vn\",\"hotline\":\"1900-9012\"}",
                Website = "https://pkdlsg.vn",
                FabricMspId = "ClinicMSP",
                FabricChannelPeers = "[\"peer0.clinic.ehr.com\"]",
                FabricCaUrl = "http://ca_clinic:10054",
                Status = OrganizationStatus.ACTIVE,
                VerifiedAt = seedTime,
                VerifiedBy = adminUserId,
                Timezone = "Asia/Ho_Chi_Minh",
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            }
        );

        var cardiologyDeptId = Guid.Parse("22222222-2222-2222-2222-222222222201");
        var emergencyDeptId = Guid.Parse("22222222-2222-2222-2222-222222222202");
        var pediatricsDeptId = Guid.Parse("22222222-2222-2222-2222-222222222203");
        var pharmacyDeptId = Guid.Parse("22222222-2222-2222-2222-222222222204");
        var receptionDeptId = Guid.Parse("22222222-2222-2222-2222-222222222205");

        modelBuilder.Entity<Department>().HasData(
            new Department
            {
                DepartmentId = cardiologyDeptId,
                OrgId = hospitalAOrgId,
                DepartmentName = "Khoa Tim mach",
                DepartmentCode = "TM",
                Description = "Khoa Tim mach chuyen sau chan doan va dieu tri benh tim",
                Floor = "3",
                RoomNumbers = "301-308",
                PhoneExtension = "3001",
                Status = DepartmentStatus.ACTIVE,
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Department
            {
                DepartmentId = pharmacyDeptId,
                OrgId = hospitalAOrgId,
                DepartmentName = "Khoa Duoc",
                DepartmentCode = "DUOC",
                Description = "Cap phat thuoc va tu van duoc lam sang",
                Floor = "1",
                RoomNumbers = "101-104",
                PhoneExtension = "1002",
                Status = DepartmentStatus.ACTIVE,
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Department
            {
                DepartmentId = receptionDeptId,
                OrgId = hospitalAOrgId,
                DepartmentName = "Quay tiep nhan",
                DepartmentCode = "TN",
                Description = "Tiep nhan, dang ky va phan luong benh nhan",
                Floor = "1",
                RoomNumbers = "L1-01",
                PhoneExtension = "1001",
                Status = DepartmentStatus.ACTIVE,
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Department
            {
                DepartmentId = pediatricsDeptId,
                OrgId = hospitalBOrgId,
                DepartmentName = "Khoa Nhi",
                DepartmentCode = "NHI",
                Description = "Khoa Nhi tong hop va cham soc tre em",
                Floor = "2",
                RoomNumbers = "201-208",
                PhoneExtension = "2001",
                Status = DepartmentStatus.ACTIVE,
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Department
            {
                DepartmentId = emergencyDeptId,
                OrgId = hospitalBOrgId,
                DepartmentName = "Phong cap cuu",
                DepartmentCode = "CC",
                Description = "Tiep nhan benh nhan cap cuu 24/7",
                Floor = "1",
                RoomNumbers = "101-106",
                PhoneExtension = "1101",
                Status = DepartmentStatus.ACTIVE,
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            }
        );

        modelBuilder.Entity<Membership>().HasData(
            new Membership
            {
                MembershipId = Guid.Parse("33333333-3333-3333-3333-333333333301"),
                UserId = adminUserId,
                OrgId = hospitalAOrgId,
                DepartmentId = null,
                EmployeeId = "EMP-ADM-001",
                JobTitle = "System Admin",
                OrgPermissions = "[\"ALL\"]",
                StartDate = new DateOnly(2024, 1, 1),
                Status = MembershipStatus.ACTIVE,
                Notes = "Tai khoan quan tri he thong",
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Membership
            {
                MembershipId = Guid.Parse("33333333-3333-3333-3333-333333333302"),
                UserId = doctorUserId,
                OrgId = hospitalAOrgId,
                DepartmentId = cardiologyDeptId,
                EmployeeId = "EMP-DOC-001",
                JobTitle = "Bac si chuyen khoa Tim mach",
                LicenseNumber = "VN-DOC-001",
                Specialty = "Tim mach",
                Qualifications = "[\"Dai hoc Y Duoc TP.HCM\",\"Thac si Tim mach\"]",
                StartDate = new DateOnly(2024, 1, 10),
                Status = MembershipStatus.ACTIVE,
                OrgPermissions = "[\"VIEW_PATIENTS\",\"CREATE_RECORDS\"]",
                Notes = "Bac si chinh Khoa Tim mach",
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Membership
            {
                MembershipId = Guid.Parse("33333333-3333-3333-3333-333333333303"),
                UserId = pharmacistUserId,
                OrgId = hospitalAOrgId,
                DepartmentId = pharmacyDeptId,
                EmployeeId = "EMP-STF-001",
                JobTitle = "Duoc si lam sang",
                LicenseNumber = "DS-001",
                Specialty = "Duoc lam sang",
                Qualifications = "[\"Dai hoc Duoc\",\"Chung chi Duoc lam sang\"]",
                StartDate = new DateOnly(2024, 1, 12),
                Status = MembershipStatus.ACTIVE,
                OrgPermissions = "[\"VIEW_PRESCRIPTIONS\",\"DISPENSE_DRUGS\"]",
                Notes = "Phu trach cap phat thuoc",
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Membership
            {
                MembershipId = Guid.Parse("33333333-3333-3333-3333-333333333304"),
                UserId = nurseUserId,
                OrgId = hospitalBOrgId,
                DepartmentId = pediatricsDeptId,
                EmployeeId = "EMP-STF-002",
                JobTitle = "Dieu duong Nhi khoa",
                LicenseNumber = "DD-001",
                Specialty = "Dieu duong Nhi",
                Qualifications = "[\"Cu nhan Dieu duong\",\"Chung chi Nhi khoa\"]",
                StartDate = new DateOnly(2024, 2, 1),
                Status = MembershipStatus.ACTIVE,
                OrgPermissions = "[\"VIEW_PATIENTS\",\"UPDATE_VITALS\"]",
                Notes = "Dieu duong khoa Nhi",
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Membership
            {
                MembershipId = Guid.Parse("33333333-3333-3333-3333-333333333305"),
                UserId = patientUserId,
                OrgId = clinicOrgId,
                DepartmentId = null,
                EmployeeId = null,
                JobTitle = "Patient",
                StartDate = new DateOnly(2024, 1, 15),
                Status = MembershipStatus.ACTIVE,
                OrgPermissions = "[\"READ_OWN_RECORDS\"]",
                Notes = "Benh nhan dang ky tai phong kham",
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            },
            new Membership
            {
                MembershipId = Guid.Parse("33333333-3333-3333-3333-333333333306"),
                UserId = receptionistUserId,
                OrgId = hospitalAOrgId,
                DepartmentId = receptionDeptId,
                EmployeeId = "EMP-STF-003",
                JobTitle = "Nhan vien tiep nhan",
                LicenseNumber = "LT-001",
                Specialty = "Tiep nhan",
                Qualifications = "[\"Trung cap Y\",\"Chung chi tiep nhan benh nhan\"]",
                StartDate = new DateOnly(2024, 1, 20),
                Status = MembershipStatus.ACTIVE,
                OrgPermissions = "[\"VIEW_PATIENTS\",\"CREATE_APPOINTMENTS\"]",
                Notes = "Le tan khu tiep nhan",
                CreatedAt = seedTime,
                UpdatedAt = seedTime
            }
        );
    }
}
