namespace DBH.Shared.Contracts.Blockchain;

// ============================================================================
// Blockchain Transaction Result (shared across services)
// ============================================================================

/// <summary>
/// Kết quả trả về từ Blockchain transaction
/// </summary>
public class BlockchainTransactionResult
{
    public bool Success { get; init; }
    public string TxHash { get; init; } = string.Empty;
    public long BlockNumber { get; init; }
    public DateTime Timestamp { get; init; }
    public string? ErrorMessage { get; init; }
}

// ============================================================================
// Chaincode Function Names
// ============================================================================

/// <summary>
/// Tên các function trong chaincode - dùng làm contract giữa SDK và Chaincode
/// </summary>
public static class ChaincodeFunctions
{
    // === EHR Chaincode ===
    public const string CreateEhrHash = "CreateEhrHash";
    public const string UpdateEhrHash = "UpdateEhrHash";
    public const string GetEhrHash = "GetEhrHash";
    public const string GetEhrHistory = "GetEhrHistory";
    public const string VerifyEhrIntegrity = "VerifyEhrIntegrity";

    // === Consent Chaincode ===
    public const string GrantConsent = "GrantConsent";
    public const string RevokeConsent = "RevokeConsent";
    public const string GetConsent = "GetConsent";
    public const string GetPatientConsents = "GetPatientConsents";
    public const string VerifyConsent = "VerifyConsent";
    public const string GetConsentHistory = "GetConsentHistory";

    // === Audit Chaincode ===
    public const string CreateAuditEntry = "CreateAuditEntry";
    public const string GetAuditEntry = "GetAuditEntry";
    public const string GetAuditsByPatient = "GetAuditsByPatient";
    public const string GetAuditsByActor = "GetAuditsByActor";
    public const string GetAuditsByTarget = "GetAuditsByTarget";

    // === Emergency Contract ===
    public const string EmergencyAccess = "EmergencyAccess";
    public const string GetEmergencyAccessByRecord = "GetEmergencyAccessByRecord";
    public const string GetEmergencyAccessByAccessor = "GetEmergencyAccessByAccessor";
    public const string GetAllEmergencyAccess = "GetAllEmergencyAccess";
}

// ============================================================================
// Channel & Chaincode Names
// ============================================================================

public static class FabricChannels
{
    /// <summary>
    /// Channel lưu hash hồ sơ y tế — đảm bảo tính toàn vẹn dữ liệu
    /// </summary>
    public const string EhrHashChannel = "ehr-hash-channel";

    /// <summary>
    /// Channel quản lý quyền truy cập dữ liệu — patient kiểm soát consent
    /// </summary>
    public const string ConsentChannel = "consent-channel";

    /// <summary>
    /// Channel lưu audit log — truy vết minh bạch, không thể sửa/xóa
    /// </summary>
    public const string AuditChannel = "audit-channel";

    /// <summary>
    /// Channel tổng hợp one for all
    /// </summary>
    public const string EhrChannel = "ehr-channel";
}

public static class FabricChaincodes
{
    public const string EhrChaincode = "ehrcc";
    public const string ConsentChaincode = "consentcc";
    public const string AuditChaincode = "auditcc";
}

// ============================================================================
// Chaincode Data Models (shared between .NET SDK and Chaincode)
// ============================================================================

/// <summary>
/// EHR hash record lưu trên blockchain
/// </summary>
public class EhrHashRecord
{
    public string EhrId { get; set; } = string.Empty;
    public string PatientDid { get; set; } = string.Empty;
    public string CreatedByDid { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string ContentHash { get; set; } = string.Empty;  // SHA-256
    public string FileHash { get; set; } = string.Empty;     // SHA-256 of uploaded file
    public string Timestamp { get; set; } = string.Empty;    // ISO 8601
    public string EncryptedAesKey { get; set; } = string.Empty; // Wrapped blue key
}

/// <summary>
/// Consent record lưu trên blockchain
/// </summary>
public class ConsentRecord
{
    public string ConsentId { get; set; } = string.Empty;
    public string PatientDid { get; set; } = string.Empty;
    public string GranteeDid { get; set; } = string.Empty;
    public string GranteeType { get; set; } = string.Empty;       // DOCTOR, ORGANIZATION, etc.
    public string Permission { get; set; } = string.Empty;        // READ, WRITE, DOWNLOAD, FULL_ACCESS
    public string Purpose { get; set; } = string.Empty;           // TREATMENT, RESEARCH, etc.
    public string? EhrId { get; set; }                            // null = all records
    public string GrantedAt { get; set; } = string.Empty;         // ISO 8601
    public string? ExpiresAt { get; set; }                        // ISO 8601
    public string Status { get; set; } = string.Empty;            // ACTIVE, REVOKED, EXPIRED
    public string? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public string? EncryptedAesKey { get; set; } // Wrapped blue key for doctor
}

/// <summary>
/// Audit entry lưu trên blockchain
/// </summary>
public class AuditEntry
{
    public string AuditId { get; set; } = string.Empty;
    public string ActorDid { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;         // PATIENT, DOCTOR, NURSE, ADMIN, SYSTEM
    public string Action { get; set; } = string.Empty;            // VIEW, CREATE, UPDATE, DELETE, etc.
    public string TargetType { get; set; } = string.Empty;        // EHR, CONSENT, FILE, etc.
    public string TargetId { get; set; } = string.Empty;
    public string? PatientDid { get; set; }
    public string? ConsentId { get; set; }
    public string? OrganizationId { get; set; }
    public string Result { get; set; } = string.Empty;            // SUCCESS, DENIED, HASH_MISMATCH, etc.
    public string Timestamp { get; set; } = string.Empty;         // ISO 8601
    public string? IpAddress { get; set; }
    public string? Metadata { get; set; }                         // JSON string
}

/// <summary>
/// Emergency access log stored on the ehr chaincode.
/// </summary>
public class EmergencyAccessRecord
{
    public string LogId { get; set; } = string.Empty;
    public string TargetRecordDid { get; set; } = string.Empty;
    public string AccessorDid { get; set; } = string.Empty;
    public string AccessorOrg { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}

// ============================================================================
// Blockchain Events (published via MassTransit after blockchain tx)
// ============================================================================

public class EhrHashCommittedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid EhrId { get; init; }
    public int Version { get; init; }
    public string ContentHash { get; init; } = string.Empty;
    public string TxHash { get; init; } = string.Empty;
    public long BlockNumber { get; init; }
}

public class ConsentCommittedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string ConsentId { get; init; } = string.Empty;
    public string PatientDid { get; init; } = string.Empty;
    public string GranteeDid { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string TxHash { get; init; } = string.Empty;
    public long BlockNumber { get; init; }
}

public class AuditCommittedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string AuditId { get; init; } = string.Empty;
    public string TxHash { get; init; } = string.Empty;
    public long BlockNumber { get; init; }
}
