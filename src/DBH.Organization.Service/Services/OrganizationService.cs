using DBH.Organization.Service.DbContext;
using DBH.Organization.Service.DTOs;
using DBH.Organization.Service.Models.Entities;
using DBH.Organization.Service.Models.Enums;
using DBH.Shared.Contracts;
using DBH.Shared.Infrastructure.Caching;
using DBH.Shared.Infrastructure.cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DBH.Organization.Service.Services;

public class OrganizationService : IOrganizationService
{
    private readonly OrganizationDbContext _context;
    private readonly ILogger<OrganizationService> _logger;
    private readonly IAuthUserClient _authUserClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICacheService _cache;

    private static readonly TimeSpan OrgCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(5);

    public OrganizationService(
        OrganizationDbContext context,
        ILogger<OrganizationService> logger,
        IAuthUserClient authUserClient,
        IHttpContextAccessor httpContextAccessor,
        ICacheService cache)
    {
        _context = context;
        _logger = logger;
        _authUserClient = authUserClient;
        _httpContextAccessor = httpContextAccessor;
        _cache = cache;
    }

    // =========================================================================
    // ORGANIZATIONS
    // =========================================================================

    public async Task<ApiResponse<OrganizationResponse>> CreateOrganizationAsync(CreateOrganizationRequest request)
    {
        var org = new Models.Entities.Organization
        {
            OrgName = request.OrgName,
            OrgCode = request.OrgCode,
            OrgType = request.OrgType,
            LicenseNumber = request.LicenseNumber,
            TaxId = request.TaxId,
            Address = request.Address,
            ContactInfo = request.ContactInfo,
            Website = request.Website,
            Status = OrganizationStatus.PENDING_VERIFICATION
        };

        _context.Organizations.Add(org);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created organization {OrgId}: {OrgName}", org.OrgId, org.OrgName);

        return new ApiResponse<OrganizationResponse>
        {
            Success = true,
            Message = "Organization created successfully",
            Data = MapToResponse(org)
        };
    }

    public async Task<ApiResponse<OrganizationResponse>> GetOrganizationByIdAsync(Guid orgId)
    {
        var cacheKey = $"org:{orgId}";
        var cached = await _cache.GetAsync<ApiResponse<OrganizationResponse>>(cacheKey);
        if (cached != null) return cached;

        var org = await _context.Organizations
            .Include(o => o.Departments)
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.OrgId == orgId);

        if (org == null)
        {
            return new ApiResponse<OrganizationResponse>
            {
                Success = false,
                Message = "Không tìm thấy tổ chức / bệnh viện."
            };
        }

        var result = new ApiResponse<OrganizationResponse>
        {
            Success = true,
            Data = MapToResponse(org)
        };
        await _cache.SetAsync(cacheKey, result, OrgCacheTtl);
        return result;
    }

    public async Task<PagedResponse<OrganizationResponse>> GetOrganizationsAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        var cacheKey = $"orgs:list:{page}:{pageSize}:{search ?? string.Empty}";
        var cached = await _cache.GetAsync<PagedResponse<OrganizationResponse>>(cacheKey);
        if (cached != null) return cached;

        var query = _context.Organizations
            .Include(o => o.Departments)
            .Include(o => o.Memberships)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => 
                o.OrgName.Contains(search) || 
                (o.OrgCode != null && o.OrgCode.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PagedResponse<OrganizationResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
        await _cache.SetAsync(cacheKey, result, ListCacheTtl);
        return result;
    }

    public async Task<ApiResponse<OrganizationResponse>> UpdateOrganizationAsync(Guid orgId, UpdateOrganizationRequest request)
    {
        var org = await _context.Organizations.FindAsync(orgId);
        if (org == null)
        {
            return new ApiResponse<OrganizationResponse>
            {
                Success = false,
                Message = "Không tìm thấy tổ chức / bệnh viện."
            };
        }

        if (request.OrgName != null) org.OrgName = request.OrgName;
        if (request.OrgCode != null) org.OrgCode = request.OrgCode;
        if (request.OrgType.HasValue) org.OrgType = request.OrgType.Value;
        if (request.LicenseNumber != null) org.LicenseNumber = request.LicenseNumber;
        if (request.TaxId != null) org.TaxId = request.TaxId;
        if (request.Address != null) org.Address = request.Address;
        if (request.ContactInfo != null) org.ContactInfo = request.ContactInfo;
        if (request.Website != null) org.Website = request.Website;
        if (request.LogoUrl != null) org.LogoUrl = request.LogoUrl;
        if (request.Settings != null) org.Settings = request.Settings;
        org.UpdatedAt = VietnamTimeHelper.Now;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"org:{orgId}");
        await _cache.RemoveByPatternAsync("orgs:list:*");

        return new ApiResponse<OrganizationResponse>
        {
            Success = true,
            Message = "Organization updated successfully",
            Data = MapToResponse(org)
        };
    }

    public async Task<ApiResponse<bool>> DeleteOrganizationAsync(Guid orgId)
    {
        var org = await _context.Organizations.FindAsync(orgId);
        if (org == null)
        {
            return new ApiResponse<bool> { Success = false, Message = "Không tìm thấy tổ chức / bệnh viện." };
        }

        org.Status = OrganizationStatus.INACTIVE;
        org.UpdatedAt = VietnamTimeHelper.Now;
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"org:{orgId}");
        await _cache.RemoveByPatternAsync("orgs:list:*");

        return new ApiResponse<bool> { Success = true, Message = "Organization deactivated", Data = true };
    }

    public async Task<ApiResponse<OrganizationResponse>> VerifyOrganizationAsync(Guid orgId, Guid verifiedByUserId)
    {
        var org = await _context.Organizations.FindAsync(orgId);
        if (org == null)
        {
            return new ApiResponse<OrganizationResponse>
            {
                Success = false,
                Message = "Không tìm thấy tổ chức / bệnh viện."
            };
        }

        org.Status = OrganizationStatus.ACTIVE;
        org.VerifiedAt = VietnamTimeHelper.Now;
        org.VerifiedBy = verifiedByUserId;
        org.UpdatedAt = VietnamTimeHelper.Now;

        // Generate DID for blockchain
        org.OrgDid = $"did:fabric:org:{org.OrgId:N}";

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"org:{orgId}");
        await _cache.RemoveByPatternAsync("orgs:list:*");

        _logger.LogInformation("Verified organization {OrgId} by user {UserId}", orgId, verifiedByUserId);

        return new ApiResponse<OrganizationResponse>
        {
            Success = true,
            Message = "Organization verified successfully",
            Data = MapToResponse(org)
        };
    }

    // =========================================================================
    // DEPARTMENTS
    // =========================================================================

    public async Task<ApiResponse<DepartmentResponse>> CreateDepartmentAsync(CreateDepartmentRequest request)
    {
        var orgExists = await _context.Organizations.AnyAsync(o => o.OrgId == request.OrgId);
        if (!orgExists)
        {
            return new ApiResponse<DepartmentResponse>
            {
                Success = false,
                Message = "Không tìm thấy tổ chức / bệnh viện."
            };
        }

        var dept = new Department
        {
            OrgId = request.OrgId,
            DepartmentName = request.DepartmentName,
            DepartmentCode = request.DepartmentCode,
            Description = request.Description,
            HeadUserId = request.HeadUserId,
            ParentDepartmentId = request.ParentDepartmentId,
            Floor = request.Floor,
            RoomNumbers = request.RoomNumbers,
            PhoneExtension = request.PhoneExtension
        };

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        return new ApiResponse<DepartmentResponse>
        {
            Success = true,
            Message = "Department created successfully",
            Data = MapToResponse(dept)
        };
    }

    public async Task<ApiResponse<DepartmentResponse>> GetDepartmentByIdAsync(Guid departmentId)
    {
        var cacheKey = $"dept:{departmentId}";
        var cached = await _cache.GetAsync<ApiResponse<DepartmentResponse>>(cacheKey);
        if (cached != null) return cached;

        var dept = await _context.Departments
            .Include(d => d.Organization)
            .Include(d => d.Memberships)
            .FirstOrDefaultAsync(d => d.DepartmentId == departmentId);

        if (dept == null)
        {
            return new ApiResponse<DepartmentResponse>
            {
                Success = false,
                Message = "Department not found"
            };
        }

        var result = new ApiResponse<DepartmentResponse>
        {
            Success = true,
            Data = MapToResponse(dept)
        };
        await _cache.SetAsync(cacheKey, result, OrgCacheTtl);
        return result;
    }

    public async Task<PagedResponse<DepartmentResponse>> GetDepartmentsByOrgAsync(Guid orgId, int page = 1, int pageSize = 10)
    {
        var deptCacheKey = $"depts:org:{orgId}:{page}:{pageSize}";
        var deptCached = await _cache.GetAsync<PagedResponse<DepartmentResponse>>(deptCacheKey);
        if (deptCached != null) return deptCached;

        var query = _context.Departments
            .Include(d => d.Organization)
            .Include(d => d.Memberships)
            .Where(d => d.OrgId == orgId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(d => d.DepartmentName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var deptListResult = new PagedResponse<DepartmentResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
        await _cache.SetAsync($"depts:org:{orgId}:{page}:{pageSize}", deptListResult, OrgCacheTtl);
        return deptListResult;
    }

    public async Task<ApiResponse<DepartmentResponse>> UpdateDepartmentAsync(Guid departmentId, UpdateDepartmentRequest request)
    {
        var dept = await _context.Departments.FindAsync(departmentId);
        if (dept == null)
        {
            return new ApiResponse<DepartmentResponse>
            {
                Success = false,
                Message = "Department not found"
            };
        }

        if (request.DepartmentName != null) dept.DepartmentName = request.DepartmentName;
        if (request.DepartmentCode != null) dept.DepartmentCode = request.DepartmentCode;
        if (request.Description != null) dept.Description = request.Description;
        if (request.HeadUserId.HasValue) dept.HeadUserId = request.HeadUserId;
        if (request.ParentDepartmentId.HasValue) dept.ParentDepartmentId = request.ParentDepartmentId;
        if (request.Floor != null) dept.Floor = request.Floor;
        if (request.RoomNumbers != null) dept.RoomNumbers = request.RoomNumbers;
        if (request.PhoneExtension != null) dept.PhoneExtension = request.PhoneExtension;
        if (request.Status.HasValue) dept.Status = request.Status.Value;
        dept.UpdatedAt = VietnamTimeHelper.Now;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"dept:{departmentId}");
        await _cache.RemoveByPatternAsync($"depts:org:{dept.OrgId}:*");

        return new ApiResponse<DepartmentResponse>
        {
            Success = true,
            Message = "Department updated successfully",
            Data = MapToResponse(dept)
        };
    }

    public async Task<ApiResponse<bool>> DeleteDepartmentAsync(Guid departmentId)
    {
        var dept = await _context.Departments.FindAsync(departmentId);
        if (dept == null)
        {
            return new ApiResponse<bool> { Success = false, Message = "Department not found" };
        }

        dept.Status = DepartmentStatus.INACTIVE;
        dept.UpdatedAt = VietnamTimeHelper.Now;
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"dept:{departmentId}");
        await _cache.RemoveByPatternAsync($"depts:org:{dept.OrgId}:*");

        return new ApiResponse<bool> { Success = true, Message = "Department deactivated", Data = true };
    }

    // =========================================================================
    // MEMBERSHIPS
    // =========================================================================

    public async Task<ApiResponse<MembershipResponse>> CreateMembershipAsync(CreateMembershipRequest request)
    {
        var actorUserId = GetCurrentActorId();
        var orgExists = await _context.Organizations.AnyAsync(o => o.OrgId == request.OrgId);
        if (!orgExists)
        {
            return new ApiResponse<MembershipResponse>
            {
                Success = false,
                Message = "Không tìm thấy tổ chức / bệnh viện."
            };
        }

        // Check duplicate membership
        var exists = await _context.Memberships.AnyAsync(m => 
            m.UserId == request.UserId && 
            m.OrgId == request.OrgId &&
            m.Status == MembershipStatus.ACTIVE);

        if (exists)
        {
            return new ApiResponse<MembershipResponse>
            {
                Success = false,
                Message = "User already has an active membership in this organization"
            };
        }

        var membership = new Membership
        {
            UserId = request.UserId,
            OrgId = request.OrgId,
            DepartmentId = request.DepartmentId,
            EmployeeId = request.EmployeeId,
            JobTitle = request.JobTitle,
            LicenseNumber = request.LicenseNumber,
            Specialty = request.Specialty,
            Qualifications = request.Qualifications,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            OrgPermissions = request.OrgPermissions,
            Notes = request.Notes,
            CreatedBy = actorUserId,
            UpdatedAt = VietnamTimeHelper.Now,
            UpdatedBy = actorUserId
        };

        _context.Memberships.Add(membership);
        await _context.SaveChangesAsync();
        await _cache.RemoveByPatternAsync($"members:org:{request.OrgId}:*");
        await _cache.RemoveByPatternAsync($"members:user:{request.UserId}:*");

        return new ApiResponse<MembershipResponse>
        {
            Success = true,
            Message = "Membership created successfully",
            Data = MapToResponse(membership)
        };
    }

    public async Task<ApiResponse<MembershipResponse>> GetMembershipByIdAsync(Guid membershipId)
    {
        var mCacheKey = $"member:{membershipId}";
        var mCached = await _cache.GetAsync<ApiResponse<MembershipResponse>>(mCacheKey);
        if (mCached != null) return mCached;

        var membership = await _context.Memberships
            .Include(m => m.Organization)
            .Include(m => m.Department)
            .FirstOrDefaultAsync(m => m.MembershipId == membershipId);

        if (membership == null)
        {
            return new ApiResponse<MembershipResponse>
            {
                Success = false,
                Message = "Membership not found"
            };
        }

        var mResult = new ApiResponse<MembershipResponse>
        {
            Success = true,
            Data = MapToResponse(membership)
        };
        await _cache.SetAsync(mCacheKey, mResult, OrgCacheTtl);
        return mResult;
    }

    public async Task<PagedResponse<MembershipResponse>> GetMembershipsByOrgAsync(Guid orgId, string? search = null, int page = 1, int pageSize = 10)
    {
        var moKey = $"members:org:{orgId}:{search}:{page}:{pageSize}";
        var moCached = await _cache.GetAsync<PagedResponse<MembershipResponse>>(moKey);
        if (moCached != null) return moCached;

        var query = _context.Memberships
            .Include(m => m.Organization)
            .Include(m => m.Department)
            .Where(m => m.OrgId == orgId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchStr = search.Trim();
            var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            var token = authHeader?.Replace("Bearer ", "");

            List<Guid> matchingUserIds = new List<Guid>();
            if (!string.IsNullOrEmpty(token))
            {
                matchingUserIds = await _authUserClient.SearchUserIdsAsync(token, searchStr);
            }

            var searchPattern = $"%{searchStr}%";
            query = query.Where(m => 
                matchingUserIds.Contains(m.UserId) ||
                (m.EmployeeId != null && EF.Functions.ILike(m.EmployeeId, searchPattern)) ||
                (m.JobTitle != null && EF.Functions.ILike(m.JobTitle, searchPattern)) ||
                (m.Specialty != null && EF.Functions.ILike(m.Specialty, searchPattern)) ||
                (m.Department != null && EF.Functions.ILike(m.Department.DepartmentName, searchPattern))
            );
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var authHeaderToken = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString()?.Replace("Bearer ", "");
        var tasks = items.Select(async m =>
        {
            var response = MapToResponse(m);
            if (!string.IsNullOrWhiteSpace(authHeaderToken))
            {
                var userInfo = await _authUserClient.GetUserProfileDetailAsync(authHeaderToken, m.UserId);
                if (userInfo != null && response.User != null)
                {
                    response.User.FullName = userInfo.FullName;
                    response.User.Email = userInfo.Email;
                }
            }
            return response;
        });
        var mappedItems = await Task.WhenAll(tasks);

        var moResult = new PagedResponse<MembershipResponse>
        {
            Data = mappedItems.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
        await _cache.SetAsync(moKey, moResult, ListCacheTtl);
        return moResult;
    }

    public async Task<PagedResponse<MembershipResponse>> GetMembershipsByUserAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        var muKey = $"members:user:{userId}:{page}:{pageSize}";
        var muCached = await _cache.GetAsync<PagedResponse<MembershipResponse>>(muKey);
        if (muCached != null) return muCached;

        var query = _context.Memberships
            .Include(m => m.Organization)
            .Include(m => m.Department)
            .Where(m => m.UserId == userId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var muResult = new PagedResponse<MembershipResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
        await _cache.SetAsync(muKey, muResult, ListCacheTtl);
        return muResult;
    }

    public async Task<PagedResponse<MembershipResponse>> SearchDoctorsAsync(SearchDoctorsRequest request)
    {
        var token = _httpContextAccessor.HttpContext?.Request.Headers.Authorization
            .ToString()
            .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var query = _context.Memberships
            .Include(m => m.Organization)
            .Include(m => m.Department)
            .Where(m => m.Status == MembershipStatus.ACTIVE);

        if (request.OrgId.HasValue)
        {
            query = query.Where(m => m.OrgId == request.OrgId.Value);
        }

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(m => m.DepartmentId == request.DepartmentId.Value);
        }

        var candidates = await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
        
        _logger.LogInformation("Found {Count} candidates for doctor search", candidates.Count);

        var allMapped = await Task.WhenAll(candidates.Select(async m =>
        {
            var response = MapToResponse(m);
            if (!string.IsNullOrWhiteSpace(token))
            {
                var userInfo = await _authUserClient.GetDoctorByUserIdInMyOrganizationAsync(token, m.OrgId, m.UserId);
                if (userInfo != null)
                {
                    var userProfile = await _authUserClient.GetUserProfileDetailAsync(token, userInfo.UserId);
                    response.User = new MembershipUserResponse
                    {
                        UserId = userInfo.UserId,
                        UserProfile = userProfile,
                        FullName = userInfo.FullName,
                        Gender = userInfo.Gender,
                        Email = userInfo.Email,
                        Phone = userInfo.Phone,
                        DateOfBirth = userInfo.DateOfBirth,
                        Address = userInfo.Address,
                        AvatarUrl = userInfo.AvatarUrl,
                        OrganizationId = userInfo.OrganizationId,
                        Status = userInfo.Status
                    };
                }
            }
            return response;
        }));

        var filteredResults = allMapped.ToList();

        // Filter by DateOfBirth if requested (DateOfBirth is in Auth User, so we filter after mapping)
        if (!string.IsNullOrWhiteSpace(request.DateOfBirth))
        {
            if (DateTime.TryParse(request.DateOfBirth, out var searchDob))
            {
                filteredResults = filteredResults.Where(r => 
                    r.User != null && 
                    r.User.DateOfBirth.HasValue && 
                    r.User.DateOfBirth.Value.Date == searchDob.Date).ToList();
            }
        }

        // Filter by Specialty in memory (accent-insensitive)
        if (!string.IsNullOrWhiteSpace(request.Specialty))
        {
            var searchSpecialty = RemoveDiacritics(request.Specialty.Trim().ToLower());
            filteredResults = filteredResults.Where(r => 
                !string.IsNullOrWhiteSpace(r.Specialty) && 
                RemoveDiacritics(r.Specialty.ToLower()).Contains(searchSpecialty)).ToList();
        }

        // Filter by DoctorName in memory (accent-insensitive)
        if (!string.IsNullOrWhiteSpace(request.DoctorName))
        {
            var searchName = RemoveDiacritics(request.DoctorName.Trim().ToLower());
            filteredResults = filteredResults.Where(r => 
                (!string.IsNullOrWhiteSpace(r.User?.FullName) && RemoveDiacritics(r.User.FullName.ToLower()).Contains(searchName)) ||
                (!string.IsNullOrWhiteSpace(r.JobTitle) && RemoveDiacritics(r.JobTitle.ToLower()).Contains(searchName)) ||
                (!string.IsNullOrWhiteSpace(r.EmployeeId) && RemoveDiacritics(r.EmployeeId.ToLower()).Contains(searchName))
            ).ToList();
        }

        var totalCount = filteredResults.Count;
        var paginatedItems = filteredResults
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResponse<MembershipResponse>
        {
            Data = paginatedItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ApiResponse<MembershipResponse>> UpdateMembershipAsync(Guid membershipId, UpdateMembershipRequest request)
    {
        var actorUserId = GetCurrentActorId();
        var membership = await _context.Memberships.FindAsync(membershipId);
        if (membership == null)
        {
            return new ApiResponse<MembershipResponse>
            {
                Success = false,
                Message = "Membership not found"
            };
        }

        if (request.DepartmentId.HasValue) membership.DepartmentId = request.DepartmentId;
        if (request.EmployeeId != null) membership.EmployeeId = request.EmployeeId;
        if (request.JobTitle != null) membership.JobTitle = request.JobTitle;
        if (request.LicenseNumber != null) membership.LicenseNumber = request.LicenseNumber;
        if (request.Specialty != null) membership.Specialty = request.Specialty;
        if (request.Qualifications != null) membership.Qualifications = request.Qualifications;
        if (request.EndDate.HasValue) membership.EndDate = request.EndDate;
        if (request.Status.HasValue) membership.Status = request.Status.Value;
        if (request.OrgPermissions != null) membership.OrgPermissions = request.OrgPermissions;
        if (request.Notes != null) membership.Notes = request.Notes;
        membership.UpdatedAt = VietnamTimeHelper.Now;
        membership.UpdatedBy = actorUserId;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"member:{membershipId}");
        await _cache.RemoveByPatternAsync($"members:org:{membership.OrgId}:*");
        await _cache.RemoveByPatternAsync($"members:user:{membership.UserId}:*");

        return new ApiResponse<MembershipResponse>
        {
            Success = true,
            Message = "Membership updated successfully",
            Data = MapToResponse(membership)
        };
    }

    public async Task<ApiResponse<bool>> DeleteMembershipAsync(Guid membershipId)
    {
        var actorUserId = GetCurrentActorId();
        var membership = await _context.Memberships.FindAsync(membershipId);
        if (membership == null)
        {
            return new ApiResponse<bool> { Success = false, Message = "Membership not found" };
        }

        membership.Status = MembershipStatus.TERMINATED;
        membership.EndDate = VietnamTimeHelper.Today;
        membership.UpdatedAt = VietnamTimeHelper.Now;
        membership.UpdatedBy = actorUserId;
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"member:{membershipId}");
        await _cache.RemoveByPatternAsync($"members:org:{membership.OrgId}:*");
        await _cache.RemoveByPatternAsync($"members:user:{membership.UserId}:*");

        return new ApiResponse<bool> { Success = true, Message = "Membership terminated", Data = true };
    }

    // =========================================================================
    // PAYMENT CONFIG
    // =========================================================================

    public async Task<ApiResponse<PaymentConfigStatusResponse>> ConfigurePaymentAsync(Guid orgId, ConfigurePaymentRequest request)
    {
        var org = await _context.Organizations.FindAsync(orgId);
        if (org == null)
            return new ApiResponse<PaymentConfigStatusResponse> { Success = false, Message = "Không tìm thấy tổ chức / bệnh viện." };

        var exists = await _context.PaymentConfigs.AnyAsync(pc => pc.OrgId == orgId);
        if (exists)
            return new ApiResponse<PaymentConfigStatusResponse> { Success = false, Message = "Payment config already exists. Use PUT to update." };

        var config = new PaymentConfig
        {
            OrgId = orgId,
            EncryptedClientId = MasterKeyEncryptionService.Encrypt(request.ClientId),
            EncryptedApiKey = MasterKeyEncryptionService.Encrypt(request.ApiKey),
            EncryptedChecksumKey = MasterKeyEncryptionService.Encrypt(request.ChecksumKey),
            IsActive = true
        };

        _context.PaymentConfigs.Add(config);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Configured payment for organization {OrgId}", orgId);

        return new ApiResponse<PaymentConfigStatusResponse>
        {
            Success = true,
            Message = "Payment config created successfully",
            Data = new PaymentConfigStatusResponse
            {
                OrgId = orgId,
                HasPaymentConfig = true,
                IsActive = config.IsActive,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            }
        };
    }

    public async Task<ApiResponse<PaymentConfigStatusResponse>> UpdatePaymentConfigAsync(Guid orgId, ConfigurePaymentRequest request)
    {
        var config = await _context.PaymentConfigs.FirstOrDefaultAsync(pc => pc.OrgId == orgId);
        if (config == null)
            return new ApiResponse<PaymentConfigStatusResponse> { Success = false, Message = "Payment config not found. Use POST to create." };

        config.EncryptedClientId = MasterKeyEncryptionService.Encrypt(request.ClientId);
        config.EncryptedApiKey = MasterKeyEncryptionService.Encrypt(request.ApiKey);
        config.EncryptedChecksumKey = MasterKeyEncryptionService.Encrypt(request.ChecksumKey);
        config.UpdatedAt = VietnamTimeHelper.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated payment config for organization {OrgId}", orgId);

        return new ApiResponse<PaymentConfigStatusResponse>
        {
            Success = true,
            Message = "Payment config updated successfully",
            Data = new PaymentConfigStatusResponse
            {
                OrgId = orgId,
                HasPaymentConfig = true,
                IsActive = config.IsActive,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            }
        };
    }

    private Guid? GetCurrentActorId()
    {
        var claimValue = _httpContextAccessor.HttpContext?.User
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }

    public async Task<ApiResponse<PaymentConfigStatusResponse>> GetPaymentConfigStatusAsync(Guid orgId)
    {
        var config = await _context.PaymentConfigs.FirstOrDefaultAsync(pc => pc.OrgId == orgId);

        return new ApiResponse<PaymentConfigStatusResponse>
        {
            Success = true,
            Data = new PaymentConfigStatusResponse
            {
                OrgId = orgId,
                HasPaymentConfig = config != null,
                IsActive = config?.IsActive ?? false,
                CreatedAt = config?.CreatedAt,
                UpdatedAt = config?.UpdatedAt
            }
        };
    }

    public async Task<ApiResponse<PaymentKeysResponse>> GetPaymentKeysAsync(Guid orgId)
    {
        var config = await _context.PaymentConfigs.FirstOrDefaultAsync(pc => pc.OrgId == orgId && pc.IsActive);
        if (config == null)
            return new ApiResponse<PaymentKeysResponse> { Success = false, Message = "Payment config not found or inactive." };

        return new ApiResponse<PaymentKeysResponse>
        {
            Success = true,
            Data = new PaymentKeysResponse
            {
                ClientId = MasterKeyEncryptionService.Decrypt(config.EncryptedClientId),
                ApiKey = MasterKeyEncryptionService.Decrypt(config.EncryptedApiKey),
                ChecksumKey = MasterKeyEncryptionService.Decrypt(config.EncryptedChecksumKey)
            }
        };
    }

    // Fabric config
    public async Task<ApiResponse<OrganizationResponse>> UpdateOrganizationFabricConfigAsync(Guid orgId, UpdateOrganizationFabricConfigRequest request)
    {
        var org = await _context.Organizations.FindAsync(orgId);
        if (org == null)
        {
            return new ApiResponse<OrganizationResponse>
            {
                Success = false,
                Message = "Không tìm thấy tổ chức / bệnh viện."
            };
        }

        if (request.FabricMspId != null) org.FabricMspId = request.FabricMspId;
        if (request.FabricChannelPeers != null) org.FabricChannelPeers = request.FabricChannelPeers;
        if (request.FabricCaUrl != null) org.FabricCaUrl = request.FabricCaUrl;
        org.UpdatedAt = VietnamTimeHelper.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated fabric config for organization {OrgId}", orgId);

        return new ApiResponse<OrganizationResponse>
        {
            Success = true,
            Message = "Fabric config updated successfully",
            Data = MapToResponse(org)
        };
    }

    // =========================================================================
    // MAPPERS
    // =========================================================================

    private static OrganizationResponse MapToResponse(Models.Entities.Organization org)
    {
        return new OrganizationResponse
        {
            OrgId = org.OrgId,
            OrgDid = org.OrgDid,
            OrgName = org.OrgName,
            OrgCode = org.OrgCode,
            OrgType = org.OrgType,
            LicenseNumber = org.LicenseNumber,
            Address = org.Address,
            ContactInfo = org.ContactInfo,
            Website = org.Website,
            LogoUrl = org.LogoUrl,
            Status = org.Status,
            VerifiedAt = org.VerifiedAt,
            CreatedAt = org.CreatedAt,
            FabricMspId = org.FabricMspId,
            FabricChannelPeers = org.FabricChannelPeers,
            FabricCaUrl = org.FabricCaUrl,
            DepartmentCount = org.Departments?.Count ?? 0,
            MemberCount = org.Memberships?.Count ?? 0
        };
    }

    private static DepartmentResponse MapToResponse(Department dept)
    {
        return new DepartmentResponse
        {
            DepartmentId = dept.DepartmentId,
            OrgId = dept.OrgId,
            OrgName = dept.Organization?.OrgName,
            DepartmentName = dept.DepartmentName,
            DepartmentCode = dept.DepartmentCode,
            Description = dept.Description,
            HeadUserId = dept.HeadUserId,
            ParentDepartmentId = dept.ParentDepartmentId,
            Floor = dept.Floor,
            RoomNumbers = dept.RoomNumbers,
            Status = dept.Status,
            CreatedAt = dept.CreatedAt,
            MemberCount = dept.Memberships?.Count ?? 0
        };
    }

    private static MembershipResponse MapToResponse(Membership membership)
    {
        return new MembershipResponse
        {
            MembershipId = membership.MembershipId,
            User = new MembershipUserResponse
            {
                UserId = membership.UserId
            },
            OrgId = membership.OrgId,
            OrgName = membership.Organization?.OrgName,
            DepartmentId = membership.DepartmentId,
            DepartmentName = membership.Department?.DepartmentName,
            EmployeeId = membership.EmployeeId,
            JobTitle = membership.JobTitle,
            LicenseNumber = membership.LicenseNumber,
            Specialty = membership.Specialty,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            Status = membership.Status,
            CreatedAt = membership.CreatedAt
        };
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        var normalizedString = text.Normalize(System.Text.NormalizationForm.FormD);
        var stringBuilder = new System.Text.StringBuilder(capacity: normalizedString.Length);

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(System.Text.NormalizationForm.FormC).Replace("đ", "d").Replace("Đ", "D");
    }
}

