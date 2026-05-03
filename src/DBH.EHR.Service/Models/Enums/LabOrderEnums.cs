namespace DBH.EHR.Service.Models.Enums;

/// <summary>
/// Trạng thái chỉ định xét nghiệm
/// PENDING  → Đã tạo, chờ LabTech nhận
/// RECEIVED → LabTech đã nhận mẫu
/// IN_PROGRESS → Đang thực hiện
/// COMPLETED → Đã có kết quả
/// CANCELLED → Đã hủy
/// </summary>
public enum LabOrderStatus
{
    PENDING,
    RECEIVED,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED
}
