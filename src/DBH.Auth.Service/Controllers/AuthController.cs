
using DBH.Auth.Service.Services;
using DBH.Auth.Service.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Auth.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }



    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var response = await _authService.LoginAsync(request, ipAddress);
        if (!response.Success)
        {
            return Unauthorized(response);
        }
        return Ok(response);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier); 
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
             return Unauthorized();
        }

        var result = await _authService.RevokeTokenAsync(userId);
        if (!result) return BadRequest("Could not revoke token.");
        return Ok("Token revoked.");
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        Console.WriteLine($"IsAuthenticated: {User.Identity?.IsAuthenticated}");
        foreach (var claim in User.Claims)
        {
            Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
        }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
             return Unauthorized();
        }

        var profile = await _authService.GetMyProfileAsync(userId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }
}

