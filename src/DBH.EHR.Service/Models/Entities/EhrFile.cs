using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.EHR.Service.Models.Enums;

namespace DBH.EHR.Service.Models.Entities;


[Table("ehr_files")]
public class EhrFile
{
    [Key]
    [Column("file_id")]
    public Guid FileId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("ehr_id")]
    public Guid EhrId { get; set; }

    [Column("version")]
    public int Version { get; set; }


    [Column("report_type")]
    public ReportType ReportType { get; set; }

    /// <summary>
    /// S3/IPFS path
    /// </summary>
    [Column("file_url")]
    [MaxLength(1000)]
    public string? FileUrl { get; set; }

    /// <summary>
    /// Hash để kiểm tra 
    /// </summary>
    [Column("file_hash")]
    [MaxLength(255)]
    public string? FileHash { get; set; }

    [Column("mime_type")]
    [MaxLength(100)]
    public string? MimeType { get; set; }

    [Column("size_bytes")]
    public long? SizeBytes { get; set; }

    /// User tạo file (doctor/nurse/pharmacist/lab)
    [Column("created_by")]
    public Guid? CreatedBy { get; set; }


    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("EhrId")]
    public virtual EhrRecord? EhrRecord { get; set; }
}
