using System.ComponentModel.DataAnnotations;
using DBH.Organization.Service.Models.Enums;

namespace DBH.Organization.Service.DTOs;

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