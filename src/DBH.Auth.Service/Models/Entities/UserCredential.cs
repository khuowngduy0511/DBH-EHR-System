using System.ComponentModel.DataAnnotations;
using DBH.Auth.Service.Models.Enums;

namespace DBH.Auth.Service.Models.Entities;

public class UserCredential
{
    [Key]
    public Guid CredentialId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ProviderType Provider { get; set; }

    [MaxLength(255)]
    public string? CredentialValue { get; set; }
    
    public bool Verified { get; set; }

    public DateTime? VerifiedAt { get; set; }

    [MaxLength(2048)]
    public string? PublicKey { get; set; }

    [MaxLength(2048)]
    public string? EncryptedPrivateKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
