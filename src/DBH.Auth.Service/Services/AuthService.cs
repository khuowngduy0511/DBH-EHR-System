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
    private readonly IGenericRepository<Doctor> _doctorRepository;
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IGenericRepository<Staff> _staffRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly IFabricCaService _fabricCa;

    public AuthService(
        IUserRepository userRepository, 
        IGenericRepository<UserCredential> credentialRepository, 
        IGenericRepository<UserSecurity> securityRepository,
        IGenericRepository<Role> roleRepository,
        IGenericRepository<UserRole> userRoleRepository,
        IGenericRepository<Doctor> doctorRepository,
        IGenericRepository<Patient> patientRepository,
        IGenericRepository<Staff> staffRepository,
        ILogger<AuthService> logger,
        ITokenService tokenService,
        IFabricCaService fabricCa)
    {
        _userRepository = userRepository;
        _credentialRepository = credentialRepository;
        _securityRepository = securityRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _doctorRepository = doctorRepository;
        _patientRepository = patientRepository;
        _staffRepository = staffRepository;
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
            PublicKey = keyPair.PublicKey,
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

            await EnsureRoleProfileAsync(user.UserId, patientRole.RoleName);
        }
        
        // Initialize Security
        var security = new UserSecurity
        {
            UserId = user.UserId,
            MfaEnabled = false
        };
        await _securityRepository.AddAsync(security);

        // Save Private Key in User Credentials
        var encryptedPrivateKey = MasterKeyEncryptionService.Encrypt(keyPair.PrivateKey);
        await _credentialRepository.AddAsync(new UserCredential
        {
            UserId = user.UserId,
            Provider = ProviderType.EncryptedPrivateKey,
            CredentialValue = encryptedPrivateKey,
            CreatedAt = DateTime.UtcNow,
        });

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

    public async Task<AuthResponse> RegisterStaffDoctorAsync(RegisterStaffDoctorRequest request)
    {
        if (await _userRepository.ExistsAsync(u => u.Email == request.Email))
        {
            return new AuthResponse { Success = false, Message = "User with this email already exists." };
        }

        if (!Enum.TryParse<RoleName>(request.Role, true, out var roleName))
        {
            return new AuthResponse { Success = false, Message = "Invalid role specified." };
        }

        var keyPair = AsymmetricEncryptionService.GenerateKeyPair();

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = Models.Enums.UserStatus.Active,
            PublicKey = keyPair.PublicKey,
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

            await EnsureRoleProfileAsync(user.UserId, assignedRole.RoleName);
        }

        var security = new UserSecurity
        {
            UserId = user.UserId,
            MfaEnabled = false
        };
        await _securityRepository.AddAsync(security);

        var encryptedPrivateKey = MasterKeyEncryptionService.Encrypt(keyPair.PrivateKey);
        await _credentialRepository.AddAsync(new UserCredential
        {
            UserId = user.UserId,
            Provider = ProviderType.EncryptedPrivateKey,            
            CredentialValue= encryptedPrivateKey,
            CreatedAt = DateTime.UtcNow,            
        });

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
            if (existingUserRole.RoleId == newRoleEntity.RoleId)
            {
                return new AuthResponse { Success = true, Message = "User already has the specified role." };
            }
            await _userRoleRepository.DeleteAsync(existingUserRole);
            await _userRoleRepository.AddAsync(new UserRole
            {
                UserId = user.UserId,
                RoleId = newRoleEntity.RoleId
            });
        }
        else
        {
            await _userRoleRepository.AddAsync(new UserRole
            {
                UserId = user.UserId,
                RoleId = newRoleEntity.RoleId
            });
        }

        await EnsureRoleProfileAsync(user.UserId, newRoleEntity.RoleName);

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
                    if (user.DoctorProfile != null)
                    {
                        profiles[roleName.ToString()] = new
                        {
                            user.DoctorProfile.DoctorId,
                            user.DoctorProfile.UserId,
                            user.DoctorProfile.Specialty,
                            user.DoctorProfile.LicenseNumber,
                            user.DoctorProfile.LicenseImage,
                            VerifiedStatus = user.DoctorProfile.VerifiedStatus.ToString()
                        };
                    }
                    break;
                case Models.Enums.RoleName.Patient:
                    if (user.PatientProfile != null)
                    {
                        profiles[roleName.ToString()] = new
                        {
                            user.PatientProfile.PatientId,
                            user.PatientProfile.UserId,
                            user.PatientProfile.Dob,
                            user.PatientProfile.BloodType
                        };
                    }
                    break;
                case Models.Enums.RoleName.Nurse:
                case Models.Enums.RoleName.Pharmacist:
                case Models.Enums.RoleName.Receptionist:
                case Models.Enums.RoleName.LabTech:
                    // Tất cả staff roles sử dụng chung StaffProfile
                    if (user.StaffProfile != null)
                    {
                        profiles[roleName.ToString()] = new
                        {
                            user.StaffProfile.StaffId,
                            user.StaffProfile.UserId,
                            Role = user.StaffProfile.Role.ToString(),
                            user.StaffProfile.LicenseNumber,
                            user.StaffProfile.Specialty,
                            VerifiedStatus = user.StaffProfile.VerifiedStatus.ToString()
                        };
                    }
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
        var privateCredential = await _credentialRepository.FindAsync(c => c.UserId == userId && c.Provider == ProviderType.EncryptedPrivateKey);
        if (privateCredential == null || string.IsNullOrEmpty(privateCredential.CredentialValue))
        {
            return null;
        }

        return new UserKeysDto
        {
            UserId = userId,
            EncryptedPrivateKey = privateCredential.CredentialValue
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

    private async Task EnsureRoleProfileAsync(Guid userId, RoleName roleName)
    {
        switch (roleName)
        {
            case RoleName.Patient:
            {
                if (!await _patientRepository.ExistsAsync(p => p.UserId == userId))
                {
                    await _patientRepository.AddAsync(new Patient
                    {
                        UserId = userId
                    });
                }

                break;
            }
            case RoleName.Doctor:
            {
                if (!await _doctorRepository.ExistsAsync(d => d.UserId == userId))
                {
                    await _doctorRepository.AddAsync(new Doctor
                    {
                        UserId = userId,
                        VerifiedStatus = VerificationStatus.Pending
                    });
                }

                break;
            }
            case RoleName.Nurse:
            case RoleName.Pharmacist:
            case RoleName.Receptionist:
            case RoleName.LabTech:
            {
                var expectedStaffRole = MapToStaffRole(roleName);
                var staffProfile = await _staffRepository.FindAsync(s => s.UserId == userId);

                if (staffProfile == null)
                {
                    await _staffRepository.AddAsync(new Staff
                    {
                        UserId = userId,
                        Role = expectedStaffRole,
                        VerifiedStatus = VerificationStatus.Pending
                    });
                }
                else if (staffProfile.Role != expectedStaffRole)
                {
                    staffProfile.Role = expectedStaffRole;
                    await _staffRepository.UpdateAsync(staffProfile);
                }

                break;
            }
        }
    }

    private static StaffRole MapToStaffRole(RoleName roleName)
    {
        return roleName switch
        {
            RoleName.Nurse => StaffRole.Nurse,
            RoleName.Pharmacist => StaffRole.Pharmacist,
            RoleName.Receptionist => StaffRole.Receptionist,
            RoleName.LabTech => StaffRole.LabTech,
            _ => throw new InvalidOperationException($"Role '{roleName}' is not a staff role.")
        };
    }
}
