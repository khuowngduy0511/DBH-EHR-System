using DBH.Organization.Service.DTOs;
using DBH.Organization.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Organization.Service.Controllers;

[ApiController]
[Route("api/v1/memberships")]
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
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetMembershipsByOrg(
        Guid orgId,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _organizationService.GetMembershipsByOrgAsync(orgId, search, page, pageSize);
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
    /// Search doctors by organization and optional department (request body)
    /// </summary>
    [HttpPost("doctors/search")]
    [Authorize]
    public async Task<IActionResult> SearchDoctors([FromBody] SearchDoctorsRequest request)
    {
        var result = await _organizationService.SearchDoctorsAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Update membership details
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMembership(Guid id)
    {
        var result = await _organizationService.DeleteMembershipAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
