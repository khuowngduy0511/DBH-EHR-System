using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using DBH.Appointment.Service.Models.Enums;

namespace DBH.Appointment.Service.DTOs;

// =============================================================================
// Appointment DTOs
// =============================================================================

/// <summary>
/// Yêu cầu tạo lịch hẹn khám bệnh mới (Flow 3: Đặt lịch khám)
/// </summary>
public class CreateAppointmentRequest
{
    /// <summary>Mã bệnh nhân (từ Auth Service)</summary>
    [Required]
    public Guid PatientId { get; set; }

    /// <summary>Mã bác sĩ được chọn</summary>
    [Required]
    public Guid DoctorId { get; set; }

    /// <summary>Mã tổ chức / cơ sở y tế</summary>
    [Required]
    public Guid OrgId { get; set; }

    /// <summary>Ngày giờ hẹn khám (UTC)</summary>
    [Required]
    public DateTime ScheduledAt { get; set; }
}

/// <summary>
/// Yêu cầu cập nhật trạng thái hoặc thời gian lịch hẹn
/// </summary>
public class UpdateAppointmentRequest
{
    /// <summary>Trạng thái mới (PENDING, CONFIRMED, CHECKED_IN, IN_PROGRESS, COMPLETED, CANCELLED, NO_SHOW, RESCHEDULED)</summary>
    public AppointmentStatus? Status { get; set; }

    /// <summary>Ngày giờ hẹn mới (nếu cần dời lịch)</summary>
    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Yêu cầu hủy lịch hẹn (bệnh nhân hoặc bác sĩ)
/// </summary>
public class CancelAppointmentRequest
{
    /// <summary>Lý do hủy lịch hẹn</summary>
    [Required]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Yêu cầu xác nhận lịch hẹn (bác sĩ xác nhận)
/// </summary>
public class ConfirmAppointmentRequest
{
    /// <summary>Ghi chú thêm từ bác sĩ (tuỳ chọn)</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Yêu cầu check-in khi bệnh nhân đến cơ sở y tế
/// </summary>
public class CheckInRequest
{
    /// <summary>Ghi chú khi check-in (tuỳ chọn)</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Thông tin lịch hẹn trả về cho client
/// </summary>
public class AppointmentResponse
{
    /// <summary>Mã lịch hẹn</summary>
    public Guid AppointmentId { get; set; }

    /// <summary>Mã bệnh nhân</summary>
    public Guid PatientId { get; set; }

    /// <summary>Mã bác sĩ</summary>
    public Guid DoctorId { get; set; }

    /// <summary>Mã tổ chức / cơ sở y tế</summary>
    public Guid OrgId { get; set; }

    /// <summary>Ngày giờ hẹn khám (UTC)</summary>
    public DateTime ScheduledAt { get; set; }

    /// <summary>Trạng thái hiện tại (PENDING, CONFIRMED, CHECKED_IN, IN_PROGRESS, COMPLETED, CANCELLED, ...)</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Thời điểm tạo lịch hẹn</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Số lượt khám (encounter) liên kết với lịch hẹn này</summary>
    public int EncounterCount { get; set; }
}

// =============================================================================
// Encounter DTOs
// =============================================================================

/// <summary>
/// Yêu cầu tạo lượt khám mới — bác sĩ bắt đầu khám bệnh nhân (Flow 4)
/// </summary>
public class CreateEncounterRequest
{
    /// <summary>Mã bệnh nhân</summary>
    [Required]
    public Guid PatientId { get; set; }

    /// <summary>Mã bác sĩ thực hiện khám</summary>
    [Required]
    public Guid DoctorId { get; set; }

    /// <summary>Mã lịch hẹn liên kết</summary>
    [Required]
    public Guid AppointmentId { get; set; }

    /// <summary>Mã tổ chức / cơ sở y tế</summary>
    [Required]
    public Guid OrgId { get; set; }

    /// <summary>Ghi chú ban đầu của bác sĩ (tuỳ chọn)</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Yêu cầu cập nhật ghi chú lượt khám
/// </summary>
public class UpdateEncounterRequest
{
    /// <summary>Ghi chú lâm sàng cập nhật</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Yêu cầu hoàn tất lượt khám — kết thúc khám và tự động tạo hồ sơ bệnh án (EHR).
/// Chẩn đoán + Phác đồ điều trị được đưa vào EhrData → lưu trong ehr_records.data (jsonb).
/// </summary>
public class CompleteEncounterRequest
{
    /// <summary>Ghi chú cuối cùng của bác sĩ</summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Dữ liệu hồ sơ bệnh án — tự động tạo EHR record.
    /// Nên bao gồm: diagnosis (chẩn đoán), treatment_plan (phác đồ điều trị), vitals, v.v.
    /// </summary>
    public JsonElement? EhrData { get; set; }
}

/// <summary>
/// Thông tin lượt khám trả về cho client
/// </summary>
public class EncounterResponse
{
    /// <summary>Mã lượt khám</summary>
    public Guid EncounterId { get; set; }

    /// <summary>Mã bệnh nhân</summary>
    public Guid PatientId { get; set; }

    /// <summary>Mã bác sĩ thực hiện khám</summary>
    public Guid DoctorId { get; set; }

    /// <summary>Mã lịch hẹn liên kết</summary>
    public Guid AppointmentId { get; set; }

    /// <summary>Mã tổ chức / cơ sở y tế</summary>
    public Guid OrgId { get; set; }

    /// <summary>Ghi chú lâm sàng</summary>
    public string? Notes { get; set; }

    /// <summary>Thời điểm tạo lượt khám</summary>
    public DateTime CreatedAt { get; set; }
}

// =============================================================================
// Doctor Search DTOs
// =============================================================================

/// <summary>
/// Truy vấn tìm kiếm bác sĩ theo chuyên khoa — gọi sang Organization Service
/// </summary>
public class SearchDoctorQuery
{
    /// <summary>Chuyên khoa (VD: "Nội khoa", "Tim mạch", "Nhi")</summary>
    public string? Specialty { get; set; }

    /// <summary>Lọc theo tổ chức / cơ sở y tế (tuỳ chọn)</summary>
    public Guid? OrgId { get; set; }

    /// <summary>Trang hiện tại (mặc định: 1)</summary>
    public int Page { get; set; } = 1;

    /// <summary>Số kết quả mỗi trang (mặc định: 10)</summary>
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Kết quả tìm kiếm bác sĩ
/// </summary>
public class DoctorSearchResult
{
    /// <summary>Mã bác sĩ (membership ID)</summary>
    public Guid DoctorId { get; set; }

    /// <summary>Mã người dùng (user ID từ Auth Service)</summary>
    public Guid UserId { get; set; }

    /// <summary>Họ tên bác sĩ</summary>
    public string? Name { get; set; }

    /// <summary>Chuyên khoa</summary>
    public string? Specialty { get; set; }

    /// <summary>Số giấy phép hành nghề</summary>
    public string? LicenseNumber { get; set; }

    /// <summary>Mã tổ chức / cơ sở y tế</summary>
    public Guid? OrgId { get; set; }

    /// <summary>Tên tổ chức / cơ sở y tế</summary>
    public string? OrgName { get; set; }
}

// =============================================================================
// Common Response
// =============================================================================

/// <summary>
/// Phản hồi API chuẩn — bao bọc dữ liệu trả về kèm trạng thái và thông báo
/// </summary>
public class ApiResponse<T>
{
    /// <summary>Thành công hay thất bại</summary>
    public bool Success { get; set; }

    /// <summary>Thông báo mô tả kết quả</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Dữ liệu trả về (nếu thành công)</summary>
    public T? Data { get; set; }
}

/// <summary>
/// Phản hồi API có phân trang — dùng cho danh sách
/// </summary>
public class PagedResponse<T>
{
    /// <summary>Thành công hay thất bại</summary>
    public bool Success { get; set; } = true;

    /// <summary>Danh sách kết quả</summary>
    public List<T> Data { get; set; } = new();

    /// <summary>Trang hiện tại</summary>
    public int Page { get; set; }

    /// <summary>Số kết quả mỗi trang</summary>
    public int PageSize { get; set; }

    /// <summary>Tổng số kết quả</summary>
    public int TotalCount { get; set; }

    /// <summary>Tổng số trang</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
