using System.ComponentModel.DataAnnotations;
using DBH.Organization.Service.Models.Enums;

namespace DBH.Organization.Service.DTOs;

// =============================================================================
// Organization DTOs
// =============================================================================

public class CreateOrganizationRequest
{
    [Required]
    [MaxLength(255)]
    public string OrgName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? OrgCode { get; set; }

    public OrganizationType OrgType { get; set; } = OrganizationType.HOSPITAL;

    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [MaxLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// JSON format: { "line": ["123 Street"], "city": "Ho Chi Minh", "district": "Quan 1", "country": "VN" }
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// JSON format: { "phone": "028-xxx", "email": "contact@hospital.vn" }
    /// </summary>
    public string? ContactInfo { get; set; }

    [MaxLength(255)]
    public string? Website { get; set; }
}

public class UpdateOrganizationRequest
{
    [MaxLength(255)]
    public string? OrgName { get; set; }

    [MaxLength(50)]
    public string? OrgCode { get; set; }

    public OrganizationType? OrgType { get; set; }

    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [MaxLength(50)]
    public string? TaxId { get; set; }

    public string? Address { get; set; }

    public string? ContactInfo { get; set; }

    [MaxLength(255)]
    public string? Website { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    public string? Settings { get; set; }
}

public class OrganizationResponse
{
    public Guid OrgId { get; set; }
    public string? OrgDid { get; set; }
    public string OrgName { get; set; } = string.Empty;
    public string? OrgCode { get; set; }
    public OrganizationType OrgType { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Address { get; set; }
    public string? ContactInfo { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public OrganizationStatus Status { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DepartmentCount { get; set; }
    public int MemberCount { get; set; }
}

// =============================================================================
// Department DTOs
// =============================================================================

public class CreateDepartmentRequest
{
    [Required]
    public Guid OrgId { get; set; }

    [Required]
    [MaxLength(100)]
    public string DepartmentName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DepartmentCode { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public Guid? HeadUserId { get; set; }

    public Guid? ParentDepartmentId { get; set; }

    [MaxLength(20)]
    public string? Floor { get; set; }

    [MaxLength(100)]
    public string? RoomNumbers { get; set; }

    [MaxLength(20)]
    public string? PhoneExtension { get; set; }
}

public class UpdateDepartmentRequest
{
    [MaxLength(100)]
    public string? DepartmentName { get; set; }

    [MaxLength(20)]
    public string? DepartmentCode { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public Guid? HeadUserId { get; set; }

    public Guid? ParentDepartmentId { get; set; }

    [MaxLength(20)]
    public string? Floor { get; set; }

    [MaxLength(100)]
    public string? RoomNumbers { get; set; }

    [MaxLength(20)]
    public string? PhoneExtension { get; set; }

    public DepartmentStatus? Status { get; set; }
}

public class DepartmentResponse
{
    public Guid DepartmentId { get; set; }
    public Guid OrgId { get; set; }
    public string? OrgName { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? DepartmentCode { get; set; }
    public string? Description { get; set; }
    public Guid? HeadUserId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public string? Floor { get; set; }
    public string? RoomNumbers { get; set; }
    public DepartmentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
}

// =============================================================================
// Membership DTOs
// =============================================================================

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

public class MembershipResponse
{
    public Guid MembershipId { get; set; }
    public Guid UserId { get; set; }
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

// =============================================================================
// Common Response
// =============================================================================

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public class PagedResponse<T>
{
    public bool Success { get; set; } = true;
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
