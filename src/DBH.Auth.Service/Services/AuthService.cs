using DBH.Auth.Service.Repositories;
using DBH.Auth.Service.DTOs;
using DBH.Auth.Service.Models.Entities;
using DBH.Auth.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;
using DBH.Shared.Infrastructure.cryptography;
using DBH.Shared.Contracts.Blockchain;

namespace DBH.Auth.Service.Services;

public class AuthService : IAuthService
{

    private readonly IUserRepository _userRepository;
    private readonly IGenericRepository<UserCredential> _credentialRepository;
    private readonly IGenericRepository<UserSecurity> _securityRepository; 
    private readonly IGenericRepository<Role> _roleRepository;
    private readonly IGenericRepository<UserRole> _userRoleRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly IFabricCaService _fabricCa;

    public AuthService(
        IUserRepository userRepository, 
        IGenericRepository<UserCredential> credentialRepository, 
        IGenericRepository<UserSecurity> securityRepository,
        IGenericRepository<Role> roleRepository,
        IGenericRepository<UserRole> userRoleRepository,
        ILogger<AuthService> logger,
        ITokenService tokenService,
        IFabricCaService fabricCa)
    {
        _userRepository = userRepository;
        _credentialRepository = credentialRepository;
        _securityRepository = securityRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _tokenService = tokenService;
        _logger = logger;
        _fabricCa = fabricCa;
    }


    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress)
    {
        _logger.LogInformation("Login attempt for user: {Email}", request.Email);
        var user = await _userRepository.GetByEmailWithRolesAsync(request.Email);

        if (user == null)
        {
            _logger.LogWarning("User not found: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "Invalid email or password." };
        }

        if (string.IsNullOrEmpty(user.Password) || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            _logger.LogWarning("Invalid password for user: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "Invalid email or password." };
        }

        if (user.Status != Models.Enums.UserStatus.Active)
        {
            _logger.LogWarning("User account is not active: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "User account delete" };
        }
        
        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.ExistsAsync(u => u.Email == request.Email))
        {
            _logger.LogWarning("User with this email already exists: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "User with this email already exists." };
        }

        // Generate RSA/ECC Key Pair
        var keyPair = AsymmetricEncryptionService.GenerateKeyPair();
        // _logger.LogInformation("Generated key pair for PublicKey: {PublicKey}", keyPair.PublicKey);
        // _logger.LogInformation("Generated key pair for PrivateKey: {Privatekey}", keyPair.PrivateKey);
        // Create User
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = Models.Enums.UserStatus.Active,
            // PublicKey = keyPair.PublicKey,
            CreatedAt = DateTime.UtcNow
        };
        
        // Save User
        await _userRepository.AddAsync(user);

        // Assign default Patient Role
        var patientRole = await _roleRepository.FindAsync(r => r.RoleName == RoleName.Patient);
        if (patientRole != null)
        {
            await _userRoleRepository.AddAsync(new UserRole
            {
                UserId = user.UserId,
                RoleId = patientRole.RoleId
            });
        }
        
        // Initialize Security
        var security = new UserSecurity
        {
            UserId = user.UserId,
            MfaEnabled = false
        };
        await _securityRepository.AddAsync(security);

        // Save Private Key in User Credentials
        // var encryptedPrivateKey = MasterKeyEncryptionService.Encrypt(keyPair.PrivateKey);
        // await _credentialRepository.AddAsync(new UserCredential
        // {
        //     UserId = user.UserId,
        //     Provider = ProviderType.SystemKey,
        //     PublicKey = keyPair.PublicKey,
        //     EncryptedPrivateKey = encryptedPrivateKey,
        //     CreatedAt = DateTime.UtcNow,
        //     Verified = true 
        // });

        // Enroll a Fabric CA blockchain identity for this user.
        // Non-blocking: a failure here does not fail registration.
        _ = Task.Run(async () =>
        {
            var roleName = patientRole?.RoleName.ToString() ?? "Patient";
            var enrollResult = await _fabricCa.EnrollUserAsync(
                enrollmentId: user.UserId.ToString(),
                username: user.FullName ?? user.Email ?? user.UserId.ToString(),
                role: roleName);

            if (!enrollResult.Success)
            {
                _logger.LogWarning(
                    "Blockchain enrollment skipped for user {UserId}: {Error}",
                    user.UserId, enrollResult.ErrorMessage);
            }
        });

        return new AuthResponse
        {
            Success = true,
            Message = "User registered successfully.",
            UserId = user.UserId,
        };
    }

    public async Task<AuthResponse> RegisterAdminAsync(RegisterAdminRequest request)
    {
        if (await _userRepository.ExistsAsync(u => u.Email == request.Email))
        {
            return new AuthResponse { Success = false, Message = "User with this email already exists." };
        }

        if (!Enum.TryParse<RoleName>(request.Role, true, out var roleName))
        {
            return new AuthResponse { Success = false, Message = "Invalid role specified." };
        }

        // var keyPair = AsymmetricEncryptionService.GenerateKeyPair();

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = Models.Enums.UserStatus.Active,
            // PublicKey = keyPair.PublicKey,
            CreatedAt = DateTime.UtcNow
        };
        
        await _userRepository.AddAsync(user);

        var assignedRole = await _roleRepository.FindAsync(r => r.RoleName == roleName);
        if (assignedRole != null)
        {
            await _userRoleRepository.AddAsync(new UserRole
            {
                UserId = user.UserId,
                RoleId = assignedRole.RoleId
            });
        }

        var security = new UserSecurity
        {
            UserId = user.UserId,
            MfaEnabled = false
        };
        await _securityRepository.AddAsync(security);

        // var encryptedPrivateKey = MasterKeyEncryptionService.Encrypt(keyPair.PrivateKey);
        // await _credentialRepository.AddAsync(new UserCredential
        // {
        //     UserId = user.UserId,
        //     Provider = ProviderType.SystemKey,
        //     PublicKey = keyPair.PublicKey,
        //     EncryptedPrivateKey = encryptedPrivateKey,
        //     CreatedAt = DateTime.UtcNow,
        //     Verified = true 
        // });

        return new AuthResponse
        {
            Success = true,
            Message = "Admin/User registered successfully with the specified role.",
            UserId = user.UserId,
        };
    }

    public async Task<AuthResponse> UpdateRoleAsync(UpdateRoleRequest request)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            return new AuthResponse { Success = false, Message = "User not found." };

        if (!Enum.TryParse<RoleName>(request.NewRole, true, out var newRoleEnum))
        {
            return new AuthResponse { Success = false, Message = "Invalid role specified." };
        }

        var newRoleEntity = await _roleRepository.FindAsync(r => r.RoleName == newRoleEnum);
        if (newRoleEntity == null)
        {
            return new AuthResponse { Success = false, Message = "Role does not exist in the system." };
        }

        var existingUserRole = await _userRoleRepository.FindAsync(ur => ur.UserId == user.UserId);
        if (existingUserRole != null)
        {
            existingUserRole.RoleId = newRoleEntity.RoleId;
            await _userRoleRepository.UpdateAsync(existingUserRole);
        }
        else
        {
            await _userRoleRepository.AddAsync(new UserRole
            {
                UserId = user.UserId,
                RoleId = newRoleEntity.RoleId
            });
        }

        return new AuthResponse { Success = true, Message = "User role updated successfully." };
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

    public async Task<UserKeysDto?> GetUserKeysAsync(Guid userId)
    {
        var credentials = await _credentialRepository.FindAsync(c => c.UserId == userId && c.Provider == ProviderType.SystemKey);
        if (credentials == null || string.IsNullOrEmpty(credentials.PublicKey) || string.IsNullOrEmpty(credentials.EncryptedPrivateKey))
        {
            return null;
        }

        return new UserKeysDto
        {
            UserId = userId,
            PublicKey = credentials.PublicKey,
            EncryptedPrivateKey = credentials.EncryptedPrivateKey
        };
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.RoleName.ToString()).ToList();
        var accessToken = _tokenService.GenerateToken(user.UserId, user.Email!, user.FullName, roles);
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
