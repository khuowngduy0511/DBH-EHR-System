using DBH.Auth.Service.DTOs;
using DBH.Auth.Service.Models.Entities;
using DBH.Auth.Service.Models.Enums;
using DBH.Auth.Service.Repositories;
using DBH.Auth.Service.Services;
using DBH.Shared.Infrastructure.Caching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Auth.Service.Controllers;

[ApiController]
[Route("api/v1/patients")]
public class PatientsController : ControllerBase
{
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGenericRepository<Role> _roleRepository;
    private readonly IGenericRepository<UserRole> _userRoleRepository;
    private readonly IAuthService _authService;
    private readonly ICacheService _cacheService;

    public PatientsController(
        IGenericRepository<Patient> patientRepository,
        IUserRepository userRepository,
        IGenericRepository<Role> roleRepository,
        IGenericRepository<UserRole> userRoleRepository,
        IAuthService authService,
        ICacheService cacheService)
    {
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _authService = authService;
        _cacheService = cacheService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetAllUsersQuery query)
    {
        query.Role = RoleName.Patient.ToString();
        var result = await _authService.GetAllUsersAsync(query, User.IsInRole("Admin"));
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{patientId:guid}")]
    public async Task<IActionResult> GetById(Guid patientId)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null)
        {
            return NotFound("Patient not found.");
        }

        var user = await _userRepository.GetByIdAsync(patient.UserId);
        return Ok(MapToResponse(patient, user));
    }

    [Authorize(Roles = "Admin,Receptionist")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return BadRequest("User not found.");
        }

        if (await _patientRepository.ExistsAsync(p => p.UserId == request.UserId))
        {
            return BadRequest("This user already has a patient profile.");
        }

        var patient = new Patient
        {
            UserId = request.UserId,
            Dob = request.Dob,
            BloodType = request.BloodType
        };

        await _patientRepository.AddAsync(patient);
        await EnsureUserRoleAsync(request.UserId, RoleName.Patient);

        return CreatedAtAction(nameof(GetById), new { patientId = patient.PatientId }, MapToResponse(patient));
    }

    [Authorize(Roles = "Admin,Receptionist")]
    [HttpPut("{patientId:guid}")]
    public async Task<IActionResult> Update(Guid patientId, [FromBody] UpdatePatientRequest request)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null)
        {
            return NotFound("Patient not found.");
        }

        patient.Dob = request.Dob;
        patient.BloodType = request.BloodType;

        await _patientRepository.UpdateAsync(patient);
        await _cacheService.RemoveAsync($"profile:{patient.UserId}");
        return Ok(MapToResponse(patient));
    }

    [Authorize(Roles = "Admin,Receptionist")]
    [HttpDelete("{patientId:guid}")]
    public async Task<IActionResult> Delete(Guid patientId)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null)
        {
            return NotFound("Patient not found.");
        }

        await _patientRepository.DeleteAsync(patient);
        return Ok("Patient deleted successfully.");
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

    private static PatientResponse MapToResponse(Patient patient, User? user = null)
    {
        return new PatientResponse
        {
            PatientId = patient.PatientId,
            UserId = patient.UserId,
            Dob = patient.Dob,
            BloodType = patient.BloodType,
            FullName = user?.FullName,
            Email = user?.Email,
            Phone = user?.Phone,
            DateOfBirth = user?.DateOfBirth,
            Gender = user?.Gender
        };
    }
}
