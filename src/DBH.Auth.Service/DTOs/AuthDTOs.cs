using System.Text.Json.Serialization;
using DBH.Auth.Service.Models.Enums;

namespace DBH.Auth.Service.DTOs;

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; set; } 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RefreshToken { get; set; } 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? UserId { get; set; }
}
    
public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}


public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class RegisterStaffDoctorRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? OrganizationId { get; set; }
}

public class UpdateRoleRequest
{
    public Guid UserId { get; set; }
    public string NewRole { get; set; } = string.Empty;
}

public class UserKeysDto
{
    public Guid UserId { get; set; }
    public string PublicKey { get; set; } = string.Empty;
    public string EncryptedPrivateKey { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
}

public class UserProfileResponse
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
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public Dictionary<string, object?> Profiles { get; set; } = new();
}

public class GetAllUsersQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Gender { get; set; }
    public string? OrganizationId { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }
    public string? Specialty { get; set; }
}

public class PagedResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
