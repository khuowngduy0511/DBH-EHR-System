namespace DBH.Consent.Service.Models.Enums;

/// <summary>
/// Loại người được cấp quyền
/// </summary>
public enum GranteeType
{
    DOCTOR,
    ORGANIZATION,
    EMERGENCY,
    RESEARCHER,
    INSURANCE
}

/// <summary>
/// Quyền truy cập
/// </summary>
public enum ConsentPermission
{
    READ,
    WRITE,
    READ_WRITE
}

/// <summary>
/// Mục đích sử dụng dữ liệu
/// </summary>
public enum ConsentPurpose
{
    TREATMENT,
    RESEARCH,
    INSURANCE,
    LEGAL,
    EMERGENCY,
    PUBLIC_HEALTH
}

/// <summary>
/// Trạng thái consent
/// </summary>
public enum ConsentStatus
{
    ACTIVE,
    REVOKED,
    EXPIRED,
    PENDING
}

/// <summary>
/// Trạng thái access request
/// </summary>
public enum AccessRequestStatus
{
    PENDING,
    APPROVED,
    DENIED,
    EXPIRED,
    CANCELLED
}
