using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBH.Auth.Service.Models.Entities;

/// <summary>
/// Refresh Token cho OAuth2
/// </summary>
[Table("refresh_tokens")]
public class RefreshToken
{
    [Key]
    [Column("token_id")]
    public Guid TokenId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("token_hash")]
    [MaxLength(255)]
    public string TokenHash { get; set; } = string.Empty;

    [Column("device_info")]
    [MaxLength(500)]
    public string? DeviceInfo { get; set; }

    [Column("ip_address")]
    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [Column("user_agent", TypeName = "text")]
    public string? UserAgent { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("replaced_by_token")]
    [MaxLength(255)]
    public string? ReplacedByToken { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
