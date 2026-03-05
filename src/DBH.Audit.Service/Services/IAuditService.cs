using DBH.Audit.Service.DTOs;
using DBH.Audit.Service.Models.Enums;

namespace DBH.Audit.Service.Services;

public interface IAuditService
{
    // CRUD Operations
    Task<ApiResponse<AuditLogResponse>> CreateAuditLogAsync(CreateAuditLogRequest request);
    Task<ApiResponse<AuditLogResponse>> GetAuditLogByIdAsync(Guid auditId);

    // Query Operations
    Task<PagedResponse<AuditLogResponse>> SearchAuditLogsAsync(AuditLogQueryParams query);
    Task<PagedResponse<AuditLogResponse>> GetAuditLogsByPatientAsync(Guid patientId, int page, int pageSize);
    Task<PagedResponse<AuditLogResponse>> GetAuditLogsByActorAsync(Guid actorUserId, int page, int pageSize);
    Task<PagedResponse<AuditLogResponse>> GetAuditLogsByTargetAsync(Guid targetId, TargetType targetType, int page, int pageSize);

    // Statistics
    Task<AuditStatsResponse> GetAuditStatsAsync(Guid? organizationId, DateTime? fromDate, DateTime? toDate);

    // Blockchain sync
    Task<ApiResponse<AuditLogResponse>> SyncFromBlockchainAsync(string blockchainAuditId);
}
