using System.ComponentModel.DataAnnotations;
using DBH.Auth.Service.Models.Enums;

namespace DBH.Auth.Service.Models.Entities;

public class UserSecurity
{
    [Key]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool MfaEnabled { get; set; }

    public MfaMethod? MfaMethod { get; set; }

    public DateTime? LastPasswordChange { get; set; }

    public DateTime? LastMfaEnrollAt { get; set; }
}
