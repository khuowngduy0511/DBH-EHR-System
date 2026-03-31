using System.Text.Json;

namespace DBH.Organization.Service.DTOs;

public class AuthUserProfileDetailDto
{
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? OrganizationId { get; set; }
    public string? Status { get; set; }
    public IEnumerable<string>? Roles { get; set; }
    public Dictionary<string, JsonElement>? Profiles { get; set; }
}
