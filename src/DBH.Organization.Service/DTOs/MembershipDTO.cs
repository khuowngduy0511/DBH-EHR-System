using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DBH.Organization.Service.Models.Enums;

namespace DBH.Organization.Service.DTOs;

public class CreateMembershipRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid OrgId { get; set; }

    public Guid? DepartmentId { get; set; }

    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    [MaxLength(100)]
    public string? JobTitle { get; set; }

    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [MaxLength(100)]
    public string? Specialty { get; set; }

    public string? Qualifications { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? OrgPermissions { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateMembershipRequest
{
    public Guid? DepartmentId { get; set; }

    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    [MaxLength(100)]
    public string? JobTitle { get; set; }

    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [MaxLength(100)]
    public string? Specialty { get; set; }

    public string? Qualifications { get; set; }

    public DateOnly? EndDate { get; set; }

    public MembershipStatus? Status { get; set; }

    public string? OrgPermissions { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class SearchDoctorsRequest
{
    public Guid? OrgId { get; set; }

    public Guid? DepartmentId { get; set; }
    
    public string? Specialty { get; set; }
    
    public string? DoctorName { get; set; }

    public string? DateOfBirth { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}

public class MembershipResponse
{
    public Guid MembershipId { get; set; }
    public MembershipUserResponse? User { get; set; }
    public Guid OrgId { get; set; }
    public string? OrgName { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? EmployeeId { get; set; }
    public string? JobTitle { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Specialty { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MembershipUserResponse
{
    public Guid UserId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public AuthUserProfileDetailDto? UserProfile { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? FullName { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Gender { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Email { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Phone { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public DateTime? DateOfBirth { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Address { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? AvatarUrl { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? OrganizationId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Status { get; set; }
}