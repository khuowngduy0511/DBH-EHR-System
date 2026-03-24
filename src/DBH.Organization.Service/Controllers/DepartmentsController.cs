using DBH.Organization.Service.DTOs;
using DBH.Organization.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Organization.Service.Controllers;

[ApiController]
[Route("api/v1/departments")]
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
