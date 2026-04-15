using DBH.Auth.Service.DTOs;
using DBH.Auth.Service.Models.Entities;
using DBH.Auth.Service.Models.Enums;
using DBH.Auth.Service.Repositories;
using DBH.Auth.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DBH.Auth.Service.Controllers;

[ApiController]

[Route("api/v1/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly IGenericRepository<Doctor> _doctorRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGenericRepository<Role> _roleRepository;
    private readonly IGenericRepository<UserRole> _userRoleRepository;
    private readonly IAuthService _authService;

    public DoctorsController(
        IGenericRepository<Doctor> doctorRepository,
        IUserRepository userRepository,
        IGenericRepository<Role> roleRepository,
        IGenericRepository<UserRole> userRoleRepository,
        IAuthService authService)
    {
        _doctorRepository = doctorRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _authService = authService;
    }


    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetAllUsersQuery query)
    {
        query.Role = RoleName.Doctor.ToString();
        var result = await _authService.GetAllUsersAsync(query, User.IsInRole("Admin"));
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet("organization/me")]
    public async Task<IActionResult> GetAllByMyOrganization([FromQuery] string? orgId = null)
    {
        var organizationId = User.FindFirstValue(ClaimTypes.GroupSid);
        if (string.IsNullOrWhiteSpace(organizationId) && !string.IsNullOrWhiteSpace(orgId))
        {
            organizationId = orgId;
        }

        if (string.IsNullOrWhiteSpace(organizationId))
        {
            return Unauthorized("Organization claim is missing in token.");
        }

        var doctors = await _userRepository.GetDoctorsByOrganizationAsync(organizationId);
        return Ok(doctors.Select(MapToBasicInfoResponse));
    }

    [Authorize]
    [HttpGet("organization/me/{userId:guid}")]
    public async Task<IActionResult> GetDoctorByUserIdInMyOrganization(Guid userId, [FromQuery] string? orgId = null)
    {
        var organizationId = !string.IsNullOrWhiteSpace(orgId)
            ? orgId
            : User.FindFirstValue(ClaimTypes.GroupSid);

        if (string.IsNullOrWhiteSpace(organizationId))
        {
            return Unauthorized("Organization claim is missing in token.");
        }

        var doctorUser = await _userRepository.GetByIdAsync(userId);
        if (doctorUser == null)
        {
            return NotFound("User not found.");
        }

        // if (!string.Equals(doctorUser.OrganizationId, organizationId, StringComparison.OrdinalIgnoreCase))
        // {
        //     return NotFound("Doctor not found in your organization.");
        // }

        return Ok(MapToBasicInfoResponse(doctorUser));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{doctorId:guid}")]
    public async Task<IActionResult> GetById(Guid doctorId)
    {
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        if (doctor == null)
        {
            return NotFound("Doctor not found.");
        }

        return Ok(MapToResponse(doctor));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateDoctorRequest request)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return BadRequest("User not found.");
        }

        if (await _doctorRepository.ExistsAsync(d => d.UserId == request.UserId))
        {
            return BadRequest("This user already has a doctor profile.");
        }

        var doctor = new Doctor
        {
            UserId = request.UserId,
            Specialty = request.Specialty,
            LicenseNumber = request.LicenseNumber,
            LicenseImage = request.LicenseImage,
            VerifiedStatus = request.VerifiedStatus
        };

        await _doctorRepository.AddAsync(doctor);
        await EnsureUserRoleAsync(request.UserId, RoleName.Doctor);

        return CreatedAtAction(nameof(GetById), new { doctorId = doctor.DoctorId }, MapToResponse(doctor));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{doctorId:guid}")]
    public async Task<IActionResult> Update(Guid doctorId, [FromBody] UpdateDoctorRequest request)
    {
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        if (doctor == null)
        {
            return NotFound("Doctor not found.");
        }

        doctor.Specialty = request.Specialty;
        doctor.LicenseNumber = request.LicenseNumber;
        doctor.LicenseImage = request.LicenseImage;
        doctor.VerifiedStatus = request.VerifiedStatus;

        await _doctorRepository.UpdateAsync(doctor);
        return Ok(MapToResponse(doctor));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{doctorId:guid}")]
    public async Task<IActionResult> Delete(Guid doctorId)
    {
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        if (doctor == null)
        {
            return NotFound("Doctor not found.");
        }

        await _doctorRepository.DeleteAsync(doctor);
        return Ok("Doctor deleted successfully.");
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
            await _userRoleRepository.DeleteAsync(userRole);
            await _userRoleRepository.AddAsync(new UserRole
            {
                UserId = userId,
                RoleId = role.RoleId
            });
        }
    }

    private static DoctorResponse MapToResponse(Doctor doctor)
    {
        return new DoctorResponse
        {
            DoctorId = doctor.DoctorId,
            UserId = doctor.UserId,
            Specialty = doctor.Specialty,
            LicenseNumber = doctor.LicenseNumber,
            LicenseImage = doctor.LicenseImage,
            VerifiedStatus = doctor.VerifiedStatus
        };
    }

    private static DoctorBasicInfoResponse MapToBasicInfoResponse(User user)
    {
        return new DoctorBasicInfoResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Gender = user.Gender,
            Email = user.Email,
            Phone = user.Phone,
            DateOfBirth = user.DateOfBirth,
            Address = user.Address,
            OrganizationId = user.OrganizationId,
            Status = user.Status
        };
    }
}
