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

public abstract class RegisterProfileBaseRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? OrganizationId { get; set; }
}

public class RegisterDoctorRequest : RegisterProfileBaseRequest
{
    public string? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
    public string? LicenseImage { get; set; }
    public VerificationStatus VerifiedStatus { get; set; } = VerificationStatus.Pending;
}

public class RegisterStaffRequest : RegisterProfileBaseRequest
{
    public StaffRole Role { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Specialty { get; set; }
    public VerificationStatus VerifiedStatus { get; set; } = VerificationStatus.Pending;
}

public class RegisterStaffDoctorRequest : RegisterProfileBaseRequest
{
    public string Role { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public string? Specialty { get; set; }
    public string? LicenseImage { get; set; }
    public VerificationStatus VerifiedStatus { get; set; } = VerificationStatus.Pending;
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

public class AdminUpdateUserRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? OrganizationId { get; set; }
    public UserStatus? Status { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class AdminChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
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
    public string? SearchTerm { get; set; }
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
