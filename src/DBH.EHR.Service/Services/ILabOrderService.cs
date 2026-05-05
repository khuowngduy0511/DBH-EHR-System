using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Enums;

namespace DBH.EHR.Service.Services;

public interface ILabOrderService
{
    /// <summary>Doctor tạo chỉ định xét nghiệm</summary>
    Task<LabOrderResponseDto> CreateAsync(CreateLabOrderDto dto, Guid doctorUserId);

    /// <summary>Lấy chi tiết một lab order</summary>
    Task<LabOrderResponseDto?> GetByIdAsync(Guid labOrderId);

    /// <summary>LabTech xem danh sách chỉ định theo org (có lọc theo status)</summary>
    Task<IEnumerable<LabOrderResponseDto>> GetByOrgAsync(Guid orgId, LabOrderStatus? status = null);

    /// <summary>Bác sĩ xem chỉ định XN theo EHR</summary>
    Task<IEnumerable<LabOrderResponseDto>> GetByEhrAsync(Guid ehrId);

    /// <summary>Tổng hợp chỉ định theo bệnh nhân</summary>
    Task<IEnumerable<LabOrderResponseDto>> GetByPatientAsync(Guid patientId);

    /// <summary>LabTech cập nhật trạng thái (RECEIVED / IN_PROGRESS / COMPLETED / CANCELLED)</summary>
    Task<LabOrderResponseDto?> UpdateStatusAsync(Guid labOrderId, Guid labTechUserId, LabOrderStatus newStatus);

    /// <summary>LabTech nhập kết quả xét nghiệm có cấu trúc</summary>
    Task<LabOrderResponseDto?> SubmitResultAsync(Guid labOrderId, Guid labTechUserId, SubmitLabResultDto dto);

    /// <summary>Doctor/Admin hủy chỉ định (chỉ khi PENDING)</summary>
    Task<bool> CancelAsync(Guid labOrderId, Guid requesterId);
}
