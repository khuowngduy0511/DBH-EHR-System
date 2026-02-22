using System.ComponentModel.DataAnnotations;
using DBH.Consent.Service.Models.Enums;

namespace DBH.Consent.Service.DTOs;

// ============================================================================
// CONSENT DTOs
// ============================================================================

/// <summary>
/// Request to grant consent for EHR access
/// </summary>
public class GrantConsentRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    [MaxLength(255)]
    public string PatientDid { get; set; } = string.Empty;

    [Required]
    public Guid GranteeId { get; set; }

    [Required]
    [MaxLength(255)]
    public string GranteeDid { get; set; } = string.Empty;

    public GranteeType GranteeType { get; set; }

    /// <summary>
    /// Specific EHR ID (null = all patient records)
    /// </summary>
    public Guid? EhrId { get; set; }

    public ConsentPermission Permission { get; set; } = ConsentPermission.READ;

    public ConsentPurpose Purpose { get; set; } = ConsentPurpose.TREATMENT;

    /// <summary>
    /// Additional conditions as JSON (report types, etc.)
    /// </summary>
    public string? Conditions { get; set; }

    /// <summary>
    /// Duration in days (null = no expiration)
    /// </summary>
    public int? DurationDays { get; set; }
}

/// <summary>
/// Request to revoke consent
/// </summary>
public class RevokeConsentRequest
{
    [MaxLength(500)]
    public string? RevokeReason { get; set; }
}

/// <summary>
/// Consent response
/// </summary>
public class ConsentResponse
{
    public Guid ConsentId { get; set; }
    public string BlockchainConsentId { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public string PatientDid { get; set; } = string.Empty;
    public Guid GranteeId { get; set; }
    public string GranteeDid { get; set; } = string.Empty;
    public GranteeType GranteeType { get; set; }
    public Guid? EhrId { get; set; }
    public ConsentPermission Permission { get; set; }
    public ConsentPurpose Purpose { get; set; }
    public string? Conditions { get; set; }
    public DateTime GrantedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public ConsentStatus Status { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public string? GrantTxHash { get; set; }
    public string? RevokeTxHash { get; set; }
    public long? BlockchainBlockNum { get; set; }
}

// ============================================================================
// ACCESS REQUEST DTOs
// ============================================================================

/// <summary>
/// Request for access to patient EHR
/// </summary>
public class CreateAccessRequestDto
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    [MaxLength(255)]
    public string PatientDid { get; set; } = string.Empty;

    [Required]
    public Guid RequesterId { get; set; }

    [Required]
    [MaxLength(255)]
    public string RequesterDid { get; set; } = string.Empty;

    public GranteeType RequesterType { get; set; } = GranteeType.DOCTOR;

    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// Specific EHR ID (null = all patient records)
    /// </summary>
    public Guid? EhrId { get; set; }

    public ConsentPermission Permission { get; set; } = ConsentPermission.READ;

    public ConsentPurpose Purpose { get; set; } = ConsentPurpose.TREATMENT;

    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    public int RequestedDurationDays { get; set; } = 30;
}

/// <summary>
/// Response to access request (approve/deny)
/// </summary>
public class RespondAccessRequestDto
{
    public bool Approve { get; set; }

    [MaxLength(500)]
    public string? ResponseReason { get; set; }
}

/// <summary>
/// Access request response
/// </summary>
public class AccessRequestResponse
{
    public Guid RequestId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientDid { get; set; } = string.Empty;
    public Guid RequesterId { get; set; }
    public string RequesterDid { get; set; } = string.Empty;
    public GranteeType RequesterType { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? EhrId { get; set; }
    public ConsentPermission Permission { get; set; }
    public ConsentPurpose Purpose { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int RequestedDurationDays { get; set; }
    public AccessRequestStatus Status { get; set; }
    public Guid? ConsentId { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ResponseReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

// ============================================================================
// COMMON DTOs
// ============================================================================

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public class PagedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Query parameters for consent search
/// </summary>
public class ConsentQueryParams
{
    public Guid? PatientId { get; set; }
    public Guid? GranteeId { get; set; }
    public ConsentStatus? Status { get; set; }
    public ConsentPurpose? Purpose { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>
/// Verify consent access request
/// </summary>
public class VerifyConsentRequest
{
    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public Guid GranteeId { get; set; }

    public Guid? EhrId { get; set; }

    public ConsentPermission? RequiredPermission { get; set; }
}

/// <summary>
/// Result of consent verification
/// </summary>
public class VerifyConsentResponse
{
    public bool HasAccess { get; set; }
    public Guid? ConsentId { get; set; }
    public ConsentPermission? Permission { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Message { get; set; }
}
