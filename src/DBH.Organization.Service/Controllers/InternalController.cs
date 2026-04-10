using System.Security.Cryptography;
using DBH.Organization.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Organization.Service.Controllers;

[ApiController]
[Route("api/v1/internal/organizations")]
public class InternalController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly IConfiguration _configuration;

    public InternalController(IOrganizationService organizationService, IConfiguration configuration)
    {
        _organizationService = organizationService;
        _configuration = configuration;
    }

    /// <summary>
    /// Internal endpoint — service-to-service only.
    /// Protected by X-Internal-Api-Key header (shared secret).
    /// </summary>
    [HttpGet("{orgId}/payment-keys")]
    public async Task<IActionResult> GetPaymentKeys(Guid orgId)
    {
        var expectedKey = _configuration["InternalApi:SecretKey"];
        if (string.IsNullOrEmpty(expectedKey))
            return StatusCode(503, new { Success = false, Message = "Internal API not configured." });

        if (!Request.Headers.TryGetValue("X-Internal-Api-Key", out var providedKey)
            || !CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(providedKey.ToString()),
                System.Text.Encoding.UTF8.GetBytes(expectedKey)))
        {
            return Unauthorized(new { Success = false, Message = "Unauthorized internal request." });
        }

        var result = await _organizationService.GetPaymentKeysAsync(orgId);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
