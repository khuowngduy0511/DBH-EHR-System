using DBH.Auth.Service.DTOs;
using DBH.Auth.Service.Models.Entities;
using DBH.Auth.Service.Models.Enums;
using DBH.Auth.Service.Repositories;
using DBH.Auth.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Auth.Service.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/staff")]
public class StaffController : ControllerBase
{
    private readonly IGenericRepository<Staff> _staffRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGenericRepository<Role> _roleRepository;
    private readonly IGenericRepository<UserRole> _userRoleRepository;
    private readonly IAuthService _authService;

    public StaffController(
        IGenericRepository<Staff> staffRepository,
        IUserRepository userRepository,
        IGenericRepository<Role> roleRepository,
        IGenericRepository<UserRole> userRoleRepository,
        IAuthService authService)
    {
        _staffRepository = staffRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _authService = authService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetAllUsersQuery query)
    {
        query.Role = "Staff";
        var result = await _authService.GetAllUsersAsync(query, User.IsInRole("Admin"));
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{staffId:guid}")]
    public async Task<IActionResult> GetById(Guid staffId)
    {
        var staff = await _staffRepository.GetByIdAsync(staffId);
        if (staff == null)
        {
            return NotFound("Staff profile not found.");
        }

        return Ok(MapToResponse(staff));
    }

    [Authorize(Roles = "Admin,Receptionist")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return BadRequest("User not found.");
        }

        if (await _staffRepository.ExistsAsync(s => s.UserId == request.UserId))
        {
            return BadRequest("This user already has a staff profile.");
        }

        var staff = new Staff
        {
            UserId = request.UserId,
            Role = request.Role,
            LicenseNumber = request.LicenseNumber,
            Specialty = request.Specialty,
            VerifiedStatus = request.VerifiedStatus
        };

        await _staffRepository.AddAsync(staff);
        await _authService.UpdateRoleAsync(new UpdateRoleRequest { UserId = request.UserId, NewRole = MapToRoleName(request.Role).ToString() });

        return CreatedAtAction(nameof(GetById), new { staffId = staff.StaffId }, MapToResponse(staff));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{staffId:guid}/verify")]
    public async Task<IActionResult> Verify(Guid staffId)
    {
        var response = await _authService.VerifyStaffAsync(staffId);
        if (!response.Success)
        {
            return NotFound(response);
        }

        var staff = await _staffRepository.GetByIdAsync(staffId);
        return Ok(new
        {
            response.Message,
            StaffId = staffId,
            VerifiedStatus = staff?.VerifiedStatus.ToString() ?? VerificationStatus.Verified.ToString()
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{staffId:guid}")]
    public async Task<IActionResult> Update(Guid staffId, [FromBody] UpdateStaffRequest request)
    {
        var staff = await _staffRepository.GetByIdAsync(staffId);
        if (staff == null)
        {
            return NotFound("Staff profile not found.");
        }

        staff.Role = request.Role;
        staff.LicenseNumber = request.LicenseNumber;
        staff.Specialty = request.Specialty;
        staff.VerifiedStatus = request.VerifiedStatus;

        await _staffRepository.UpdateAsync(staff);
        await _authService.UpdateRoleAsync(new UpdateRoleRequest { UserId = staff.UserId, NewRole = MapToRoleName(request.Role).ToString() });

        return Ok(MapToResponse(staff));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{staffId:guid}")]
    public async Task<IActionResult> Delete(Guid staffId)
    {
        var staff = await _staffRepository.GetByIdAsync(staffId);
        if (staff == null)
        {
            return NotFound("Staff profile not found.");
        }

        await _staffRepository.DeleteAsync(staff);
        return Ok("Staff profile deleted successfully.");
    }



    private static RoleName MapToRoleName(StaffRole staffRole)
    {
        return staffRole switch
        {
            StaffRole.Nurse => RoleName.Nurse,
            StaffRole.Pharmacist => RoleName.Pharmacist,
            StaffRole.Receptionist => RoleName.Receptionist,
            StaffRole.LabTech => RoleName.LabTech,
            _ => throw new InvalidOperationException($"Unsupported staff role '{staffRole}'.")
        };
    }

    private static StaffResponse MapToResponse(Staff staff)
    {
        return new StaffResponse
        {
            StaffId = staff.StaffId,
            UserId = staff.UserId,
            Role = staff.Role,
            LicenseNumber = staff.LicenseNumber,
            Specialty = staff.Specialty,
            VerifiedStatus = staff.VerifiedStatus
        };
    }
}
