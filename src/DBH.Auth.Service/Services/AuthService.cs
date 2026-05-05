using DBH.Auth.Service.Repositories;
using DBH.Auth.Service.DbContext;
using DBH.Auth.Service.DTOs;
using DBH.Auth.Service.Models.Entities;
using DBH.Auth.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;
using DBH.Shared.Infrastructure.cryptography;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using DBH.Shared.Infrastructure.Caching;
using DBH.Shared.Infrastructure.Time;
using DBH.Shared.Contracts;
using DBH.Shared.Contracts.Blockchain;
using System.Text.RegularExpressions;

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
    private readonly AuthDbContext _dbContext;
    private readonly ICacheService _cache;

    private static readonly TimeSpan ProfileCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan KeysCacheTtl = TimeSpan.FromMinutes(60);

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
        IHttpContextAccessor httpContextAccessor,
        AuthDbContext dbContext,
        ICacheService cache)
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
        _dbContext = dbContext;
        _cache = cache;
    }


    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null)
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

        // Validate email format
        if (!IsValidEmail(request.Email))
        {
            _logger.LogWarning("Invalid email format provided: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "Email không hợp lệ." };
        }

        // Check if user with this email already exists
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            if (existingUser.Status == Models.Enums.UserStatus.Inactive)
            {
                _logger.LogInformation("Reactivating deactivated account for: {Email}", request.Email);
                return await ReactivateAccountAsync(existingUser, request, actorUserId);
            }

            _logger.LogWarning("User with this email already exists: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "Email này đã được sử dụng." };
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var normalizedPhone = request.Phone.Trim();
            if (await _userRepository.ExistsAsync(u => u.Phone == normalizedPhone && u.Status != Models.Enums.UserStatus.Inactive))
            {
                _logger.LogWarning("User with this phone already exists: {Phone}", normalizedPhone);
                return new AuthResponse { Success = false, Message = "Số điện thoại này đã được sử dụng." };
            }

            request.Phone = normalizedPhone;
        }

        if (request.Password.Length < 8)
        {
            return new AuthResponse { Success = false, Message = "Mật khẩu phải có ít nhất 8 ký tự." };
        }

        // Generate RSA/ECC Key Pair
        var keyPair = AsymmetricEncryptionService.GenerateKeyPair();
        // Create User
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = Models.Enums.UserStatus.Active,
            PublicKey = keyPair.PublicKey,
            CreatedAt = VietnamTime.DatabaseNow,
            CreatedBy = actorUserId,
            UpdatedAt = VietnamTime.DatabaseNow,
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
            CreatedAt = VietnamTime.DatabaseNow,
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

    public async Task<AuthResponse> RegisterDoctorAsync(RegisterDoctorRequest request)
    {
        var verifiedStatus = GetInitialVerificationStatus();

        return await RegisterProfileAsync(
            request,
            RoleName.Doctor,
            async user =>
            {
                if (await _doctorRepository.ExistsAsync(d => d.UserId == user.UserId))
                {
                    return;
                }

                await _doctorRepository.AddAsync(new Doctor
                {
                    UserId = user.UserId,
                    Specialty = request.Specialty,
                    LicenseNumber = request.LicenseNumber,
                    LicenseImage = request.LicenseImage,
                    VerifiedStatus = verifiedStatus
                });
            },
            "Đăng ký tài khoản bác sĩ thành công.");
    }

    public async Task<AuthResponse> RegisterStaffAsync(RegisterStaffRequest request)
    {
        var verifiedStatus = GetInitialVerificationStatus();

        var roleName = request.Role switch
        {
            StaffRole.Nurse => RoleName.Nurse,
            StaffRole.Pharmacist => RoleName.Pharmacist,
            StaffRole.Receptionist => RoleName.Receptionist,
            StaffRole.LabTech => RoleName.LabTech,
            _ => throw new InvalidOperationException($"Unsupported staff role '{request.Role}'.")
        };

        return await RegisterProfileAsync(
            request,
            roleName,
            async user =>
            {
                if (await _staffRepository.ExistsAsync(s => s.UserId == user.UserId))
                {
                    return;
                }

                await _staffRepository.AddAsync(new Staff
                {
                    UserId = user.UserId,
                    Role = request.Role,
                    LicenseNumber = request.LicenseNumber,
                    Specialty = request.Specialty,
                    VerifiedStatus = verifiedStatus
                });
            },
            "Đăng ký tài khoản nhân sự thành công.");
    }

    public async Task<AuthResponse> RegisterStaffDoctorAsync(RegisterStaffDoctorRequest request)
    {
                var actorUserId = GetCurrentActorId();

        // Validate email format
        if (!IsValidEmail(request.Email))
        {
            _logger.LogWarning("Invalid email format provided: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "Email không hợp lệ." };
        }

        // Check if user with this email already exists
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            _logger.LogWarning("User with this email already exists: {Email}", request.Email);
            return new AuthResponse { Success = false, Message = "Email này đã được sử dụng." };
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var normalizedPhone = request.Phone.Trim();
            if (await _userRepository.ExistsAsync(u => u.Phone == normalizedPhone && u.Status != Models.Enums.UserStatus.Inactive))
            {
                _logger.LogWarning("User with this phone already exists: {Phone}", normalizedPhone);
                return new AuthResponse { Success = false, Message = "Số điện thoại này đã được sử dụng." };
            }

            request.Phone = normalizedPhone;
        }
        if (request.Password.Length < 6)
            {
                return new AuthResponse { Success = false, Message = "Mật khẩu phải có ít nhất 8 ký tự." };
            }
        if (Enum.TryParse<RoleName>(request.Role, true, out var roleName) && roleName == RoleName.Doctor)
        {
            return await RegisterDoctorAsync(new RegisterDoctorRequest
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = request.Password,
                Phone = request.Phone,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                Address = request.Address,
                OrganizationId = request.OrganizationId,
                Specialty = request.Specialty,
                LicenseNumber = request.LicenseNumber,
                LicenseImage = request.LicenseImage,
                VerifiedStatus = request.VerifiedStatus
            });
        }

        if (Enum.TryParse<StaffRole>(request.Role, true, out var staffRole))
        {
            return await RegisterStaffAsync(new RegisterStaffRequest
            {
                FullName = request.FullName,
                Email = request.Email,
                Password = request.Password,
                Phone = request.Phone,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                Address = request.Address,
                OrganizationId = request.OrganizationId,
                Role = staffRole,
                LicenseNumber = request.LicenseNumber,
                Specialty = request.Specialty,
                VerifiedStatus = request.VerifiedStatus
            });
        }

        return new AuthResponse { Success = false, Message = "Quyền (Role) không hợp lệ." };
    }

    public async Task<AuthResponse> VerifyDoctorAsync(Guid doctorId)
    {
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        if (doctor == null)
        {
            return new AuthResponse { Success = false, Message = "Không tìm thấy hồ sơ bác sĩ." };
        }

        doctor.VerifiedStatus = VerificationStatus.Verified;
        await _doctorRepository.UpdateAsync(doctor);

        return new AuthResponse { Success = true, Message = "Xác minh bác sĩ thành công." };
    }

    public async Task<AuthResponse> VerifyStaffAsync(Guid staffId)
    {
        var staff = await _staffRepository.GetByIdAsync(staffId);
        if (staff == null)
        {
            return new AuthResponse { Success = false, Message = "Không tìm thấy hồ sơ nhân sự." };
        }

        staff.VerifiedStatus = VerificationStatus.Verified;
        await _staffRepository.UpdateAsync(staff);

        return new AuthResponse { Success = true, Message = "Xác minh nhân sự thành công." };
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

        // Remove all existing roles to ensure one primary role (unless you explicitly support multiple, which current UI does not)
        var existingUserRoles = await _userRoleRepository.FindManyAsync(ur => ur.UserId == user.UserId);
        var existingRolesList = existingUserRoles.ToList();
        
        if (existingRolesList.Count == 1 && existingRolesList[0].RoleId == newRoleEntity.RoleId)
        {
            return new AuthResponse { Success = true, Message = "Người dùng đã sở hữu quyền này." };
        }

        foreach (var userRole in existingRolesList)
        {
            await _userRoleRepository.DeleteAsync(userRole);
        }

        // Add the new role
        await _userRoleRepository.AddAsync(new UserRole
        {
            UserId = user.UserId,
            RoleId = newRoleEntity.RoleId
        });

        // Clean up ghost profiles based on role transition
        if (newRoleEnum == RoleName.Doctor)
        {
            var staffProfile = await _staffRepository.FindAsync(s => s.UserId == user.UserId);
            if (staffProfile != null) await _staffRepository.DeleteAsync(staffProfile);
        }
        else if (newRoleEnum is RoleName.Nurse or RoleName.Pharmacist or RoleName.Receptionist or RoleName.LabTech or RoleName.Admin)
        {
            var doctorProfile = await _doctorRepository.FindAsync(d => d.UserId == user.UserId);
            if (doctorProfile != null) await _doctorRepository.DeleteAsync(doctorProfile);
        }

        await EnsureRoleProfileAsync(user.UserId, newRoleEntity.RoleName);
        await RevokeTokenAsync(request.UserId);
        await _cache.RemoveAsync($"profile:{request.UserId}");

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

        var now = DateTime.UtcNow;
        var security = await _securityRepository.FindAsync(s => s.UserId == userId);
        if (security == null)
        {
            await _securityRepository.AddAsync(new UserSecurity
            {
                UserId = userId,
                MfaEnabled = false,
                LastPasswordChange = now
            });
        }
        else
        {
            security.LastPasswordChange = now;
            await _securityRepository.UpdateAsync(security);
        }

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
        var cacheKey = $"profile:{userId}";
        var cached = await _cache.GetAsync<UserProfileResponse>(cacheKey);
        if (cached != null) return cached;

        var user = await _userRepository.GetByIdWithProfileAsync(userId);
        if (user == null) return null;

        var result = BuildUserProfileResponse(user);
        await _cache.SetAsync(cacheKey, result, ProfileCacheTtl);
        return result;
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

    private async Task<AuthResponse> RegisterProfileAsync<TRequest>(
        TRequest request,
        RoleName roleName,
        Func<User, Task> profileFactory,
        string successMessage)
        where TRequest : RegisterProfileBaseRequest
    {
        var actorUserId = GetCurrentActorId();
        var normalizedEmail = request.Email.Trim();

        // Validate email format
        if (!IsValidEmail(normalizedEmail))
        {
            _logger.LogWarning("Registration rejected for role {Role}: invalid email format {Email}", roleName, normalizedEmail);
            return new AuthResponse { Success = false, Message = "Email không hợp lệ." };
        }

        if (await _userRepository.ExistsAsync(u => u.Email == normalizedEmail))
        {
            _logger.LogWarning("Registration rejected for role {Role}: email already exists {Email}", roleName, normalizedEmail);
            return new AuthResponse { Success = false, Message = "Email này đã được sử dụng." };
        }

        var normalizedPhone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            if (!Regex.IsMatch(normalizedPhone, @"^(0|\+84)\d{9,10}$"))
            {
                return new AuthResponse { Success = false, Message = "Số điện thoại không hợp lệ (phải có 10-11 chữ số và bắt đầu bằng 0 hoặc +84)." };
            }

            if (await _userRepository.ExistsAsync(u => u.Phone == normalizedPhone))
            {
                _logger.LogWarning("Registration rejected for role {Role}: phone already exists {Phone}", roleName, normalizedPhone);
                return new AuthResponse { Success = false, Message = "Số điện thoại này đã được sử dụng." };
            }
        }

        var keyPair = AsymmetricEncryptionService.GenerateKeyPair();
        var user = new User
        {
            FullName = request.FullName,
            Email = normalizedEmail,
            Phone = normalizedPhone,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth.HasValue
                ? DateTime.SpecifyKind(request.DateOfBirth.Value, DateTimeKind.Utc)
                : null,
            Address = request.Address,
            OrganizationId = string.IsNullOrWhiteSpace(request.OrganizationId) ? null : request.OrganizationId.Trim(),
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Status = Models.Enums.UserStatus.Active,
            PublicKey = keyPair.PublicKey,
            CreatedAt = VietnamTime.DatabaseNow,
            CreatedBy = actorUserId,
            UpdatedAt = VietnamTime.DatabaseNow,
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
        }

        await profileFactory(user);

        var security = new UserSecurity
        {
            UserId = user.UserId,
            MfaEnabled = false
        };
        await _securityRepository.AddAsync(security);

        await _credentialRepository.AddAsync(new UserCredential
        {
            UserId = user.UserId,
            Provider = ProviderType.EncryptedPrivateKey,
            CredentialValue = MasterKeyEncryptionService.Encrypt(keyPair.PrivateKey),
            CreatedAt = VietnamTime.DatabaseNow,
        });

        var organizationWarning = await HandleOrganizationMembershipAsync(user, user.OrganizationId, null);

        _blockchainSyncService.EnqueueFabricCaEnrollment(
            enrollmentId: user.UserId.ToString(),
            username: user.FullName ?? user.Email ?? user.UserId.ToString(),
            role: assignedRole?.RoleName.ToString() ?? roleName.ToString(),
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
                ? successMessage
                : $"{successMessage} {organizationWarning}",
            UserId = user.UserId,
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
            if (!Regex.IsMatch(normalizedPhone, @"^(0|\+84)\d{9,10}$"))
            {
                return new AuthResponse { Success = false, Message = "Số điện thoại không hợp lệ (phải có 10-11 chữ số và bắt đầu bằng 0 hoặc +84)." };
            }

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
            var dob = DateTime.SpecifyKind(request.DateOfBirth.Value, DateTimeKind.Utc);
            if (dob > VietnamTime.DatabaseNow)
            {
                return new AuthResponse { Success = false, Message = "Ngày sinh không được lớn hơn ngày hiện tại." };
            }
            user.DateOfBirth = dob;
            isUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Address) && user.Address != request.Address)
        {
            user.Address = request.Address;
            isUpdated = true;
        }

        if (isUpdated)
        {
            user.UpdatedAt = VietnamTime.DatabaseNow;
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
        await _cache.RemoveAsync($"profile:{userId}");

        return new AuthResponse { Success = true, Message = "Cập nhật hồ sơ cá nhân thành công." };
    }

    public async Task<AuthResponse> UpdateUserAsync(Guid userId, AdminUpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdWithProfileAsync(userId);
        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "Không tìm thấy tài khoản người dùng." };
        }

        var hasChanges = false;

        if (!string.IsNullOrWhiteSpace(request.FullName) && !string.Equals(user.FullName, request.FullName, StringComparison.Ordinal))
        {
            user.FullName = request.FullName;
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim();
            if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailExists = await _userRepository.ExistsAsync(u => u.Email == normalizedEmail && u.UserId != userId);
                if (emailExists)
                {
                    return new AuthResponse { Success = false, Message = "Email này đã được sử dụng." };
                }

                user.Email = normalizedEmail;
                hasChanges = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var normalizedPhone = request.Phone.Trim();
            if (!string.Equals(user.Phone, normalizedPhone, StringComparison.Ordinal))
            {
                if (!Regex.IsMatch(normalizedPhone, @"^(0|\+84)\d{9,10}$"))
                {
                    return new AuthResponse { Success = false, Message = "Số điện thoại không hợp lệ (phải có 10-11 chữ số và bắt đầu bằng 0 hoặc +84)." };
                }

                var phoneExists = await _userRepository.ExistsAsync(u => u.Phone == normalizedPhone && u.UserId != userId);
                if (phoneExists)
                {
                    return new AuthResponse { Success = false, Message = "Số điện thoại này đã được sử dụng." };
                }

                user.Phone = normalizedPhone;
                hasChanges = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Gender) && !string.Equals(user.Gender, request.Gender, StringComparison.Ordinal))
        {
            user.Gender = request.Gender;
            hasChanges = true;
        }

        if (request.DateOfBirth.HasValue && user.DateOfBirth != request.DateOfBirth.Value)
        {
            var dob = DateTime.SpecifyKind(request.DateOfBirth.Value, DateTimeKind.Utc);
            if (dob > VietnamTime.DatabaseNow)
            {
                return new AuthResponse { Success = false, Message = "Ngày sinh không được lớn hơn ngày hiện tại." };
            }
            user.DateOfBirth = dob;
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Address) && !string.Equals(user.Address, request.Address, StringComparison.Ordinal))
        {
            user.Address = request.Address;
            hasChanges = true;
        }

        if (!string.IsNullOrWhiteSpace(request.OrganizationId) && !string.Equals(user.OrganizationId, request.OrganizationId.Trim(), StringComparison.Ordinal))
        {
            user.OrganizationId = request.OrganizationId.Trim();
            hasChanges = true;
        }

        if (request.Status.HasValue && user.Status != request.Status.Value)
        {
            user.Status = request.Status.Value;
            hasChanges = true;
        }

        if (hasChanges)
        {
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = GetCurrentActorId();
            await _userRepository.UpdateAsync(user);
        }

        await _cache.RemoveAsync($"profile:{userId}");
        return new AuthResponse
        {
            Success = true,
            Message = "Cập nhật người dùng thành công.",
            UserId = user.UserId
        };
    }

    public async Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, bool isAdminOverride = false)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return new AuthResponse { Success = false, Message = "Mật khẩu mới không được để trống." };
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "Không tìm thấy tài khoản người dùng." };
        }

        if (!isAdminOverride)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(user.Password) || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            {
                return new AuthResponse { Success = false, Message = "Mật khẩu hiện tại không chính xác." };
            }
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = GetCurrentActorId();
        await _userRepository.UpdateAsync(user);

        var security = await _securityRepository.FindAsync(s => s.UserId == userId);
        if (security == null)
        {
            await _securityRepository.AddAsync(new UserSecurity
            {
                UserId = userId,
                MfaEnabled = false,
                LastPasswordChange = DateTime.UtcNow
            });
        }
        else
        {
            security.LastPasswordChange = DateTime.UtcNow;
            await _securityRepository.UpdateAsync(security);
        }

        await RevokeTokenAsync(userId);

        return new AuthResponse
        {
            Success = true,
            Message = isAdminOverride ? "Cập nhật mật khẩu người dùng thành công." : "Đổi mật khẩu thành công."
        };
    }

    public async Task<AuthResponse> AdminChangePasswordAsync(Guid userId, AdminChangePasswordRequest request)
    {
        return await ChangePasswordAsync(userId, new ChangePasswordRequest
        {
            CurrentPassword = string.Empty,
            NewPassword = request.NewPassword
        }, isAdminOverride: true);
    }

    public async Task<Guid?> GetUserIdByProfileIdAsync(Guid? patientId, Guid? doctorId, Guid? staffId = null)
    {
        if (patientId.HasValue)
        {
            var patientUser = await _userRepository.GetByIdAsync(patientId.Value);
            if (patientUser != null)
            {
                return patientUser.UserId;
            }

            var patient = await _patientRepository.FindAsync(p => p.PatientId == patientId.Value);
            return patient?.UserId;
        }

        if (doctorId.HasValue)
        {
            var doctorUser = await _userRepository.GetByIdAsync(doctorId.Value);
            if (doctorUser != null)
            {
                return doctorUser.UserId;
            }

            var doctor = await _doctorRepository.FindAsync(d => d.DoctorId == doctorId.Value);
            return doctor?.UserId;
        }

        if (staffId.HasValue)
        {
            // First check if staffId is directly a userId (fallback for already-normalized IDs)
            var staffUser = await _userRepository.GetByIdAsync(staffId.Value);
            if (staffUser != null)
            {
                return staffUser.UserId;
            }

            // Look up the Staff profile by StaffId to get the linked UserId
            var staff = await _staffRepository.FindAsync(s => s.StaffId == staffId.Value);
            return staff?.UserId;
        }

        return null;
    }

    public async Task<UserKeysDto?> GetUserKeysAsync(Guid userId)
    {
        var keyCacheKey = $"keys:{userId}";
        var keyCached = await _cache.GetAsync<UserKeysDto>(keyCacheKey);
        if (keyCached != null) return keyCached;

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

        var keyResult = new UserKeysDto
        {
            UserId = userId,
            PublicKey = user.PublicKey,
            EncryptedPrivateKey = privateCredential.CredentialValue
        };
        await _cache.SetAsync(keyCacheKey, keyResult, KeysCacheTtl);
        return keyResult;
    }

    public async Task<PagedResponse<UserProfileResponse>> GetAllUsersAsync(GetAllUsersQuery query, bool isAdminActor)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 10 : Math.Min(query.PageSize, 100);

        var normalizedGender = string.IsNullOrWhiteSpace(query.Gender) ? null : query.Gender.Trim();
        var normalizedOrganizationId = string.IsNullOrWhiteSpace(query.OrganizationId) ? null : query.OrganizationId.Trim();
        var normalizedStatus = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim();
        var normalizedRole = query.Role?.ToString();
        var normalizedSpecialty = string.IsNullOrWhiteSpace(query.Specialty) ? null : query.Specialty.Trim();

        RoleName? requestedRole = null;
        var roleIsStaffBucket = false;

        if (!string.IsNullOrWhiteSpace(normalizedRole))
        {
            if (string.Equals(normalizedRole, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                roleIsStaffBucket = true;
            }
            else if (!Enum.TryParse<RoleName>(normalizedRole, true, out var parsedRole))
            {
                return new PagedResponse<UserProfileResponse>
                {
                    Success = false,
                    Message = $"Role '{normalizedRole}' is invalid.",
                    Page = page,
                    PageSize = pageSize
                };
            }
            else
            {
                var roleExists = await _roleRepository.ExistsAsync(r => r.RoleName == parsedRole);
                if (!roleExists)
                {
                    return new PagedResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = $"Role '{normalizedRole}' does not exist in role data.",
                        Page = page,
                        PageSize = pageSize
                    };
                }

                if (parsedRole == RoleName.Admin && !isAdminActor)
                {
                    return new PagedResponse<UserProfileResponse>
                    {
                        Success = false,
                        Message = "Only admin can search admin users.",
                        Page = page,
                        PageSize = pageSize
                    };
                }

                requestedRole = parsedRole;
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedStatus) && !Enum.TryParse<UserStatus>(normalizedStatus, true, out var parsedStatus))
        {
            return new PagedResponse<UserProfileResponse>
            {
                Success = false,
                Message = $"Status '{normalizedStatus}' is invalid.",
                Page = page,
                PageSize = pageSize
            };
        }

        var userQuery = _dbContext.Users
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.Trim();
            var searchPattern = $"%{searchTerm}%";
            userQuery = userQuery.Where(u => 
                (u.FullName != null && EF.Functions.ILike(u.FullName, searchPattern)) ||
                (u.Email != null && EF.Functions.ILike(u.Email, searchPattern)) ||
                (u.Phone != null && EF.Functions.ILike(u.Phone, searchPattern)) ||
                u.UserId.ToString() == searchTerm
            );
        }

        if (!string.IsNullOrWhiteSpace(normalizedGender))
        {
            userQuery = userQuery.Where(u => u.Gender != null && EF.Functions.ILike(u.Gender, normalizedGender));
        }

        if (!string.IsNullOrWhiteSpace(normalizedOrganizationId))
        {
            userQuery = userQuery.Where(u => u.OrganizationId != null && EF.Functions.ILike(u.OrganizationId, normalizedOrganizationId));
        }

        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            var parsedStatusR = Enum.Parse<UserStatus>(normalizedStatus, true);
            userQuery = userQuery.Where(u => u.Status == parsedStatusR);
        }

        if (roleIsStaffBucket)
        {
            userQuery = userQuery.Where(u => u.UserRoles.Any(ur =>
                ur.Role.RoleName == RoleName.Nurse ||
                ur.Role.RoleName == RoleName.Pharmacist ||
                ur.Role.RoleName == RoleName.Receptionist ||
                ur.Role.RoleName == RoleName.LabTech));
        }
        else if (requestedRole.HasValue)
        {
            var roleFilter = requestedRole.Value;
            userQuery = userQuery.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == roleFilter));
        }

        if (!string.IsNullOrWhiteSpace(normalizedSpecialty))
        {
            var canUseDoctorSpecialty = requestedRole == RoleName.Doctor;
            var canUseStaffSpecialty = roleIsStaffBucket || requestedRole is RoleName.Nurse or RoleName.Pharmacist or RoleName.Receptionist or RoleName.LabTech;

            if (canUseDoctorSpecialty)
            {
                userQuery = userQuery.Where(u => u.DoctorProfile != null && u.DoctorProfile.Specialty != null && EF.Functions.ILike(u.DoctorProfile.Specialty, $"%{normalizedSpecialty}%"));
            }
            else if (canUseStaffSpecialty)
            {
                userQuery = userQuery.Where(u => u.StaffProfile != null && u.StaffProfile.Specialty != null && EF.Functions.ILike(u.StaffProfile.Specialty, $"%{normalizedSpecialty}%"));
            }
        }

        var totalCount = await userQuery.CountAsync();
        var userIds = await userQuery
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => u.UserId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var users = new List<UserProfileResponse>(userIds.Count);
        foreach (var userId in userIds)
        {
            var profile = await GetMyProfileAsync(userId);
            if (profile != null)
            {
                users.Add(profile);
            }
        }

        return new PagedResponse<UserProfileResponse>
        {
            Success = true,
            Message = "Users fetched successfully.",
            Data = users,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<List<Guid>> SearchUserIdsAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return new List<Guid>();

        // Normalize Vietnamese characters to NFC to ensure consistent matching
        var normalizedKeyword = keyword.Trim().Normalize(System.Text.NormalizationForm.FormC);
        var searchPattern = $"%{normalizedKeyword}%";
        
        // Use a queryable to build the filter
        var query = _dbContext.Users.AsNoTracking();

        // Try to parse keyword as Guid for UserId exact match
        if (Guid.TryParse(normalizedKeyword, out var searchGuid))
        {
            query = query.Where(u => 
                u.UserId == searchGuid ||
                (u.FullName != null && (
                    EF.Functions.ILike(u.FullName, searchPattern) || 
                    EF.Functions.ILike(EF.Functions.Unaccent(u.FullName), EF.Functions.Unaccent(normalizedKeyword))
                )) ||
                (u.Email != null && EF.Functions.ILike(u.Email, searchPattern)) ||
                (u.Phone != null && EF.Functions.ILike(u.Phone, searchPattern))
            );
        }
        else
        {
            query = query.Where(u =>
                (u.FullName != null && (
                    EF.Functions.ILike(u.FullName, searchPattern) || 
                    EF.Functions.ILike(EF.Functions.Unaccent(u.FullName), EF.Functions.Unaccent(normalizedKeyword))
                )) ||
                (u.Email != null && EF.Functions.ILike(u.Email, searchPattern)) ||
                (u.Phone != null && EF.Functions.ILike(u.Phone, searchPattern))
            );
        }

        _logger.LogWarning("[AuthService] Searching for users with keyword: '{Keyword}'", normalizedKeyword);
        
        var userIds = await query
            .Select(u => u.UserId)
            .Take(1000)
            .ToListAsync();

        _logger.LogWarning("[AuthService] Found {Count} matching user IDs", userIds.Count);
        return userIds;
    }



    /// <summary>
    /// Reactivate a deactivated account: overwrite all personal fields with fresh data,
    /// set status back to Active, regenerate keys, and clear old credentials.
    /// </summary>
    private async Task<AuthResponse> ReactivateAccountAsync(User existingUser, RegisterRequest request, Guid? actorUserId)
    {
        // Generate new RSA/ECC Key Pair
        var keyPair = AsymmetricEncryptionService.GenerateKeyPair();

        // Overwrite all personal fields with fresh registration data
        existingUser.FullName = request.FullName;
        existingUser.Phone = !string.IsNullOrWhiteSpace(request.Phone) ? request.Phone.Trim() : null;
        existingUser.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        existingUser.Gender = null;
        existingUser.DateOfBirth = null;
        existingUser.Address = null;
        existingUser.Status = Models.Enums.UserStatus.Active;
        existingUser.PublicKey = keyPair.PublicKey;
        existingUser.UpdatedAt = VietnamTime.DatabaseNow;
        existingUser.UpdatedBy = actorUserId;

        await _userRepository.UpdateAsync(existingUser);
        await _cache.RemoveAsync($"profile:{existingUser.UserId}");

        // Remove old encrypted private key and store new one
        var oldPrivateKey = await _credentialRepository.FindAsync(c =>
            c.UserId == existingUser.UserId && c.Provider == ProviderType.EncryptedPrivateKey);
        if (oldPrivateKey != null)
        {
            oldPrivateKey.CredentialValue = MasterKeyEncryptionService.Encrypt(keyPair.PrivateKey);
            oldPrivateKey.CreatedAt = VietnamTime.DatabaseNow;
            await _credentialRepository.UpdateAsync(oldPrivateKey);
        }
        else
        {
            await _credentialRepository.AddAsync(new UserCredential
            {
                UserId = existingUser.UserId,
                Provider = ProviderType.EncryptedPrivateKey,
                CredentialValue = MasterKeyEncryptionService.Encrypt(keyPair.PrivateKey),
                CreatedAt = VietnamTime.DatabaseNow,
            });
        }

        // Ensure Patient role and profile exist
        var patientRole = await _roleRepository.FindAsync(r => r.RoleName == RoleName.Patient);
        if (patientRole != null)
        {
            var existingUserRole = await _userRoleRepository.FindAsync(ur => ur.UserId == existingUser.UserId && ur.RoleId == patientRole.RoleId);
            if (existingUserRole == null)
            {
                await _userRoleRepository.AddAsync(new UserRole
                {
                    UserId = existingUser.UserId,
                    RoleId = patientRole.RoleId
                });
            }

            await EnsureRoleProfileAsync(existingUser.UserId, patientRole.RoleName);
        }

        // Ensure Security record exists
        var existingSecurity = await _securityRepository.FindAsync(s => s.UserId == existingUser.UserId);
        if (existingSecurity == null)
        {
            await _securityRepository.AddAsync(new UserSecurity
            {
                UserId = existingUser.UserId,
                MfaEnabled = false
            });
        }

        _logger.LogInformation("Account reactivated for user {UserId} ({Email})", existingUser.UserId, existingUser.Email);

        await _cache.RemoveAsync($"profile:{existingUser.UserId}");

        return new AuthResponse
        {
            Success = true,
            Message = "Tài khoản đã được kích hoạt lại thành công.",
            UserId = existingUser.UserId,
        };
    }

    /// <summary>
    /// Deactivate a user account: set status to Inactive, clear personal fields,
    /// and revoke all tokens. The email is preserved so re-registration can find it.
    /// </summary>
    public async Task<AuthResponse> DeactivateAccountAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "Không tìm thấy tài khoản người dùng." };
        }

        if (user.Status == Models.Enums.UserStatus.Inactive)
        {
            return new AuthResponse { Success = false, Message = "Tài khoản đã bị vô hiệu hóa trước đó." };
        }

        // Clear personal fields but keep email for re-registration lookup
        user.FullName = null;
        user.Phone = null;
        user.Gender = null;
        user.DateOfBirth = null;
        user.Address = null;
        user.Password = null;
        user.Status = Models.Enums.UserStatus.Inactive;
        user.UpdatedAt = VietnamTime.DatabaseNow;
        user.UpdatedBy = userId;

        await _userRepository.UpdateAsync(user);
        await _cache.RemoveAsync($"profile:{userId}");

        // Revoke all refresh tokens
        await RevokeTokenAsync(userId);

        _logger.LogInformation("Account deactivated for user {UserId} ({Email})", userId, user.Email);

        return new AuthResponse
        {
            Success = true,
            Message = "Tài khoản đã được vô hiệu hóa thành công.",
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
            existingCredential.CreatedAt = VietnamTime.DatabaseNow;
            await _credentialRepository.UpdateAsync(existingCredential);
        }
        else
        {
            await _credentialRepository.AddAsync(new UserCredential
            {
                UserId = user.UserId,
                Provider = Models.Enums.ProviderType.RefreshToken,
                CredentialValue = refreshToken,
                CreatedAt = VietnamTime.DatabaseNow,
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

    private VerificationStatus GetInitialVerificationStatus()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole(RoleName.Admin.ToString()) == true
            ? VerificationStatus.Verified
            : VerificationStatus.Pending;
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        return Regex.IsMatch(email, @"^[a-zA-Z0-9._%-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
    }
    
    public async Task<AuthResponse> UpdateUserStatusAsync(Guid userId, string status)
    {
        if (!Enum.TryParse<UserStatus>(status, true, out var newStatus))
        {
            return new AuthResponse
            {
                Success = false,
                Message = $"Trạng thái '{status}' không hợp lệ. Các giá trị hợp lệ: Active, Inactive, Suspended, Pending."
            };
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "Không tìm thấy tài khoản người dùng." };
        }

        if (user.Status == newStatus)
        {
            return new AuthResponse { Success = true, Message = "Người dùng đã có trạng thái này." };
        }

        user.Status = newStatus;
        user.UpdatedAt = VietnamTime.DatabaseNow;
        user.UpdatedBy = GetCurrentActorId();
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("User {UserId} status updated to {Status} by actor {ActorId}.",
            userId, newStatus, GetCurrentActorId());

        return new AuthResponse
        {
            Success = true,
            Message = $"Cập nhật trạng thái người dùng thành '{newStatus}' thành công.",
            UserId = userId
        };
    }
}

