# DBH.Audit.Service - API Implementation TODO

## Overview
Audit Service cần triển khai API Controllers để quản lý audit logs.
Source of truth: Blockchain (audit-channel), PostgreSQL chỉ là cache để query nhanh.

---

## 1. Folders cần tạo
```
DBH.Audit.Service/
├── Controllers/
│   └── AuditLogsController.cs
├── DTOs/
│   └── AuditLogDTOs.cs
├── Services/
│   ├── IAuditService.cs
│   └── AuditService.cs
```

---

## 2. DTOs cần tạo (DTOs/AuditLogDTOs.cs)

### Request DTOs:
```csharp
// Tạo audit log mới
public class CreateAuditLogRequest
{
    public string ActorDid { get; set; }           // DID người thực hiện
    public Guid? ActorUserId { get; set; }         // User ID
    public ActorType ActorType { get; set; }       // PATIENT, DOCTOR, NURSE, etc.
    public AuditAction Action { get; set; }        // READ, CREATE, UPDATE, DELETE, etc.
    public TargetType TargetType { get; set; }     // EHR, CONSENT, FILE, USER
    public Guid? TargetId { get; set; }            // ID đối tượng
    public string? PatientDid { get; set; }        // Patient DID liên quan
    public Guid? PatientId { get; set; }           // Patient ID
    public Guid? ConsentId { get; set; }           // Consent ID authorize
    public Guid? OrganizationId { get; set; }      // Org ID
    public AuditResult Result { get; set; }        // SUCCESS, DENIED, ERROR
    public string? Metadata { get; set; }          // JSON metadata
    public string? ErrorMessage { get; set; }      // Lỗi nếu có
    public string? IpAddress { get; set; }         // IP
    public string? UserAgent { get; set; }         // User agent
}

// Query params cho search
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
```

### Response DTOs:
```csharp
public class AuditLogResponse
{
    public Guid AuditId { get; set; }
    public string BlockchainAuditId { get; set; }
    public string ActorDid { get; set; }
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
    public string? GrantTxHash { get; set; }
}

// Audit statistics
public class AuditStatsResponse
{
    public int TotalLogs { get; set; }
    public int SuccessCount { get; set; }
    public int DeniedCount { get; set; }
    public int ErrorCount { get; set; }
    public Dictionary<string, int> ActionBreakdown { get; set; }
}
```

---

## 3. Service Interface (Services/IAuditService.cs)

```csharp
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
    Task<ApiResponse<int>> BatchSyncFromBlockchainAsync(DateTime? fromDate);
}
```

---

## 4. Controller Endpoints (Controllers/AuditLogsController.cs)

```csharp
[ApiController]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    // POST /api/audit-logs - Tạo audit log (internal use)
    // GET /api/audit-logs/{id} - Lấy audit log theo ID
    // GET /api/audit-logs/search - Search với filters
    // GET /api/audit-logs/by-patient/{patientId} - Logs của patient
    // GET /api/audit-logs/by-actor/{actorUserId} - Logs của user
    // GET /api/audit-logs/by-target/{targetId} - Logs của target object
    // GET /api/audit-logs/stats - Thống kê
    // POST /api/audit-logs/sync/{blockchainAuditId} - Sync từ blockchain
}
```

---

## 5. Program.cs cần update

```csharp
// Thêm using
using DBH.Audit.Service.Services;

// Thêm service registration
builder.Services.AddScoped<IAuditService, AuditService>();
```

---

## 6. Authorization Requirements

| Endpoint | Roles Allowed |
|----------|---------------|
| POST /audit-logs | Internal/Service (API Key) |
| GET /audit-logs/{id} | Admin, Auditor |
| GET /search | Admin, Auditor |
| GET /by-patient/{id} | Admin, Auditor, Patient (own) |
| GET /by-actor/{id} | Admin, Auditor |
| GET /stats | Admin, OrgAdmin |
| POST /sync | SystemAdmin |

---

## 7. Integration Notes

- Các service khác sẽ gọi POST /api/audit-logs để log actions
- Cần implement async queue (RabbitMQ/Azure Service Bus) cho high volume
- Blockchain sync cần chạy background job định kỳ
- Consider: Elasticsearch cho full-text search metadata

---

## 8. References

- Entity: [Models/Entities/AuditLog.cs](Models/Entities/AuditLog.cs)
- Enums: [Models/Enums/AuditEnums.cs](Models/Enums/AuditEnums.cs)
- DbContext: [Data/AuditDbContext.cs](Data/AuditDbContext.cs)
