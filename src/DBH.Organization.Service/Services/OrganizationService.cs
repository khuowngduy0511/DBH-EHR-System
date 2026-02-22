using DBH.Organization.Service.Data;
using DBH.Organization.Service.DTOs;
using DBH.Organization.Service.Models.Entities;
using DBH.Organization.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DBH.Organization.Service.Services;

public class OrganizationService : IOrganizationService
{
    private readonly OrganizationDbContext _context;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(OrganizationDbContext context, ILogger<OrganizationService> logger)
    {
        _context = context;
        _logger = logger;
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
        var org = await _context.Organizations
            .Include(o => o.Departments)
            .Include(o => o.Memberships)
            .FirstOrDefaultAsync(o => o.OrgId == orgId);

        if (org == null)
        {
            return new ApiResponse<OrganizationResponse>
            {
                Success = false,
                Message = "Organization not found"
            };
        }

        return new ApiResponse<OrganizationResponse>
        {
            Success = true,
            Data = MapToResponse(org)
        };
    }

    public async Task<PagedResponse<OrganizationResponse>> GetOrganizationsAsync(int page = 1, int pageSize = 10, string? search = null)
    {
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

        return new PagedResponse<OrganizationResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ApiResponse<OrganizationResponse>> UpdateOrganizationAsync(Guid orgId, UpdateOrganizationRequest request)
    {
        var org = await _context.Organizations.FindAsync(orgId);
        if (org == null)
        {
            return new ApiResponse<OrganizationResponse>
            {
                Success = false,
                Message = "Organization not found"
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
        org.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

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
            return new ApiResponse<bool> { Success = false, Message = "Organization not found" };
        }

        org.Status = OrganizationStatus.INACTIVE;
        org.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

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
                Message = "Organization not found"
            };
        }

        org.Status = OrganizationStatus.ACTIVE;
        org.VerifiedAt = DateTime.UtcNow;
        org.VerifiedBy = verifiedByUserId;
        org.UpdatedAt = DateTime.UtcNow;

        // Generate DID for blockchain
        org.OrgDid = $"did:fabric:org:{org.OrgId:N}";

        await _context.SaveChangesAsync();

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
                Message = "Organization not found"
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

        return new ApiResponse<DepartmentResponse>
        {
            Success = true,
            Data = MapToResponse(dept)
        };
    }

    public async Task<PagedResponse<DepartmentResponse>> GetDepartmentsByOrgAsync(Guid orgId, int page = 1, int pageSize = 10)
    {
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

        return new PagedResponse<DepartmentResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
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
        dept.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

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
        dept.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ApiResponse<bool> { Success = true, Message = "Department deactivated", Data = true };
    }

    // =========================================================================
    // MEMBERSHIPS
    // =========================================================================

    public async Task<ApiResponse<MembershipResponse>> CreateMembershipAsync(CreateMembershipRequest request)
    {
        var orgExists = await _context.Organizations.AnyAsync(o => o.OrgId == request.OrgId);
        if (!orgExists)
        {
            return new ApiResponse<MembershipResponse>
            {
                Success = false,
                Message = "Organization not found"
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
            Notes = request.Notes
        };

        _context.Memberships.Add(membership);
        await _context.SaveChangesAsync();

        return new ApiResponse<MembershipResponse>
        {
            Success = true,
            Message = "Membership created successfully",
            Data = MapToResponse(membership)
        };
    }

    public async Task<ApiResponse<MembershipResponse>> GetMembershipByIdAsync(Guid membershipId)
    {
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

        return new ApiResponse<MembershipResponse>
        {
            Success = true,
            Data = MapToResponse(membership)
        };
    }

    public async Task<PagedResponse<MembershipResponse>> GetMembershipsByOrgAsync(Guid orgId, int page = 1, int pageSize = 10)
    {
        var query = _context.Memberships
            .Include(m => m.Organization)
            .Include(m => m.Department)
            .Where(m => m.OrgId == orgId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<MembershipResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResponse<MembershipResponse>> GetMembershipsByUserAsync(Guid userId, int page = 1, int pageSize = 10)
    {
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

        return new PagedResponse<MembershipResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ApiResponse<MembershipResponse>> UpdateMembershipAsync(Guid membershipId, UpdateMembershipRequest request)
    {
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
        membership.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ApiResponse<MembershipResponse>
        {
            Success = true,
            Message = "Membership updated successfully",
            Data = MapToResponse(membership)
        };
    }

    public async Task<ApiResponse<bool>> DeleteMembershipAsync(Guid membershipId)
    {
        var membership = await _context.Memberships.FindAsync(membershipId);
        if (membership == null)
        {
            return new ApiResponse<bool> { Success = false, Message = "Membership not found" };
        }

        membership.Status = MembershipStatus.TERMINATED;
        membership.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        membership.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ApiResponse<bool> { Success = true, Message = "Membership terminated", Data = true };
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
            UserId = membership.UserId,
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
}
