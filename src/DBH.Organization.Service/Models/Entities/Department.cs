using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Organization.Service.Models.Enums;

namespace DBH.Organization.Service.Models.Entities;

/// <summary>
/// Phòng ban trong tổ chức
/// </summary>
[Table("departments")]
public class Department
{
    [Key]
    [Column("department_id")]
    public Guid DepartmentId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("org_id")]
    public Guid OrgId { get; set; }

    [Required]
    [Column("department_name")]
    [MaxLength(100)]
    public string DepartmentName { get; set; } = string.Empty;

    [Column("department_code")]
    [MaxLength(20)]
    public string? DepartmentCode { get; set; }

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Trưởng phòng (lưu User ID, không FK vì User ở service khác)
    /// </summary>
    [Column("head_user_id")]
    public Guid? HeadUserId { get; set; }

    /// <summary>
    /// Phòng ban cha (nếu có)
    /// </summary>
    [Column("parent_department_id")]
    public Guid? ParentDepartmentId { get; set; }

    [Column("floor")]
    [MaxLength(20)]
    public string? Floor { get; set; }

    [Column("room_numbers")]
    [MaxLength(100)]
    public string? RoomNumbers { get; set; }

    [Column("phone_extension")]
    [MaxLength(20)]
    public string? PhoneExtension { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public DepartmentStatus Status { get; set; } = DepartmentStatus.ACTIVE;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation (chỉ trong Organization Service)
    [ForeignKey("OrgId")]
    public virtual Organization? Organization { get; set; }

    [ForeignKey("ParentDepartmentId")]
    public virtual Department? ParentDepartment { get; set; }

    public virtual ICollection<Department> ChildDepartments { get; set; } = new List<Department>();
    public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}
