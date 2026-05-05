using System.ComponentModel.DataAnnotations;
using DBH.EHR.Service.Models.Enums;

namespace DBH.EHR.Service.Models.DTOs;

// ─────────────────────────────────────────────────────
//  Request DTOs
// ─────────────────────────────────────────────────────

/// <summary>
/// Doctor tạo chỉ định xét nghiệm
/// </summary>
public class CreateLabOrderDto
{
    [Required]
    public Guid EhrId { get; set; }

    [Required]
    public Guid PatientId { get; set; }

    public Guid? OrgId { get; set; }

    /// <summary>Loại xét nghiệm: CBC, Glucose, X-Quang, CT Ngực, v.v.</summary>
    [Required]
    [MaxLength(200)]
    public string TestType { get; set; } = string.Empty;

    /// <summary>Ghi chú lâm sàng</summary>
    public string? ClinicalNote { get; set; }
}

/// <summary>
/// LabTech cập nhật trạng thái chỉ định
/// </summary>
public class UpdateLabOrderStatusDto
{
    [Required]
    public LabOrderStatus Status { get; set; }
}

/// <summary>
/// Một chỉ số trong kết quả xét nghiệm
/// </summary>
public class LabResultItemDto
{
    /// <summary>Tên chỉ số (ví dụ: Hemoglobin, WBC, ...)</summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>Giá trị đo được</summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>Đơn vị (g/dL, 10³/μL, ...)</summary>
    public string? Unit { get; set; }

    /// <summary>Khoảng tham chiếu (12.0-16.0)</summary>
    public string? RefRange { get; set; }

    /// <summary>Cờ: H (High), L (Low), N (Normal), C (Critical)</summary>
    public string? Flag { get; set; }
}

/// <summary>
/// LabTech nhập kết quả xét nghiệm
/// </summary>
public class SubmitLabResultDto
{
    /// <summary>Danh sách các chỉ số kết quả (có thể rỗng nếu chỉ upload file)</summary>
    public List<LabResultItemDto>? ResultItems { get; set; }

    /// <summary>Nhận xét tổng quát của KTV</summary>
    public string? ResultNote { get; set; }

    /// <summary>EHR File ID đã upload (file kết quả PDF/ảnh, upload riêng qua EHR file API)</summary>
    public Guid? AttachedFileId { get; set; }
}

// ─────────────────────────────────────────────────────
//  Response DTOs
// ─────────────────────────────────────────────────────

/// <summary>
/// Response đầy đủ một lab order
/// </summary>
public class LabOrderResponseDto
{
    public Guid LabOrderId { get; set; }
    public Guid EhrId { get; set; }
    public Guid PatientId { get; set; }
    public AuthUserProfileDetailDto? PatientProfile { get; set; }
    public Guid RequestedBy { get; set; }
    public AuthUserProfileDetailDto? DoctorProfile { get; set; }
    public Guid? AssignedTo { get; set; }
    public AuthUserProfileDetailDto? LabTechProfile { get; set; }
    public Guid? OrgId { get; set; }
    public string TestType { get; set; } = string.Empty;
    public string? ClinicalNote { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ResultNote { get; set; }
    public List<LabResultItemDto>? ResultItems { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
