using DBH.Audit.Service.Models.Enums;

namespace DBH.Audit.Service.DTOs;

// ============================================================================
// Request DTOs
// ============================================================================

/// <summary>
/// Request tạo audit log mới
/// </summary>
public class CreateAuditLogRequest
{
    public string ActorDid { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public ActorType ActorType { get; set; }
    public AuditAction Action { get; set; }
    public TargetType TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? PatientDid { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? ConsentId { get; set; }
    public Guid? OrganizationId { get; set; }
    public AuditResult Result { get; set; }
    public string? Metadata { get; set; }
    public string? ErrorMessage { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Query params cho search audit logs
/// </summary>
public class AuditLogQueryParams
{
    public Guid? ActorUserId { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? TargetId { get; set; }
    public TargetType? TargetType { get; set; }
    public AuditAction? Action { get; set; }
    public AuditResult? Result { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// ============================================================================
// Response DTOs
// ============================================================================

public class AuditLogResponse
{
    public Guid AuditId { get; set; }
    public string BlockchainAuditId { get; set; } = string.Empty;
    public string ActorDid { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public ActorType ActorType { get; set; }
    public AuditAction Action { get; set; }
    public TargetType TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? PatientDid { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? ConsentId { get; set; }
    public AuditResult Result { get; set; }
    public string? Metadata { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; }
    public string? BlockchainTxHash { get; set; }
}

/// <summary>
/// Thống kê audit
/// </summary>
public class AuditStatsResponse
{
    public int TotalLogs { get; set; }
    public int SuccessCount { get; set; }
    public int DeniedCount { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<string, int> ActionBreakdown { get; set; } = new();
}

// ============================================================================
// Common Wrappers
// ============================================================================

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true, Data = data, Message = message
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false, Message = message
    };
}

public class PagedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
