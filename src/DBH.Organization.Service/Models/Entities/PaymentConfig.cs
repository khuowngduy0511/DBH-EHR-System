using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Shared.Contracts;

namespace DBH.Organization.Service.Models.Entities;

[Table("payment_configs")]
public class PaymentConfig
{
    [Key]
    [Column("config_id")]
    public Guid ConfigId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("org_id")]
    public Guid OrgId { get; set; }

    [Required]
    [Column("encrypted_client_id")]
    [MaxLength(500)]
    public string EncryptedClientId { get; set; } = string.Empty;

    [Required]
    [Column("encrypted_api_key")]
    [MaxLength(500)]
    public string EncryptedApiKey { get; set; } = string.Empty;

    [Required]
    [Column("encrypted_checksum_key")]
    [MaxLength(500)]
    public string EncryptedChecksumKey { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = VietnamTimeHelper.Now;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = VietnamTimeHelper.Now;

    // Navigation
    [ForeignKey(nameof(OrgId))]
    public virtual Organization Organization { get; set; } = null!;
}
