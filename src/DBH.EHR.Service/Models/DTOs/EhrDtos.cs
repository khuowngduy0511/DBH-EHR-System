using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using DBH.EHR.Service.Models.Enums;

namespace DBH.EHR.Service.Models.DTOs;


/// Request tạo EHR mới
public class CreateEhrRecordDto
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid CreatedByDoctorId { get; set; }

    /// ID lượt khám (nếu có)
    public Guid? EncounterId { get; set; }

    /// <summary>
    /// ID bệnh viện
    /// </summary>
    public Guid? HospitalId { get; set; }


    [Required]
    public ReportType ReportType { get; set; }

    /// Dữ liệu EHR (JSON)
    [Required]
    public JsonElement Data { get; set; }


    /// Metadata bổ sung
    public JsonElement? Metadata { get; set; }
}

/// Response sau khi tạo EHR
public class CreateEhrRecordResponseDto
{
    public Guid EhrId { get; set; }
    public Guid VersionId { get; set; }
    public Guid FileId { get; set; }
    public int Version { get; set; }
    public string? OffchainDocId { get; set; }
    public string? DataHash { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// Response EHR record đầy đủ
public class EhrRecordResponseDto
{
    public Guid EhrId { get; set; }
    public Guid PatientId { get; set; }
    public Guid? EncounterId { get; set; }
    public Guid? HospitalId { get; set; }
    public int CurrentVersion { get; set; }
    public EhrVersionDto? LatestVersionInfo { get; set; }
    public List<EhrFileDto>? Files { get; set; }
    public Guid CreatedByDoctorId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Chi tiết version EHR
/// </summary>
public class EhrVersionDto
{
    public Guid VersionId { get; set; }
    public int Version { get; set; }
    public string? FileHash { get; set; }
    public Guid? ChangedBy { get; set; }
    public string? ChangeReason { get; set; }
    public string? BlockchainTxHash { get; set; }
    public string? TxStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Chi tiết file EHR
/// </summary>
public class EhrFileDto
{
    public Guid FileId { get; set; }
    public int Version { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public string? FileHash { get; set; }
    public string? MimeType { get; set; }
    public long? SizeBytes { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}


