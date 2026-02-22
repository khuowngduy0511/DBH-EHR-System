namespace DBH.Audit.Service.Models.Enums;

/// <summary>
/// Hành động truy cập EHR (dùng cho audit log)
/// </summary>
public enum AuditAction
{
    REQUEST,
    VIEW,
    DOWNLOAD,
    UPDATE,
    CREATE,
    DELETE,
    GRANT_CONSENT,
    REVOKE_CONSENT,
    EMERGENCY_ACCESS,
    LOGIN,
    LOGOUT,
    MFA_VERIFY
}

/// <summary>
/// Kết quả hành động audit
/// </summary>
public enum AuditResult
{
    SUCCESS,
    DENIED,
    HASH_MISMATCH,
    CONSENT_EXPIRED,
    ERROR
}

/// <summary>
/// Loại actor thực hiện hành động
/// </summary>
public enum ActorType
{
    PATIENT,
    DOCTOR,
    NURSE,
    ADMIN,
    SYSTEM,
    ORGANIZATION
}

/// <summary>
/// Loại đối tượng bị tác động
/// </summary>
public enum TargetType
{
    EHR,
    CONSENT,
    FILE,
    USER,
    ORGANIZATION,
    SYSTEM
}
