
using DBH.Auth.Service.Models.Entities;

using DBH.Auth.Service.Models.Enums;
using DBH.Shared.Infrastructure.cryptography;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace DBH.Auth.Service.DbContext;

public class AuthDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserCredential> UserCredentials { get; set; }
    public DbSet<UserSecurity> UserSecurities { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Staff> Staff { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(255);
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(50);
            entity.HasIndex(e => e.Phone).IsUnique();
            entity.Property(e => e.Password).HasColumnName("password").HasMaxLength(255);
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.PublicKey).HasColumnName("public_key");
        });

        // Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleId).HasColumnName("role_id").ValueGeneratedOnAdd();
            entity.Property(e => e.RoleName).HasColumnName("role_name").HasConversion<string>();
            entity.HasIndex(e => e.RoleName).IsUnique();
        });

        // UserRole
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.User)
                .WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Role)
                .WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserCredential
        modelBuilder.Entity<UserCredential>(entity =>
        {
            entity.ToTable("user_credentials");
            entity.HasKey(e => e.CredentialId);
            entity.Property(e => e.CredentialId).HasColumnName("credential_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Provider).HasColumnName("provider").HasConversion<string>();
            entity.Property(e => e.CredentialValue).HasColumnName("credential_value");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => new { e.UserId, e.Provider }).IsUnique();

            entity.HasOne(d => d.User)
                .WithMany(p => p.Credentials)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserSecurity
        modelBuilder.Entity<UserSecurity>(entity =>
        {
            entity.ToTable("user_security");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.MfaEnabled).HasColumnName("mfa_enabled");
            entity.Property(e => e.MfaMethod).HasColumnName("mfa_method").HasConversion<string>();
            entity.Property(e => e.LastPasswordChange).HasColumnName("last_password_change");
            entity.Property(e => e.LastMfaEnrollAt).HasColumnName("last_mfa_enroll_at");

            entity.HasOne(d => d.User)
                .WithOne(p => p.Security)
                .HasForeignKey<UserSecurity>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Doctor
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("doctors");
            entity.HasKey(e => e.DoctorId);
            entity.Property(e => e.DoctorId).HasColumnName("doctor_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Specialty).HasColumnName("specialty").HasMaxLength(255);
            entity.Property(e => e.LicenseNumber).HasColumnName("license_number").HasMaxLength(100);
            entity.Property(e => e.LicenseImage).HasColumnName("license_image").HasMaxLength(255);
            entity.Property(e => e.VerifiedStatus).HasColumnName("verified_status").HasConversion<string>();

            entity.HasOne(d => d.User)
                .WithOne(p => p.DoctorProfile)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Staff
        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("staff");
            entity.HasKey(e => e.StaffId);
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Role).HasColumnName("role").HasConversion<string>();
            entity.Property(e => e.LicenseNumber).HasColumnName("license_number").HasMaxLength(100);
            entity.Property(e => e.Specialty).HasColumnName("specialty").HasMaxLength(255);
            entity.Property(e => e.VerifiedStatus).HasColumnName("verified_status").HasConversion<string>();

            entity.HasOne(d => d.User)
                .WithOne(p => p.StaffProfile)
                .HasForeignKey<Staff>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Patient
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("patients");
            entity.HasKey(e => e.PatientId);
            entity.Property(e => e.PatientId).HasColumnName("patient_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.Dob).HasColumnName("dob");
            entity.Property(e => e.BloodType).HasColumnName("blood_type").HasMaxLength(10);
            entity.HasOne(d => d.User)
                .WithOne(p => p.PatientProfile)
                .HasForeignKey<Patient>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(e => e.PermissionId);
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // RolePermission
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");

            entity.HasOne(d => d.Role)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade);


            entity.HasOne(d => d.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Data
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var doctorRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var pharmacistRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var nurseRoleId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var patientRoleId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var receptionistRoleId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var labtechRoleId = Guid.Parse("77777777-7777-7777-7777-777777777777");

        var adminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var doctorUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var pharmacistUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var nurseUserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var patientUserId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var receptionistUserId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        var hospitalAOrgId = Guid.Parse("11111111-1111-1111-1111-111111111101");
        var hospitalBOrgId = Guid.Parse("11111111-1111-1111-1111-111111111102");
        var clinicOrgId = Guid.Parse("11111111-1111-1111-1111-111111111103");

        var userIds = new[]
        {
            adminUserId,
            doctorUserId,
            pharmacistUserId,
            nurseUserId,
            patientUserId,
            receptionistUserId
        };

        var keyPairsByUserId = userIds.ToDictionary(
            id => id,
            _ => AsymmetricEncryptionService.GenerateKeyPair());

        // 1. Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = RoleName.Admin },
            new Role { RoleId = 2, RoleName = RoleName.Doctor },
            new Role { RoleId = 3, RoleName = RoleName.Pharmacist },
            new Role { RoleId = 4, RoleName = RoleName.Nurse },
            new Role { RoleId = 5, RoleName = RoleName.Patient },
            new Role { RoleId = 6, RoleName = RoleName.Receptionist },
            new Role { RoleId = 7, RoleName = RoleName.LabTech }
        );

        // 2. Users
        var users = new[]
        {
            new User { UserId = adminUserId, FullName = "Admin User", Email = "admin@dbh.com", Password = BCrypt.Net.BCrypt.HashPassword("admin123"), Status = UserStatus.Active, CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc), Phone = "1234567890", Gender = "Male", DateOfBirth = DateTime.SpecifyKind(new DateTime(1985, 6, 15), DateTimeKind.Utc), Address = "100 Nguyen Du, Quan 1, TP.HCM", OrganizationId = hospitalAOrgId.ToString(), PublicKey = keyPairsByUserId[adminUserId].PublicKey },
            new User { UserId = doctorUserId, FullName = "Dr. House", Email = "doctor@dbh.com", Password = BCrypt.Net.BCrypt.HashPassword("doctor123"), Status = UserStatus.Active, CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc), Phone = "1234567891", Gender = "Male", DateOfBirth = DateTime.SpecifyKind(new DateTime(1980, 3, 20), DateTimeKind.Utc), Address = "50 Pasteur, Quan 1, TP.HCM", OrganizationId = hospitalAOrgId.ToString(), PublicKey = keyPairsByUserId[doctorUserId].PublicKey },
            new User { UserId = pharmacistUserId, FullName = "Pharma Joe", Email = "pharmacist@dbh.com", Password = BCrypt.Net.BCrypt.HashPassword("pharma123"), Status = UserStatus.Active, CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc), Phone = "1234567892", Gender = "Male", DateOfBirth = DateTime.SpecifyKind(new DateTime(1991, 2, 8), DateTimeKind.Utc), Address = "56 Dien Bien Phu, Binh Thanh", OrganizationId = hospitalAOrgId.ToString(), PublicKey = keyPairsByUserId[pharmacistUserId].PublicKey },
            new User { UserId = nurseUserId, FullName = "Nurse Joy", Email = "nurse@dbh.com", Password = BCrypt.Net.BCrypt.HashPassword("nurse123"), Status = UserStatus.Active, CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc), Phone = "1234567893", Gender = "Female", DateOfBirth = DateTime.SpecifyKind(new DateTime(1992, 4, 18), DateTimeKind.Utc), Address = "22 Le Van Sy, Quan 3, TP.HCM", OrganizationId = hospitalBOrgId.ToString(), PublicKey = keyPairsByUserId[nurseUserId].PublicKey },
            new User { UserId = patientUserId, FullName = "John Doe", Email = "patient@dbh.com", Password = BCrypt.Net.BCrypt.HashPassword("patient123"), Status = UserStatus.Active, CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc), Phone = "1234567894", Gender = "Male", DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 1, 1), DateTimeKind.Utc), Address = "12 Le Lai, Quan 1, TP.HCM", OrganizationId = clinicOrgId.ToString(), PublicKey = keyPairsByUserId[patientUserId].PublicKey },
            new User { UserId = receptionistUserId, FullName = "Pam Beesly", Email = "receptionist@dbh.com", Password = BCrypt.Net.BCrypt.HashPassword("receptionist123"), Status = UserStatus.Active, CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc), Phone = "1234567895", Gender = "Female", DateOfBirth = DateTime.SpecifyKind(new DateTime(1994, 6, 12), DateTimeKind.Utc), Address = "34 Pham Ngoc Thach, Quan 3, TP.HCM", OrganizationId = hospitalAOrgId.ToString(), PublicKey = keyPairsByUserId[receptionistUserId].PublicKey }
        };
        modelBuilder.Entity<User>().HasData(users);

        // 2.1 UserCredentials (seed encrypted private key for each seeded account)
        var userCredentials = users.Select(u => new UserCredential
        {
            CredentialId = Guid.NewGuid(),
            UserId = u.UserId,
            Provider = ProviderType.EncryptedPrivateKey,
            CredentialValue = MasterKeyEncryptionService.Encrypt(keyPairsByUserId[u.UserId].PrivateKey),
            CreatedAt = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc)
        }).ToArray();
        modelBuilder.Entity<UserCredential>().HasData(userCredentials);

        // 3. UserRoles
        modelBuilder.Entity<UserRole>().HasData(
            new UserRole { UserId = adminUserId, RoleId = 1 },
            new UserRole { UserId = doctorUserId, RoleId = 2 },
            new UserRole { UserId = pharmacistUserId, RoleId = 3 },
            new UserRole { UserId = nurseUserId, RoleId = 4 },
            new UserRole { UserId = patientUserId, RoleId = 5 },
            new UserRole { UserId = receptionistUserId, RoleId = 6 }
        );

        // 4. UserSecurity
        var securities = users.Select(u => new UserSecurity { UserId = u.UserId, MfaEnabled = false }).ToArray();
        modelBuilder.Entity<UserSecurity>().HasData(securities);

        // 5. Profiles (Seed basic placeholders)
        modelBuilder.Entity<Doctor>().HasData(new Doctor { DoctorId = Guid.NewGuid(), UserId = doctorUserId, LicenseNumber = "DOC123", Specialty = "General", VerifiedStatus = VerificationStatus.Verified });
        modelBuilder.Entity<Patient>().HasData(new Patient { PatientId = Guid.NewGuid(), UserId = patientUserId, Dob = new DateOnly(1990, 1, 1) });
        modelBuilder.Entity<Staff>().HasData(
            new Staff { StaffId = Guid.NewGuid(), UserId = pharmacistUserId, Role = StaffRole.Pharmacist, LicenseNumber = "PHARM123", VerifiedStatus = VerificationStatus.Verified },
            new Staff { StaffId = Guid.NewGuid(), UserId = nurseUserId, Role = StaffRole.Nurse, Specialty = "Pediatrics", VerifiedStatus = VerificationStatus.Verified },
            new Staff { StaffId = Guid.NewGuid(), UserId = receptionistUserId, Role = StaffRole.Receptionist, VerifiedStatus = VerificationStatus.Verified }
        );
    }
}
