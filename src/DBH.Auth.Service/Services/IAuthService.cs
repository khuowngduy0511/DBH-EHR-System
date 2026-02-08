using DBH.Auth.Service.DTOs;

namespace DBH.Auth.Service.Services;


public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<bool> RevokeTokenAsync(Guid userId);
    Task<object?> GetMyProfileAsync(Guid userId);
}
