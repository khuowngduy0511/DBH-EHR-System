
using DBH.Auth.Service.Repositories;
using DBH.Auth.Service.DTOs;
using DBH.Auth.Service.Models.Entities;
using DBH.Auth.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;

namespace DBH.Auth.Service.Services;

public class AuthService : IAuthService
{

    private readonly IUserRepository _userRepository;
    private readonly IGenericRepository<UserCredential> _credentialRepository;
    private readonly IGenericRepository<UserSecurity> _securityRepository; 
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository, 
        IGenericRepository<UserCredential> credentialRepository, 
        IGenericRepository<UserSecurity> securityRepository,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _credentialRepository = credentialRepository;
        _securityRepository = securityRepository;
        _tokenService = tokenService;
    }


    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress)
    {
        var user = await _userRepository.GetByEmailWithRolesAsync(request.Email);

        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "Invalid email or password." };
        }

        if (string.IsNullOrEmpty(user.Password) || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            return new AuthResponse { Success = false, Message = "Invalid email or password." };
        }

        if (user.Status != Models.Enums.UserStatus.Active)
        {
            return new AuthResponse { Success = false, Message = "User account delete" };
        }
        
        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.ExistsAsync(u => u.Email == request.Email))
        {
            return new AuthResponse { Success = false, Message = "User with this email already exists." };
        }

        // Create User
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = Models.Enums.UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        
        // Save User
        await _userRepository.AddAsync(user);
        
        // Initialize Security
        var security = new UserSecurity
        {
            UserId = user.UserId,
            MfaEnabled = false
        };
        await _securityRepository.AddAsync(security);

        return new AuthResponse
        {
            Success = true,
            Message = "User registered successfully.",
            UserId = user.UserId,
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var savedRefreshToken = await _credentialRepository.FindAsync(c => 
            c.Provider == Models.Enums.ProviderType.RefreshToken && 
            c.CredentialValue == refreshToken);

        if (savedRefreshToken == null)
        {
            return new AuthResponse { Success = false, Message = "Invalid refresh token." };
        }
        
        var userId = savedRefreshToken.UserId;
        var user = await _userRepository.GetByIdAsync(userId); 
        if (user == null) return new AuthResponse { Success = false, Message = "User not found." };
        
        user = await _userRepository.GetByEmailWithRolesAsync(user.Email!);
        
        return await GenerateAuthResponseAsync(user!);
    }

    public async Task<bool> RevokeTokenAsync(Guid userId)
    {
        var credentials = await _credentialRepository.FindAsync(c => c.UserId == userId && c.Provider == Models.Enums.ProviderType.RefreshToken);
        if (credentials != null)
        {
             await _credentialRepository.DeleteAsync(credentials);
             return true;
        }
        return false;
    }

    public async Task<object?> GetMyProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdWithProfileAsync(userId);
        if (user == null) return null;


        var profiles = new Dictionary<string, object?>();
        foreach (var role in user.UserRoles)
        {
            var roleName = role.Role.RoleName;
            switch (roleName)
            {
                case Models.Enums.RoleName.Doctor:
                    if (user.DoctorProfile != null) profiles.Add(roleName.ToString(), user.DoctorProfile);
                    break;
                case Models.Enums.RoleName.Patient:
                    if (user.PatientProfile != null) profiles.Add(roleName.ToString(), user.PatientProfile);
                    break;
                case Models.Enums.RoleName.Nurse:
                case Models.Enums.RoleName.Pharmacist:
                case Models.Enums.RoleName.Receptionist:
                case Models.Enums.RoleName.LabTech:
                    // Tất cả staff roles sử dụng chung StaffProfile
                    if (user.StaffProfile != null) profiles.Add(roleName.ToString(), user.StaffProfile);
                    break;
            }
        }

        return new 
        {
            user.UserId,
            user.FullName,
            user.Email,
            user.Phone,
            user.Status,
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName.ToString()),
            Profiles = profiles
        };
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.RoleName.ToString()).ToList();
        var accessToken = _tokenService.GenerateToken(user.UserId, user.Email!, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var existingCredential = await _credentialRepository.FindAsync(c => c.UserId == user.UserId && c.Provider == Models.Enums.ProviderType.RefreshToken);
        if (existingCredential != null)
        {
            existingCredential.CredentialValue = refreshToken;
            existingCredential.CreatedAt = DateTime.UtcNow;
            await _credentialRepository.UpdateAsync(existingCredential);
        }
        else
        {
            await _credentialRepository.AddAsync(new UserCredential
            {
                UserId = user.UserId,
                Provider = Models.Enums.ProviderType.RefreshToken,
                CredentialValue = refreshToken,
                CreatedAt = DateTime.UtcNow,
                Verified = true 
            });
        }

        return new AuthResponse
        {
            Success = true,
            Token = accessToken,
            RefreshToken = refreshToken,
            UserId = user.UserId,
            Message = "Authentication successful."
        };
    }
}
