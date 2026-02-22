namespace DBH.Auth.Service.Models.Enums;

/// <summary>
/// Loại người dùng trong hệ thống
/// </summary>
public enum UserType
{
    PATIENT,
    DOCTOR,
    NURSE,
    LAB_TECHNICIAN,
    PHARMACIST,
    ADMIN,
    SYSTEM_ADMIN
}

/// <summary>
/// Trạng thái người dùng
/// </summary>
public enum UserStatus
{
    ACTIVE,
    SUSPENDED,
    PENDING_VERIFICATION,
    DELETED
}

/// <summary>
/// Trạng thái DID (Decentralized Identifier)
/// </summary>
public enum DidStatus
{
    ACTIVE,
    REVOKED,
    EXPIRED,
    PENDING_ACTIVATION
}

/// <summary>
/// Thuật toán key cho DID
/// </summary>
public enum KeyAlgorithm
{
    ECDSA_P256,
    ECDSA_P384,
    RSA_2048,
    RSA_4096,
    Ed25519
}

/// <summary>
/// Phạm vi role
/// </summary>
public enum RoleScope
{
    SYSTEM,
    ORGANIZATION,
    DEPARTMENT
}
