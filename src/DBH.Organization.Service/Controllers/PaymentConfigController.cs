using DBH.Organization.Service.DTOs;
using DBH.Organization.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Organization.Service.Controllers;

[ApiController]
[Route("api/v1/organizations/{orgId}/payment-config")]
[Authorize(Roles = "Admin,SystemAdmin,OrgAdmin")]
public class PaymentConfigController : ControllerBase
{
    private readonly IOrganizationService _organizationService;

    public PaymentConfigController(IOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    [HttpPost]
    public async Task<IActionResult> ConfigurePayment(Guid orgId, [FromBody] ConfigurePaymentRequest request)
    {
        var result = await _organizationService.ConfigurePaymentAsync(orgId, request);
        if (!result.Success)
            return BadRequest(result);

        return Created($"/api/v1/organizations/{orgId}/payment-config", result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdatePaymentConfig(Guid orgId, [FromBody] ConfigurePaymentRequest request)
    {
        var result = await _organizationService.UpdatePaymentConfigAsync(orgId, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentConfigStatus(Guid orgId)
    {
        var result = await _organizationService.GetPaymentConfigStatusAsync(orgId);
        return Ok(result);
    }
}
