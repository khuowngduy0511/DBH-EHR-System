using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.EHR.Service.Models.Enums;

namespace DBH.EHR.Service.Models.Entities;


/// Log truy cập EHR 
[Table("ehr_access_log")]
public class EhrAccessLog
{
    [Key]
    [Column("access_id")]
    public Guid AccessId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("ehr_id")]
    public Guid EhrId { get; set; }

    [Column("version")]
    public int Version { get; set; }

    /// User truy cập
    [Column("accessed_by")]
    public Guid AccessedBy { get; set; }

    /// Hành động: VIEW, DOWNLOAD, UPDATE
    [Column("access_action")]
    [MaxLength(50)]
    public AccessAction AccessAction { get; set; }

    /// Link đến consent 
    [Column("consent_id")]
    public Guid? ConsentId { get; set; }

    [Column("ip_address")]
    [MaxLength(100)]
    public string? IpAddress { get; set; }

    /// Kết quả kiểm tra hash: PASS, FAIL
    [Column("verify_status")]
    [MaxLength(10)]
    public VerifyStatus VerifyStatus { get; set; }

    [Column("accessed_at")]
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("EhrId")]
    public virtual EhrRecord? EhrRecord { get; set; }
}
