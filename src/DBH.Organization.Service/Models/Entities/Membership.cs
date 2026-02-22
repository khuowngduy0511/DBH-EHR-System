using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Organization.Service.Models.Enums;

namespace DBH.Organization.Service.Models.Entities;

/// <summary>
/// Thành viên trong tổ chức (Doctor có thể làm việc ở nhiều bệnh viện)
/// </summary>
[Table("memberships")]
public class Membership
{
    [Key]
    [Column("membership_id")]
    public Guid MembershipId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User ID (lưu ID, không FK vì User ở Auth Service)
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("org_id")]
    public Guid OrgId { get; set; }

    [Column("department_id")]
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Mã nhân viên trong tổ chức
    /// </summary>
    [Column("employee_id")]
    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    [Column("job_title")]
    [MaxLength(100)]
    public string? JobTitle { get; set; }

    /// <summary>
    /// Số giấy phép hành nghề (cho bác sĩ)
    /// </summary>
    [Column("license_number")]
    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    /// <summary>
    /// Chuyên khoa
    /// </summary>
    [Column("specialty")]
    [MaxLength(100)]
    public string? Specialty { get; set; }

    /// <summary>
    /// Bằng cấp, chứng chỉ
    /// </summary>
    [Column("qualifications", TypeName = "jsonb")]
    public string? Qualifications { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public MembershipStatus Status { get; set; } = MembershipStatus.ACTIVE;

    /// <summary>
    /// Quyền trong tổ chức
    /// </summary>
    [Column("org_permissions", TypeName = "jsonb")]
    public string? OrgPermissions { get; set; }

    [Column("notes")]
    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation (chỉ trong Organization Service)
    [ForeignKey("OrgId")]
    public virtual Organization? Organization { get; set; }

    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }
}
