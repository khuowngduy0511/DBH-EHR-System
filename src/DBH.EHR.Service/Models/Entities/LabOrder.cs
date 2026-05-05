using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.EHR.Service.Models.Enums;
using DBH.Shared.Infrastructure.Time;

namespace DBH.EHR.Service.Models.Entities;

/// <summary>
/// Lab Order — Chỉ định xét nghiệm
/// Bác sĩ tạo → LabTech nhận + thực hiện → Kết quả trả về EHR
/// </summary>
[Table("lab_orders")]
public class LabOrder
{
    [Key]
    [Column("lab_order_id")]
    public Guid LabOrderId { get; set; } = Guid.NewGuid();

    /// <summary>EHR record liên kết</summary>
    [Required]
    [Column("ehr_id")]
    public Guid EhrId { get; set; }

    /// <summary>Bệnh nhân cần xét nghiệm</summary>
    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    /// <summary>Doctor ra chỉ định (userId)</summary>
    [Required]
    [Column("requested_by")]
    public Guid RequestedBy { get; set; }

    /// <summary>LabTech được giao (staffId), null khi chưa nhận</summary>
    [Column("assigned_to")]
    public Guid? AssignedTo { get; set; }

    /// <summary>Tổ chức</summary>
    [Column("org_id")]
    public Guid? OrgId { get; set; }

    /// <summary>Loại xét nghiệm (CBC, Glucose, X-Quang, CT, MRI, Siêu âm...)</summary>
    [Required]
    [MaxLength(200)]
    [Column("test_type")]
    public string TestType { get; set; } = string.Empty;

    /// <summary>Ghi chú lâm sàng của bác sĩ</summary>
    [Column("clinical_note")]
    public string? ClinicalNote { get; set; }

    /// <summary>Trạng thái chỉ định</summary>
    [Column("status")]
    public LabOrderStatus Status { get; set; } = LabOrderStatus.PENDING;

    /// <summary>Nhận xét kết quả text của KTV</summary>
    [Column("result_note")]
    public string? ResultNote { get; set; }

    /// <summary>
    /// Kết quả có cấu trúc JSON: [{name, value, unit, refRange, flag}]
    /// Lưu dạng string để tránh dependency vào JSONB-specific package
    /// </summary>
    [Column("result_values")]
    public string? ResultValuesJson { get; set; }

    [Column("requested_at")]
    public DateTime RequestedAt { get; set; } = VietnamTime.DatabaseNow;

    [Column("received_at")]
    public DateTime? ReceivedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public virtual EhrRecord? EhrRecord { get; set; }
}
