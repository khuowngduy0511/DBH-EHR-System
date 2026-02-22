using DBH.Organization.Service.DTOs;
using DBH.Organization.Service.Models.Entities;

namespace DBH.Organization.Service.Services;

public interface IOrganizationService
{
    // Organizations
    Task<ApiResponse<OrganizationResponse>> CreateOrganizationAsync(CreateOrganizationRequest request);
    Task<ApiResponse<OrganizationResponse>> GetOrganizationByIdAsync(Guid orgId);
    Task<PagedResponse<OrganizationResponse>> GetOrganizationsAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<ApiResponse<OrganizationResponse>> UpdateOrganizationAsync(Guid orgId, UpdateOrganizationRequest request);
    Task<ApiResponse<bool>> DeleteOrganizationAsync(Guid orgId);
    Task<ApiResponse<OrganizationResponse>> VerifyOrganizationAsync(Guid orgId, Guid verifiedByUserId);

    // Departments
    Task<ApiResponse<DepartmentResponse>> CreateDepartmentAsync(CreateDepartmentRequest request);
    Task<ApiResponse<DepartmentResponse>> GetDepartmentByIdAsync(Guid departmentId);
    Task<PagedResponse<DepartmentResponse>> GetDepartmentsByOrgAsync(Guid orgId, int page = 1, int pageSize = 10);
    Task<ApiResponse<DepartmentResponse>> UpdateDepartmentAsync(Guid departmentId, UpdateDepartmentRequest request);
    Task<ApiResponse<bool>> DeleteDepartmentAsync(Guid departmentId);

    // Memberships
    Task<ApiResponse<MembershipResponse>> CreateMembershipAsync(CreateMembershipRequest request);
    Task<ApiResponse<MembershipResponse>> GetMembershipByIdAsync(Guid membershipId);
    Task<PagedResponse<MembershipResponse>> GetMembershipsByOrgAsync(Guid orgId, int page = 1, int pageSize = 10);
    Task<PagedResponse<MembershipResponse>> GetMembershipsByUserAsync(Guid userId, int page = 1, int pageSize = 10);
    Task<ApiResponse<MembershipResponse>> UpdateMembershipAsync(Guid membershipId, UpdateMembershipRequest request);
    Task<ApiResponse<bool>> DeleteMembershipAsync(Guid membershipId);
}
