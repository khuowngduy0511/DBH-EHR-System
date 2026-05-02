
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
    /// Registers a new user and creates an associated patient profile.    
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("register")]
    // [Authorize(Roles = "Admin, Receptionist")]
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
    /// Registers a new doctor with the full doctor profile payload. Only users with Admin role can access this endpoint.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin,Receptionist")]
    [HttpPost("register-doctor")]
    public async Task<IActionResult> RegisterDoctor([FromBody] RegisterDoctorRequest request)
    {
        var response = await _authService.RegisterDoctorAsync(request);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    /// <summary>
    /// Registers a new staff account with the full staff profile payload. Only users with Admin role can access this endpoint.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin,Receptionist")]
    [HttpPost("register-staff")]
    public async Task<IActionResult> RegisterStaff([FromBody] RegisterStaffRequest request)
    {
        var response = await _authService.RegisterStaffAsync(request);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    /// <summary>
    /// Registers a new user and creates an associated profile for either Staff or Doctor based on the role specified in the request.
    /// Kept for backward compatibility.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [Authorize(Roles = "Admin,Receptionist")]
    [HttpPost("registerStaffDoctor")]
    public async Task<IActionResult> RegisterStaffDoctor([FromBody] RegisterStaffDoctorRequest request)
    {
        var response = await _authService.RegisterStaffDoctorAsync(request);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("updateRole")]
    public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleRequest request)
    {
        var response = await _authService.UpdateRoleAsync(request);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{userId:guid}")]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] AdminUpdateUserRequest request)
    {
        var response = await _authService.UpdateUserAsync(userId, request);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [Authorize]
    [HttpPut("me/change-password")]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var response = await _authService.ChangePasswordAsync(userId, request);
        if (!response.Success)
            return BadRequest(response);
        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{userId:guid}/change-password")]
    public async Task<IActionResult> AdminChangeUserPassword(Guid userId, [FromBody] AdminChangePasswordRequest request)
    {
        var response = await _authService.AdminChangePasswordAsync(userId, request);
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

    [Authorize]
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
        if (profile == null) return NotFound(Failed("Profile not found."));
        return Ok(profile);
    }

    [Authorize]
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
             return Unauthorized();
        }

        var response = await _authService.UpdateProfileAsync(userId, request);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{userId:guid}/profile")]
    public async Task<IActionResult> UpdateUserProfileByAdmin(Guid userId, [FromBody] UpdateProfileRequest request)
    {
        var response = await _authService.UpdateProfileAsync(userId, request);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{userId:guid}/status")]
    public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromQuery] string status)
    {
        var response = await _authService.UpdateUserStatusAsync(userId, status);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [Authorize(Roles = "Admin, Doctor, Patient, Nurse, Receptionist, Pharmacist, LabTech")]
    [HttpGet("users/{userId:guid}")]
    public async Task<IActionResult> GetUserProfile(Guid userId)
    {
        var profile = await _authService.GetMyProfileAsync(userId);
        if (profile == null) return NotFound(Failed("Profile not found."));
        return Ok(profile);
    }

    [Authorize]
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] GetAllUsersQuery query)
    {
        var result = await _authService.GetAllUsersAsync(query, User.IsInRole("Admin"));
        if (!result.Success)
        {
            if (string.Equals(result.Message, "Only admin can search admin users.", StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return BadRequest(result);
        }

        return Ok(result);
    }


    [Authorize]
    [HttpGet("users/by-contact")]
    public async Task<IActionResult> GetUserProfileByContact([FromQuery] string? email, [FromQuery] string? phone)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
        {
            return BadRequest("At least one of email or phone is required.");
        }

        var profile = await _authService.GetProfileByContactAsync(email, phone);
        if (profile == null) return NotFound(Failed("Profile not found."));
        return Ok(profile);
    }

    [Authorize]
    [HttpGet("user-id")]
    public async Task<IActionResult> GetUserId(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? staffId)
    {
        if (!patientId.HasValue && !doctorId.HasValue && !staffId.HasValue)
        {
            return BadRequest("Either patientId, doctorId, or staffId is required.");
        }

        var provided = new[] { patientId.HasValue, doctorId.HasValue, staffId.HasValue }.Count(x => x);
        if (provided > 1)
        {
            return BadRequest("Provide only one of patientId, doctorId, or staffId.");
        }

        var userId = await _authService.GetUserIdByProfileIdAsync(patientId, doctorId, staffId);
        if (!userId.HasValue)
        {
            return NotFound(Failed("User not found."));
        }

        return Ok(new { UserId = userId.Value });
    }

    /// <summary>
    /// Deactivate (soft-delete) a user account. Clears personal data, sets status to Inactive.
    /// User can re-register with the same email later.
    /// </summary>
    [Authorize]
    [HttpDelete("users/{userId:guid}")]
    public async Task<IActionResult> DeactivateAccount(Guid userId)
    {
        // Only admins or the user themselves can deactivate
        var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (currentUserIdClaim == null || !Guid.TryParse(currentUserIdClaim.Value, out var currentUserId))
        {
            return Unauthorized();
        }

        if (currentUserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var response = await _authService.DeactivateAccountAsync(userId);
        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [Authorize]
    [HttpGet("{userId:guid}/keys")]
    public async Task<IActionResult> GetUserKeys(Guid userId)
    {
        var keys = await _authService.GetUserKeysAsync(userId);
        if (keys == null) return NotFound(Failed("User keys not found. User might not have been initialized properly."));
        return Ok(keys);
    }

    private static AuthResponse Failed(string message)
    {
        return new AuthResponse
        {
            Success = false,
            Message = message
        };
    }
}

