using DBH.Auth.Service.Models.Enums;

namespace DBH.Auth.Service.DTOs;

public class CreatePatientRequest
{
    public Guid UserId { get; set; }
    public DateOnly? Dob { get; set; }
    public string? BloodType { get; set; }
}

public class UpdatePatientRequest
{
    public DateOnly? Dob { get; set; }
    public string? BloodType { get; set; }
}

public class PatientResponse
{
    public Guid PatientId { get; set; }
    public Guid UserId { get; set; }
    public DateOnly? Dob { get; set; }
    public string? BloodType { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
}

public class CreateDoctorRequest
{
    public Guid UserId { get; set; }
    public string? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseImage { get; set; }
    public VerificationStatus VerifiedStatus { get; set; } = VerificationStatus.Pending;
}

public class UpdateDoctorRequest
{
    public string? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseImage { get; set; }
    public VerificationStatus VerifiedStatus { get; set; } = VerificationStatus.Pending;
}

public class DoctorResponse
{
    public Guid DoctorId { get; set; }
    public Guid UserId { get; set; }
    public string? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseImage { get; set; }
    public VerificationStatus VerifiedStatus { get; set; }
}

public class DoctorBasicInfoResponse
{
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? OrganizationId { get; set; }
    public UserStatus Status { get; set; }
}

public class CreateStaffRequest
{
    public Guid UserId { get; set; }
    public StaffRole Role { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Specialty { get; set; }
    public VerificationStatus VerifiedStatus { get; set; } = VerificationStatus.Pending;
}

public class UpdateStaffRequest
{
    public StaffRole Role { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Specialty { get; set; }
    public VerificationStatus VerifiedStatus { get; set; } = VerificationStatus.Pending;
}

public class StaffResponse
{
    public Guid StaffId { get; set; }
    public Guid UserId { get; set; }
    public StaffRole Role { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Specialty { get; set; }
    public VerificationStatus VerifiedStatus { get; set; }
}
