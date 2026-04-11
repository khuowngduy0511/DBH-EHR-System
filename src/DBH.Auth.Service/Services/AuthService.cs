using DBH.Auth.Service.Repositories;
using DBH.Auth.Service.DTOs;
using DBH.Auth.Service.Models.Entities;
using DBH.Auth.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;
using DBH.Shared.Infrastructure.cryptography;
using DBH.Shared.Infrastructure.Blockchain.Sync;
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
    private readonly IBlockchainSyncService _blockchainSyncService;
    private readonly IOrganizationServiceClient _organizationServiceClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

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
        IBlockchainSyncService blockchainSyncService,
        IOrganizationServiceClient organizationServiceClient,
        IHttpContextAccessor httpContextAccessor)
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
        _blockchainSyncService = blockchainSyncService;
        _organizationServiceClient = organizationServiceClient;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress)
    {
        _logger.LogInformation("Login attempt for user: {Email}", request.Email);
        var user = await _userRepository.GetByEmailWithRolesAsync(request.Email);

        if (user == null)
        {
            _logger.LogWarning("User not found: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "Email hoặc mật khẩu không chính xác." };
        }

        if (string.IsNullOrEmpty(user.Password) || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            _logger.LogWarning("Invalid password for user: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "Email hoặc mật khẩu không chính xác." };
        }

        if (user.Status != Models.Enums.UserStatus.Active)
        {
            _logger.LogWarning("User account is not active: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "Tài khoản đã bị vô hiệu hóa." };
        }
        
        return await GenerateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var actorUserId = GetCurrentActorId();

        if (await _userRepository.ExistsAsync(u => u.Email == request.Email))
        {
            _logger.LogWarning("User with this email already exists: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "Email này đã được sử dụng." };
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var normalizedPhone = request.Phone.Trim();
            if (await _userRepository.ExistsAsync(u => u.Phone == normalizedPhone))
            {
                _logger.LogWarning("User with this phone already exists: {Phone}", normalizedPhone);
                return new AuthResponse { Success = false, Message = "Số điện thoại này đã được sử dụng." };
            }

            request.Phone = normalizedPhone;
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
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUserId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = actorUserId
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

        // Enqueue Fabric CA enrollment for async processing via RabbitMQ.
        var roleName = patientRole?.RoleName.ToString() ?? "Patient";
        _blockchainSyncService.EnqueueFabricCaEnrollment(
            enrollmentId: user.UserId.ToString(),
            username: user.FullName ?? user.Email ?? user.UserId.ToString(),
            role: roleName,
            onFailure: error =>
            {
                _logger.LogWarning(
                    "Blockchain enrollment failed for user {UserId}: {Error}",
                    user.UserId, error);
                return Task.CompletedTask;
            });

        return new AuthResponse
        {
            Success = true,
            Message = "Đăng ký tài khoản thành công.",
            UserId = user.UserId,
        };
    }

    public async Task<AuthResponse> RegisterStaffDoctorAsync(RegisterStaffDoctorRequest request)
    {
        var actorUserId = GetCurrentActorId();

        if (await _userRepository.ExistsAsync(u => u.Email == request.Email))
        {
            return new AuthResponse { Success = false, Message = "Email này đã được sử dụng." };
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var normalizedPhone = request.Phone.Trim();
            if (await _userRepository.ExistsAsync(u => u.Phone == normalizedPhone))
            {
                return new AuthResponse { Success = false, Message = "Số điện thoại này đã được sử dụng." };
            }

            request.Phone = normalizedPhone;
        }

        if (!Enum.TryParse<RoleName>(request.Role, true, out var roleName))
        {
            return new AuthResponse { Success = false, Message = "Quyền (Role) không hợp lệ." };
        }

        var keyPair = AsymmetricEncryptionService.GenerateKeyPair();

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth.HasValue
                ? DateTime.SpecifyKind(request.DateOfBirth.Value, DateTimeKind.Utc)
                : null,
            Address = request.Address,
            OrganizationId = request.OrganizationId,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = Models.Enums.UserStatus.Active,
            PublicKey = keyPair.PublicKey,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUserId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = actorUserId
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

        var organizationWarning = await HandleOrganizationMembershipAsync(user, request.OrganizationId, null);

        // Enqueue Fabric CA enrollment for async processing via RabbitMQ
        var enrollRoleName = assignedRole?.RoleName.ToString() ?? request.Role;
        _blockchainSyncService.EnqueueFabricCaEnrollment(
            enrollmentId: user.UserId.ToString(),
            username: user.FullName ?? user.Email ?? user.UserId.ToString(),
            role: enrollRoleName,
            onFailure: error =>
            {
                _logger.LogWarning(
                    "Blockchain enrollment failed for staff/doctor user {UserId}: {Error}",
                    user.UserId, error);
                return Task.CompletedTask;
            });

        return new AuthResponse
        {
            Success = true,
            Message = string.IsNullOrEmpty(organizationWarning)
                ? "Đăng ký tài khoản nhân sự thành công."
                : $"Admin/User registered successfully with the specified role. {organizationWarning}",
            UserId = user.UserId,
        };
    }

    public async Task<AuthResponse> UpdateRoleAsync(UpdateRoleRequest request)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
            return new AuthResponse { Success = false, Message = "Không tìm thấy tài khoản người dùng." };

        if (!Enum.TryParse<RoleName>(request.NewRole, true, out var newRoleEnum))
        {
            return new AuthResponse { Success = false, Message = "Quyền (Role) không hợp lệ." };
        }

        var newRoleEntity = await _roleRepository.FindAsync(r => r.RoleName == newRoleEnum);
        if (newRoleEntity == null)
        {
            return new AuthResponse { Success = false, Message = "Quyền (Role) không tồn tại trong hệ thống." };
        }

        var existingUserRole = await _userRoleRepository.FindAsync(ur => ur.UserId == user.UserId);
        if (existingUserRole != null)
        {
            if (existingUserRole.RoleId == newRoleEntity.RoleId)
            {
                return new AuthResponse { Success = true, Message = "Người dùng đã sở hữu quyền này." };
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

        return new AuthResponse { Success = true, Message = "Cập nhật quyền thành công." };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var savedRefreshToken = await _credentialRepository.FindAsync(c => 
            c.Provider == Models.Enums.ProviderType.RefreshToken && 
            c.CredentialValue == refreshToken);

        if (savedRefreshToken == null)
        {
            return new AuthResponse { Success = false, Message = "Mã xác thực lại (Refresh Token) không hợp lệ." };
        }
        
        var userId = savedRefreshToken.UserId;
        var user = await _userRepository.GetByIdAsync(userId); 
        if (user == null) return new AuthResponse { Success = false, Message = "Không tìm thấy tài khoản người dùng." };
        
        user = await _userRepository.GetByEmailWithRolesAsync(user.Email!);
        
        return await GenerateAuthResponseAsync(user!);
    }

    public async Task<bool> RevokeTokenAsync(Guid userId)
    {
        var credentials = await _credentialRepository.FindManyAsync(c => c.UserId == userId && c.Provider == Models.Enums.ProviderType.RefreshToken);
        var tokenList = credentials.ToList();
        if (tokenList.Count == 0)
            return false;

        foreach (var token in tokenList)
        {
            await _credentialRepository.DeleteAsync(token);
        }
        return true;
    }

    public async Task<UserProfileResponse?> GetMyProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdWithProfileAsync(userId);
        if (user == null) return null;

        return BuildUserProfileResponse(user);
    }

    public async Task<UserProfileResponse?> GetProfileByContactAsync(string? email, string? phone)
    {
        var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        if (normalizedEmail == null && normalizedPhone == null)
        {
            return null;
        }

        if (normalizedEmail != null)
        {
            var userByEmail = await _userRepository.GetByEmailWithProfileAsync(normalizedEmail);
            if (userByEmail == null)
            {
                return null;
            }

            if (normalizedPhone != null && !string.Equals(userByEmail.Phone, normalizedPhone, StringComparison.Ordinal))
            {
                return null;
            }

            return BuildUserProfileResponse(userByEmail);
        }

        var userByPhone = await _userRepository.GetByPhoneWithProfileAsync(normalizedPhone!);
        if (userByPhone == null)
        {
            return null;
        }

        return BuildUserProfileResponse(userByPhone);
    }

    private static UserProfileResponse BuildUserProfileResponse(User user)
    {
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

        return new UserProfileResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Gender = user.Gender,
            DateOfBirth = user.DateOfBirth,
            Address = user.Address,
            OrganizationId = user.OrganizationId,
            Status = user.Status.ToString(),
            Roles = user.UserRoles.Select(ur => ur.Role.RoleName.ToString()),
            Profiles = profiles
        };
    }

    public async Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdWithProfileAsync(userId);
        if (user == null)
            return new AuthResponse { Success = false, Message = "Không tìm thấy tài khoản người dùng." };

        bool isUpdated = false;

        if (!string.IsNullOrWhiteSpace(request.FullName) && user.FullName != request.FullName)
        {
            user.FullName = request.FullName;
            isUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Phone) && user.Phone != request.Phone)
        {
            var normalizedPhone = request.Phone.Trim();
            var phoneExists = await _userRepository.ExistsAsync(u => u.Phone == normalizedPhone && u.UserId != userId);
            if (phoneExists)
            {
                return new AuthResponse { Success = false, Message = "Số điện thoại này đã được sử dụng." };
            }

            user.Phone = normalizedPhone;
            isUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Gender) && user.Gender != request.Gender)
        {
            user.Gender = request.Gender;
            isUpdated = true;
        }

        if (request.DateOfBirth.HasValue && user.DateOfBirth != request.DateOfBirth.Value)
        {
            user.DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth.Value, DateTimeKind.Utc);
            isUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Address) && user.Address != request.Address)
        {
            user.Address = request.Address;
            isUpdated = true;
        }

        if (isUpdated)
        {
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = userId;
            await _userRepository.UpdateAsync(user);

            // Cập nhật thêm vào Profile Patient nếu User có role Patient
            if (user.PatientProfile != null && request.DateOfBirth.HasValue)
            {
                var patientDob = DateOnly.FromDateTime(request.DateOfBirth.Value);
                if (user.PatientProfile.Dob != patientDob)
                {
                    user.PatientProfile.Dob = patientDob;
                    await _patientRepository.UpdateAsync(user.PatientProfile);
                }
            }
        }

        return new AuthResponse { Success = true, Message = "Cập nhật hồ sơ cá nhân thành công." };
    }

    public async Task<Guid?> GetUserIdByProfileIdAsync(Guid? patientId, Guid? doctorId)
    {
        if (patientId.HasValue)
        {
            var patient = await _patientRepository.FindAsync(p => p.PatientId == patientId.Value);
            return patient?.UserId;
        }

        if (doctorId.HasValue)
        {
            var doctor = await _doctorRepository.FindAsync(d => d.DoctorId == doctorId.Value);
            return doctor?.UserId;
        }

        return null;
    }

    public async Task<UserKeysDto?> GetUserKeysAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || string.IsNullOrWhiteSpace(user.PublicKey))
        {
            return null;
        }

        var privateCredential = await _credentialRepository.FindAsync(c => c.UserId == userId && c.Provider == ProviderType.EncryptedPrivateKey);
        if (privateCredential == null || string.IsNullOrEmpty(privateCredential.CredentialValue))
        {
            return null;
        }

        return new UserKeysDto
        {
            UserId = userId,
            PublicKey = user.PublicKey,
            EncryptedPrivateKey = privateCredential.CredentialValue
        };
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user)
    {
        var roles = user.UserRoles.Select(ur => ur.Role.RoleName.ToString()).ToList();
        var accessToken = _tokenService.GenerateToken(user.UserId, user.Email!, user.FullName ?? "Unknown", user.OrganizationId ?? string.Empty, roles);
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
            Message = "Đăng nhập thành công."
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

    private async Task<string?> HandleOrganizationMembershipAsync(User user, string? requestedOrganizationId, string? jobTitle)
    {
        if (string.IsNullOrWhiteSpace(requestedOrganizationId))
        {
            return null;
        }

        if (!Guid.TryParse(requestedOrganizationId, out var organizationId))
        {
            _logger.LogWarning(
                "Invalid organization ID format '{OrgId}' provided for user {UserId}. Membership was not created.",
                requestedOrganizationId, user.UserId);

            return $"Organization ID '{requestedOrganizationId}' is not a valid GUID format. Membership was not created.";
        }

        var membershipResult = await _organizationServiceClient.CreateMembershipAsync(
            userId: user.UserId,
            organizationId: organizationId,
            departmentId: null,
            jobTitle: jobTitle);

        if (!membershipResult.Success)
        {
            _logger.LogWarning(
                "Membership was not created for user {UserId} in organization {OrgId}: {Error}.",
                user.UserId, organizationId, membershipResult.Message);

            return $"Failed to create membership in organization {organizationId}: {membershipResult.Message}";
        }

        _logger.LogInformation(
            "Successfully created membership for user {UserId} in organization {OrgId}",
            user.UserId, organizationId);

        return null;
    }

    private Guid? GetCurrentActorId()
    {
        var claimValue = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}

