using DBH.Organization.Service.DTOs;
using DBH.Organization.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Organization.Service.Controllers;

[ApiController]
[Route("api/v1/organizations/{orgId:guid}/fabric-config")]
[Authorize(Roles = "Admin")]
public class OrganizationFabricConfigController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public OrganizationFabricConfigController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    [HttpPut]
    public async Task<IActionResult> UpdateFabricConfig(
        Guid orgId,
        [FromBody] UpdateOrganizationFabricConfigRequest request)
    {
        var result = await _organizationService.UpdateOrganizationFabricConfigAsync(orgId, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
