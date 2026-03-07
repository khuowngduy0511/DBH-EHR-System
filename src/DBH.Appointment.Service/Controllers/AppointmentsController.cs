using DBH.Appointment.Service.DTOs;
using DBH.Appointment.Service.Models.Enums;
using DBH.Appointment.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Appointment.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    // =========================================================================
    // APPOINTMENT ENDPOINTS
    // =========================================================================

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        var result = await _appointmentService.CreateAppointmentAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetAppointment), new { id = result.Data!.AppointmentId }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetAppointment(Guid id)
    {
        var result = await _appointmentService.GetAppointmentByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? orgId,
        [FromQuery] AppointmentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _appointmentService.GetAppointmentsAsync(patientId, doctorId, orgId, status, page, pageSize);
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateAppointmentStatus(Guid id, [FromQuery] AppointmentStatus status)
    {
        var result = await _appointmentService.UpdateAppointmentStatusAsync(id, status);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPut("{id:guid}/reschedule")]
    [Authorize]
    public async Task<IActionResult> RescheduleAppointment(Guid id, [FromQuery] DateTime newDate)
    {
        var result = await _appointmentService.RescheduleAppointmentAsync(id, newDate);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
public class EncountersController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public EncountersController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    // =========================================================================
    // ENCOUNTER ENDPOINTS
    // =========================================================================

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> CreateEncounter([FromBody] CreateEncounterRequest request)
    {
        var result = await _appointmentService.CreateEncounterAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetEncounter), new { id = result.Data!.EncounterId }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetEncounter(Guid id)
    {
        var result = await _appointmentService.GetEncounterByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("by-appointment/{appointmentId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetEncountersByAppointmentId(Guid appointmentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _appointmentService.GetEncountersByAppointmentIdAsync(appointmentId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("by-patient/{patientId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetEncountersByPatientId(Guid patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _appointmentService.GetEncountersByPatientIdAsync(patientId, page, pageSize);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateEncounter(Guid id, [FromBody] UpdateEncounterRequest request)
    {
        var result = await _appointmentService.UpdateEncounterAsync(id, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}
