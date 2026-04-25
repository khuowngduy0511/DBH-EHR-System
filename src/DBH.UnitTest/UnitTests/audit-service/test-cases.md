# Audit Service Test Cases

This document defines scenario coverage for each function declared in the service interfaces.

## IAuditService

### CreateAuditLogAsync

- Signature: Task<ApiResponse<AuditLogResponse>> CreateAuditLogAsync(CreateAuditLogRequest request);
- Return Type: Task<ApiResponse<AuditLogResponse>>
- Parameters: CreateAuditLogRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateAuditLogAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| CreateAuditLogAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| CreateAuditLogAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |

### GetAuditLogByIdAsync

- Signature: Task<ApiResponse<AuditLogResponse>> GetAuditLogByIdAsync(Guid auditId);
- Return Type: Task<ApiResponse<AuditLogResponse>>
- Parameters: Guid auditId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAuditLogByIdAsync-01 | HappyPath | Valid auditId provided | Returns success payload matching declared return type |
| GetAuditLogByIdAsync-02 | InvalidInput | auditId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetAuditLogByIdAsync-03 | NotFoundOrNoData | auditId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetAuditLogByIdAsync-AUDITID-EmptyGuid | InvalidInput | auditId = Guid.Empty | Returns validation error (400 or 422) |

### SearchAuditLogsAsync

- Signature: Task<PagedResponse<AuditLogResponse>> SearchAuditLogsAsync(AuditLogQueryParams query);
- Return Type: Task<PagedResponse<AuditLogResponse>>
- Parameters: AuditLogQueryParams query

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| SearchAuditLogsAsync-01 | HappyPath | Valid query provided | Returns success payload matching declared return type |
| SearchAuditLogsAsync-02 | InvalidInput | query with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| SearchAuditLogsAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| SearchAuditLogsAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |

### GetAuditLogsByPatientAsync

- Signature: Task<PagedResponse<AuditLogResponse>> GetAuditLogsByPatientAsync(Guid patientId, int page, int pageSize);
- Return Type: Task<PagedResponse<AuditLogResponse>>
- Parameters: Guid patientId, int page, int pageSize

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAuditLogsByPatientAsync-01 | HappyPath | Valid patientId, page, pageSize provided | Returns success payload matching declared return type |
| GetAuditLogsByPatientAsync-02 | InvalidInput | patientId = Guid.Empty OR page <= 0 OR pageSize <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetAuditLogsByPatientAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetAuditLogsByPatientAsync-04 | PagingBoundary | page = 9999, pageSize = 10 (out of range) | Returns valid paging metadata; out-of-range page returns empty item set |
| GetAuditLogsByPatientAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |
| GetAuditLogsByPatientAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetAuditLogsByPatientAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### GetAuditLogsByActorAsync

- Signature: Task<PagedResponse<AuditLogResponse>> GetAuditLogsByActorAsync(Guid actorUserId, int page, int pageSize);
- Return Type: Task<PagedResponse<AuditLogResponse>>
- Parameters: Guid actorUserId, int page, int pageSize

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAuditLogsByActorAsync-01 | HappyPath | Valid actorUserId, page, pageSize provided | Returns success payload matching declared return type |
| GetAuditLogsByActorAsync-02 | InvalidInput | actorUserId = Guid.Empty OR page <= 0 OR pageSize <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetAuditLogsByActorAsync-03 | NotFoundOrNoData | actorUserId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetAuditLogsByActorAsync-04 | PagingBoundary | page = 9999, pageSize = 10 (out of range) | Returns valid paging metadata; out-of-range page returns empty item set |
| GetAuditLogsByActorAsync-ACTORUSERID-EmptyGuid | InvalidInput | actorUserId = Guid.Empty | Returns validation error (400 or 422) |
| GetAuditLogsByActorAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetAuditLogsByActorAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### GetAuditLogsByTargetAsync

- Signature: Task<PagedResponse<AuditLogResponse>> GetAuditLogsByTargetAsync(Guid targetId, TargetType targetType, int page, int pageSize);
- Return Type: Task<PagedResponse<AuditLogResponse>>
- Parameters: Guid targetId, TargetType targetType, int page, int pageSize

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAuditLogsByTargetAsync-01 | HappyPath | Valid targetId, targetType, page, pageSize provided | Returns success payload matching declared return type |
| GetAuditLogsByTargetAsync-02 | InvalidInput | targetId = Guid.Empty OR Invalid targetType OR page <= 0 OR pageSize <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetAuditLogsByTargetAsync-03 | NotFoundOrNoData | targetId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetAuditLogsByTargetAsync-04 | PagingBoundary | page = 9999, pageSize = 10 (out of range) | Returns valid paging metadata; out-of-range page returns empty item set |
| GetAuditLogsByTargetAsync-TARGETID-EmptyGuid | InvalidInput | targetId = Guid.Empty | Returns validation error (400 or 422) |
| GetAuditLogsByTargetAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetAuditLogsByTargetAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### GetAuditStatsAsync

- Signature: Task<AuditStatsResponse> GetAuditStatsAsync(Guid? organizationId, DateTime? fromDate, DateTime? toDate);
- Return Type: Task<AuditStatsResponse>
- Parameters: Guid? organizationId, DateTime? fromDate, DateTime? toDate

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAuditStatsAsync-01 | HappyPath | Valid organizationId, fromDate, toDate provided | Returns success payload matching declared return type |
| GetAuditStatsAsync-02 | InvalidInput | organizationId = Guid.Empty OR Invalid fromDate OR Invalid toDate | Returns validation error (400 or 422) or equivalent domain error |
| GetAuditStatsAsync-03 | NotFoundOrNoData | organizationId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetAuditStatsAsync-ORGANIZATIONID-EmptyGuid | InvalidInput | organizationId = Guid.Empty | Returns validation error (400 or 422) |

### SyncFromBlockchainAsync

- Signature: Task<ApiResponse<AuditLogResponse>> SyncFromBlockchainAsync(string blockchainAuditId);
- Return Type: Task<ApiResponse<AuditLogResponse>>
- Parameters: string blockchainAuditId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| SyncFromBlockchainAsync-01 | HappyPath | Valid blockchainAuditId provided | Returns success payload matching declared return type |
| SyncFromBlockchainAsync-02 | InvalidInput | blockchainAuditId = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| SyncFromBlockchainAsync-BLOCKCHAINAUDITID-EmptyString | InvalidInput | blockchainAuditId = null or empty string | Returns validation error (400 or 422) |

