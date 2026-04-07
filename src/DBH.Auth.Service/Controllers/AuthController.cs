
using DBH.Auth.Service.Services;
using DBH.Auth.Service.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Auth.Service.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers a new user and creates an associated profile Patient    
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("register")]
    // [Authorize(Roles = "Admin, Receptionist")]
    // Cái organizationId sẽ được lấy từ tài khoản của staff
    // Một là FE tự truyền xuong, hai là BE lấy từ claim của staff đang đăng nhập
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _authService.RegisterAsync(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return Ok(response);
    }

    /// <summary>    
    /// Registers a new user and creates an associated profile for either Staff or Doctor based on the role specified in the request. Only users with Admin role can access this endpoint.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin")]
    [HttpPost("registerStaffDoctor")]
    public async Task<IActionResult> RegisterStaffDoctor([FromBody] RegisterStaffDoctorRequest request)
    {
        var response = await _authService.RegisterStaffDoctorAsync(request);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [HttpPut("updateRole")]
    public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleRequest request)
    {
        var response = await _authService.UpdateRoleAsync(request);
        if (!response.Success)
            return BadRequest(response);
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

    /// <summary>
    /// Gets the profile of the currently authenticated user.
    /// </summary>
    /// <returns></returns>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        // Console.WriteLine($"IsAuthenticated: {User.Identity?.IsAuthenticated}");
        // foreach (var claim in User.Claims)
        // {
        //     Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
        // }

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
             return Unauthorized();
        }

        var profile = await _authService.GetMyProfileAsync(userId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [Authorize]
    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUserProfile(Guid userId)
    {
        var profile = await _authService.GetMyProfileAsync(userId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [Authorize]
    [HttpGet("user-id")]
    public async Task<IActionResult> GetUserId([FromQuery] Guid? patientId, [FromQuery] Guid? doctorId)
    {
        if (!patientId.HasValue && !doctorId.HasValue)
        {
            return BadRequest("Either patientId or doctorId is required.");
        }

        if (patientId.HasValue && doctorId.HasValue)
        {
            return BadRequest("Provide only one of patientId or doctorId.");
        }

        var userId = await _authService.GetUserIdByProfileIdAsync(patientId, doctorId);
        if (!userId.HasValue)
        {
            return NotFound();
        }

        return Ok(new { UserId = userId.Value });
    }

    [HttpGet("{userId:guid}/keys")]
    public async Task<IActionResult> GetUserKeys(Guid userId)
    {
        var keys = await _authService.GetUserKeysAsync(userId);
        if (keys == null) return NotFound("User keys not found. User might not have been initialized properly.");
        return Ok(keys);
    }
}

