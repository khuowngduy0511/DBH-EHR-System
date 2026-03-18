using DBH.Auth.Service.DTOs;
using DBH.Auth.Service.Models.Entities;
using DBH.Auth.Service.Models.Enums;
using DBH.Auth.Service.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Auth.Service.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/staff")]
public class StaffController : ControllerBase
{
    private readonly IGenericRepository<Staff> _staffRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGenericRepository<Role> _roleRepository;
    private readonly IGenericRepository<UserRole> _userRoleRepository;

    public StaffController(
        IGenericRepository<Staff> staffRepository,
        IUserRepository userRepository,
        IGenericRepository<Role> roleRepository,
        IGenericRepository<UserRole> userRoleRepository)
    {
        _staffRepository = staffRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var staff = await _staffRepository.GetAllAsync();
        return Ok(staff.Select(MapToResponse));
    }

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
        await EnsureUserRoleAsync(request.UserId, MapToRoleName(request.Role));

        return CreatedAtAction(nameof(GetById), new { staffId = staff.StaffId }, MapToResponse(staff));
    }

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
        await EnsureUserRoleAsync(staff.UserId, MapToRoleName(request.Role));

        return Ok(MapToResponse(staff));
    }

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

    private async Task EnsureUserRoleAsync(Guid userId, RoleName roleName)
    {
        var role = await _roleRepository.FindAsync(r => r.RoleName == roleName);
        if (role == null)
        {
            return;
        }

        var userRole = await _userRoleRepository.FindAsync(ur => ur.UserId == userId);
        if (userRole == null)
        {
            await _userRoleRepository.AddAsync(new UserRole
            {
                UserId = userId,
                RoleId = role.RoleId
            });
            return;
        }

        if (userRole.RoleId != role.RoleId)
        {
            userRole.RoleId = role.RoleId;
            await _userRoleRepository.UpdateAsync(userRole);
        }
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
