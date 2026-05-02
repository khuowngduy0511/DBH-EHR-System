using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DBH.EHR.Service.Models.DTOs;


/// <summary>
/// Request tạo EHR mới — aligned with ERD ehr_records table
/// </summary>
public class CreateEhrRecordDto
{
    [Required]
    public Guid PatientId { get; set; }

    /// <summary>
    /// ID lượt khám (encounter) liên kết
    /// </summary>
    public Guid? EncounterId { get; set; }

    /// <summary>
    /// ID tổ chức (organization)
    /// </summary>
    public Guid? OrgId { get; set; }

    /// <summary>
    /// Dữ liệu EHR (FHIR JSON, chẩn đoán, phác đồ, etc.)
    /// </summary>
    [Required]
    public JsonElement Data { get; set; }
}

/// <summary>
/// Response sau khi tạo EHR
/// </summary>
public class CreateEhrRecordResponseDto
{
    public Guid EhrId { get; set; }
    public Guid PatientId { get; set; }
    public AuthUserProfileDetailDto? PatientProfile { get; set; }
    public Guid? VersionId { get; set; }
    public Guid? FileId { get; set; }
    public int VersionNumber { get; set; }
    public string? IpfsCid { get; set; }
    public string? DataHash { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response EHR record đầy đủ
/// </summary>
public class EhrRecordResponseDto
{
    public Guid EhrId { get; set; }
    public Guid PatientId { get; set; }
    public AuthUserProfileDetailDto? PatientProfile { get; set; }
    public Guid? EncounterId { get; set; }
    public Guid? OrgId { get; set; }
    public EhrVersionDto? LatestVersionInfo { get; set; }
    public List<EhrFileDto>? Files { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Chi tiết version EHR — aligned with ERD ehr_versions table
/// </summary>
public class EhrVersionDto
{
    public Guid VersionId { get; set; }
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Chi tiết file EHR — aligned with ERD ehr_files table
/// </summary>
public class EhrFileDto
{
    public Guid FileId { get; set; }
    public string? FileUrl { get; set; }
    public string? FileHash { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request cập nhật EHR — tạo version mới
/// </summary>
public class UpdateEhrRecordDto
{
    [Required]
    public JsonElement Data { get; set; }
}

/// <summary>
/// Chi tiết version EHR (bao gồm IPFS CID)
/// </summary>
public class EhrVersionDetailDto
{
    public Guid VersionId { get; set; }
    public Guid EhrId { get; set; }
    public int VersionNumber { get; set; }
    public string? IpfsCid { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response nội dung tài liệu của một version EHR cụ thể
/// </summary>
public class EhrVersionDocumentResponseDto
{
    public Guid VersionId { get; set; }
    public Guid EhrId { get; set; }
    public Guid PatientId { get; set; }
    public AuthUserProfileDetailDto? PatientProfile { get; set; }
    public Guid? EncounterId { get; set; }
    public Guid? OrgId { get; set; }
    public int VersionNumber { get; set; }
    public string? DataHash { get; set; }
    public List<EhrFileDto>? Files { get; set; }
    public DateTime EhrCreatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Document { get; set; } = string.Empty;
}
public class EncryptIpfsPayloadRequestDto
{
    [Required]
    public string Data { get; set; } = string.Empty;
}

public class EncryptIpfsPayloadResponseDto
{
    public string IpfsCid { get; set; } = string.Empty;
    public string WrappedAesKey { get; set; } = string.Empty;
    public string DataHash { get; set; } = string.Empty;
}

public class DecryptIpfsPayloadRequestDto
{
    [Required]
    public string IpfsCid { get; set; } = string.Empty;

    [Required]
    public string WrappedAesKey { get; set; } = string.Empty;
}

public class DecryptIpfsPayloadResponseDto
{
    public string Data { get; set; } = string.Empty;
}

public class IpfsRawDownloadResponseDto
{
    public string IpfsCid { get; set; } = string.Empty;
    public string EncryptedData { get; set; } = string.Empty;
}
