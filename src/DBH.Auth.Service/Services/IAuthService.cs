using DBH.Auth.Service.DTOs;

namespace DBH.Auth.Service.Services;


public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RegisterDoctorAsync(RegisterDoctorRequest request);
    Task<AuthResponse> RegisterStaffAsync(RegisterStaffRequest request);
    Task<AuthResponse> RegisterStaffDoctorAsync(RegisterStaffDoctorRequest request);
    Task<AuthResponse> VerifyDoctorAsync(Guid doctorId);
    Task<AuthResponse> VerifyStaffAsync(Guid staffId);
    Task<AuthResponse> UpdateRoleAsync(UpdateRoleRequest request);
    Task<AuthResponse> UpdateUserAsync(Guid userId, AdminUpdateUserRequest request);
    Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, bool isAdminOverride = false);
    Task<AuthResponse> AdminChangePasswordAsync(Guid userId, AdminChangePasswordRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(Guid userId);
    Task<UserProfileResponse?> GetMyProfileAsync(Guid userId);
    Task<UserProfileResponse?> GetProfileByContactAsync(string? email, string? phone);
    Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<Guid?> GetUserIdByProfileIdAsync(Guid? patientId, Guid? doctorId, Guid? staffId = null);

    Task<UserKeysDto?> GetUserKeysAsync(Guid userId);
    Task<PagedResponse<UserProfileResponse>> GetAllUsersAsync(GetAllUsersQuery query, bool isAdminActor);
    Task<List<Guid>> SearchUserIdsAsync(string keyword);
    Task<AuthResponse> UpdateUserStatusAsync(Guid userId, string status);
    Task<AuthResponse> DeactivateAccountAsync(Guid userId);
}
