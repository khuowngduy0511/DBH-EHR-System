using System.ComponentModel.DataAnnotations;

namespace DBH.Organization.Service.DTOs;

// =============================================================================
// Payment Config DTOs
// =============================================================================

public class ConfigurePaymentRequest
{
    [Required]
    [MaxLength(255)]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string ChecksumKey { get; set; } = string.Empty;
}

public class PaymentConfigStatusResponse
{
    public Guid OrgId { get; set; }
    public bool HasPaymentConfig { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PaymentKeysResponse
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
}
