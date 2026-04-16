using DBH.Auth.Service.DTOs;

namespace DBH.Auth.Service.Services;


public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RegisterStaffDoctorAsync(RegisterStaffDoctorRequest request);
    Task<AuthResponse> UpdateRoleAsync(UpdateRoleRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(Guid userId);
    Task<UserProfileResponse?> GetMyProfileAsync(Guid userId);
    Task<UserProfileResponse?> GetProfileByContactAsync(string? email, string? phone);
    Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<Guid?> GetUserIdByProfileIdAsync(Guid? patientId, Guid? doctorId);
    Task<UserKeysDto?> GetUserKeysAsync(Guid userId);
    Task<PagedResponse<UserProfileResponse>> GetAllUsersAsync(GetAllUsersQuery query, bool isAdminActor);
    Task<AuthResponse> UpdateUserStatusAsync(Guid userId, string status);
    Task<AuthResponse> DeactivateAccountAsync(Guid userId);
}
