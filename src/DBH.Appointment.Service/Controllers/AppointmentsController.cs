using DBH.Appointment.Service.DTOs;
using DBH.Appointment.Service.Models.Enums;
using DBH.Appointment.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Appointment.Service.Controllers;


[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentsController> _logger;

    public AppointmentsController(IAppointmentService appointmentService, ILogger<AppointmentsController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    // =========================================================================
    // APPOINTMENTS - CRUD
    // =========================================================================

    /// <summary>
    /// Tạo lịch hẹn mới
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AppointmentResponse>>> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _appointmentService.CreateAppointmentAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetAppointment), new { id = result.Data!.AppointmentId }, result);
    }

    /// <summary>
    /// Lấy thông tin lịch hẹn
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AppointmentResponse>>> GetAppointment(Guid id)
    {
        var result = await _appointmentService.GetAppointmentByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách lịch hẹn với filter
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResponse<AppointmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AppointmentResponse>>> GetAppointments(
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

    /// <summary>
    /// Cập nhật trạng thái lịch hẹn
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AppointmentResponse>>> UpdateStatus(Guid id, [FromQuery] AppointmentStatus status)
    {
        var result = await _appointmentService.UpdateAppointmentStatusAsync(id, status);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Đổi lịch hẹn
    /// </summary>
    [HttpPut("{id:guid}/reschedule")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AppointmentResponse>>> Reschedule(Guid id, [FromQuery] DateTime newDate)
    {
        var result = await _appointmentService.RescheduleAppointmentAsync(id, newDate);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // =========================================================================
    // APPOINTMENTS - LIFECYCLE (Flow 3: Đặt lịch khám)
    // =========================================================================

    /// <summary>
    /// Bác sĩ xác nhận lịch hẹn (PENDING → CONFIRMED)
    /// </summary>
    [HttpPut("{id:guid}/confirm")]
    [Authorize(Roles = "Receptionist,Doctor,Admin")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AppointmentResponse>>> ConfirmAppointment(Guid id)
    {
        var result = await _appointmentService.ConfirmAppointmentAsync(id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Bác sĩ từ chối lịch hẹn (PENDING → CANCELLED)
    /// </summary>
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Receptionist,Doctor,Admin")]
    [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AppointmentResponse>>> RejectAppointment(Guid id, [FromBody] CancelAppointmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _appointmentService.RejectAppointmentAsync(id, request.Reason);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Hủy lịch hẹn (bất kỳ ai có quyền)
    /// </summary>
    [HttpPut("{id:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AppointmentResponse>>> CancelAppointment(Guid id, [FromBody] CancelAppointmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _appointmentService.CancelAppointmentAsync(id, request.Reason);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Bệnh nhân check-in (CONFIRMED → CHECKED_IN)
    /// </summary>
    [HttpPut("{id:guid}/check-in")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<AppointmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AppointmentResponse>>> CheckIn(Guid id)
    {
        var result = await _appointmentService.CheckInAsync(id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    // =========================================================================
    // DOCTOR SEARCH
    // =========================================================================

    /// <summary>
    /// Tìm bác sĩ theo chuyên khoa
    /// </summary>
    [HttpGet("doctors/search")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResponse<DoctorSearchResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<DoctorSearchResult>>> SearchDoctors([FromQuery] SearchDoctorQuery query)
    {
        var result = await _appointmentService.SearchDoctorsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách bệnh nhân đã khám của bác sĩ (distinct, sắp xếp theo lần khám gần nhất)
    /// </summary>
    [HttpGet("doctors/{doctorId:guid}/patients")]
    [Authorize(Roles = "Receptionist,Doctor,Admin")]
    [ProducesResponseType(typeof(PagedResponse<DoctorPatientResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<DoctorPatientResponse>>> GetPatientsByDoctor(
        Guid doctorId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _appointmentService.GetPatientsByDoctorAsync(doctorId, page, pageSize);
        return Ok(result);
    }
}


// =============================================================================
// ENCOUNTERS CONTROLLER
// =============================================================================


[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class EncountersController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<EncountersController> _logger;

    public EncountersController(IAppointmentService appointmentService, ILogger<EncountersController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    /// <summary>
    /// Tạo lượt khám mới (encounter)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(typeof(ApiResponse<EncounterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<EncounterResponse>>> CreateEncounter([FromBody] CreateEncounterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _appointmentService.CreateEncounterAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetEncounter), new { id = result.Data!.EncounterId }, result);
    }

    /// <summary>
    /// Lấy thông tin encounter
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<EncounterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EncounterResponse>>> GetEncounter(Guid id)
    {
        var result = await _appointmentService.GetEncounterByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Lấy encounters theo appointment
    /// </summary>
    [HttpGet("by-appointment/{appointmentId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResponse<EncounterResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<EncounterResponse>>> GetByAppointment(Guid appointmentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _appointmentService.GetEncountersByAppointmentIdAsync(appointmentId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Lấy encounters theo patient
    /// </summary>
    [HttpGet("by-patient/{patientId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResponse<EncounterResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<EncounterResponse>>> GetByPatient(Guid patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _appointmentService.GetEncountersByPatientIdAsync(patientId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Cập nhật encounter
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(typeof(ApiResponse<EncounterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EncounterResponse>>> UpdateEncounter(Guid id, [FromBody] UpdateEncounterRequest request)
    {
        var result = await _appointmentService.UpdateEncounterAsync(id, request);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Hoàn tất encounter + tự động tạo EHR (Flow 4)
    /// </summary>
    [HttpPut("{id:guid}/complete")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(typeof(ApiResponse<EncounterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<EncounterResponse>>> CompleteEncounter(Guid id, [FromBody] CompleteEncounterRequest request)
    {
        var result = await _appointmentService.CompleteEncounterAsync(id, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
