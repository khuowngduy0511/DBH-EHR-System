using DBH.Organization.Service.DTOs;
using DBH.Organization.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Organization.Service.Controllers;

[ApiController]
[Route("api/v1/organizations")]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> VerifyOrganization(Guid id, [FromQuery] Guid verifiedByUserId)
    {
        var result = await _organizationService.VerifyOrganizationAsync(id, verifiedByUserId);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
