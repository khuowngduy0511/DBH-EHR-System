using DBH.Organization.Service.DTOs;
using DBH.Organization.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Organization.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    // =========================================================================
    // ORGANIZATION ENDPOINTS
    // =========================================================================

    /// <summary>
    /// Create a new organization (hospital, clinic, etc.)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        var result = await _organizationService.CreateOrganizationAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetOrganization), new { id = result.Data!.OrgId }, result);
    }

    /// <summary>
    /// Get organization by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetOrganization(Guid id)
    {
        var result = await _organizationService.GetOrganizationByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Get all organizations with pagination and search
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetOrganizations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _organizationService.GetOrganizationsAsync(page, pageSize, search);
        return Ok(result);
    }

    /// <summary>
    /// Update organization
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,SystemAdmin,OrgAdmin")]
    public async Task<IActionResult> UpdateOrganization(Guid id, [FromBody] UpdateOrganizationRequest request)
    {
        var result = await _organizationService.UpdateOrganizationAsync(id, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Deactivate organization (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> DeleteOrganization(Guid id)
    {
        var result = await _organizationService.DeleteOrganizationAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Verify organization (admin approval)
    /// </summary>
    [HttpPost("{id:guid}/verify")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<IActionResult> VerifyOrganization(Guid id, [FromQuery] Guid verifiedByUserId)
    {
        var result = await _organizationService.VerifyOrganizationAsync(id, verifiedByUserId);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public DepartmentsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    // =========================================================================
    // DEPARTMENT ENDPOINTS
    // =========================================================================

    /// <summary>
    /// Create a new department within an organization
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,OrgAdmin")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
    {
        var result = await _organizationService.CreateDepartmentAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetDepartment), new { id = result.Data!.DepartmentId }, result);
    }

    /// <summary>
    /// Get department by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetDepartment(Guid id)
    {
        var result = await _organizationService.GetDepartmentByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Get all departments for an organization
    /// </summary>
    [HttpGet("by-organization/{orgId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetDepartmentsByOrg(
        Guid orgId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _organizationService.GetDepartmentsByOrgAsync(orgId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Update department
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,OrgAdmin,DepartmentHead")]
    public async Task<IActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        var result = await _organizationService.UpdateDepartmentAsync(id, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Deactivate department (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,OrgAdmin")]
    public async Task<IActionResult> DeleteDepartment(Guid id)
    {
        var result = await _organizationService.DeleteDepartmentAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
public class MembershipsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public MembershipsController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    // =========================================================================
    // MEMBERSHIP ENDPOINTS
    // =========================================================================

    /// <summary>
    /// Add user to organization (create membership)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,OrgAdmin,HR")]
    public async Task<IActionResult> CreateMembership([FromBody] CreateMembershipRequest request)
    {
        var result = await _organizationService.CreateMembershipAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetMembership), new { id = result.Data!.MembershipId }, result);
    }

    /// <summary>
    /// Get membership by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetMembership(Guid id)
    {
        var result = await _organizationService.GetMembershipByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Get all memberships for an organization
    /// </summary>
    [HttpGet("by-organization/{orgId:guid}")]
    [Authorize(Roles = "Admin,OrgAdmin,HR")]
    public async Task<IActionResult> GetMembershipsByOrg(
        Guid orgId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _organizationService.GetMembershipsByOrgAsync(orgId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get all memberships for a user
    /// </summary>
    [HttpGet("by-user/{userId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetMembershipsByUser(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _organizationService.GetMembershipsByUserAsync(userId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Update membership details
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,OrgAdmin,HR")]
    public async Task<IActionResult> UpdateMembership(Guid id, [FromBody] UpdateMembershipRequest request)
    {
        var result = await _organizationService.UpdateMembershipAsync(id, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Terminate membership (remove user from organization)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,OrgAdmin,HR")]
    public async Task<IActionResult> DeleteMembership(Guid id)
    {
        var result = await _organizationService.DeleteMembershipAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
