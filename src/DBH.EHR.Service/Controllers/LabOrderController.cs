using System.Security.Claims;
using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Enums;
using DBH.EHR.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.EHR.Service.Controllers;

[ApiController]
[Route("api/v1/lab-orders")]
[Produces("application/json")]
[Authorize]
public class LabOrderController : ControllerBase
{
    private readonly ILabOrderService _labOrderService;
    private readonly ILogger<LabOrderController> _logger;

    public LabOrderController(ILabOrderService labOrderService, ILogger<LabOrderController> logger)
    {
        _labOrderService = labOrderService;
        _logger = logger;
    }

    private Guid? GetCallerUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? User.FindFirstValue("userId");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    /// <summary>
    /// Doctor tạo chỉ định xét nghiệm
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(typeof(LabOrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LabOrderResponseDto>> CreateLabOrder([FromBody] CreateLabOrderDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var doctorUserId = GetCallerUserId();
        if (!doctorUserId.HasValue)
            return Unauthorized(new { Message = "Không xác định được danh tính người dùng" });

        try
        {
            var result = await _labOrderService.CreateAsync(dto, doctorUserId.Value);
            return CreatedAtAction(nameof(GetLabOrder), new { id = result.LabOrderId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết một lab order
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Doctor,Admin,LabTech,Nurse")]
    [ProducesResponseType(typeof(LabOrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LabOrderResponseDto>> GetLabOrder(Guid id)
    {
        var result = await _labOrderService.GetByIdAsync(id);
        if (result == null)
            return NotFound(new { Message = $"Lab order {id} không tìm thấy" });
        return Ok(result);
    }

    /// <summary>
    /// LabTech xem hàng đợi chỉ định theo tổ chức (có lọc status)
    /// </summary>
    [HttpGet("org/{orgId:guid}")]
    [Authorize(Roles = "LabTech,Admin,Doctor,Nurse")]
    [ProducesResponseType(typeof(IEnumerable<LabOrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LabOrderResponseDto>>> GetByOrg(
        Guid orgId,
        [FromQuery] string? status = null)
    {
        LabOrderStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LabOrderStatus>(status.ToUpperInvariant(), out var parsed))
            statusEnum = parsed;

        var results = await _labOrderService.GetByOrgAsync(orgId, statusEnum);
        return Ok(results);
    }

    /// <summary>
    /// Lấy chỉ định XN theo EHR
    /// </summary>
    [HttpGet("ehr/{ehrId:guid}")]
    [Authorize(Roles = "Doctor,Admin,LabTech,Patient,Nurse")]
    [ProducesResponseType(typeof(IEnumerable<LabOrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LabOrderResponseDto>>> GetByEhr(Guid ehrId)
    {
        var results = await _labOrderService.GetByEhrAsync(ehrId);
        return Ok(results);
    }

    /// <summary>
    /// Tổng hợp chỉ định theo bệnh nhân
    /// </summary>
    [HttpGet("patient/{patientId:guid}")]
    [Authorize(Roles = "Doctor,Admin,Patient,Nurse,LabTech")]
    [ProducesResponseType(typeof(IEnumerable<LabOrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LabOrderResponseDto>>> GetByPatient(Guid patientId)
    {
        var results = await _labOrderService.GetByPatientAsync(patientId);
        return Ok(results);
    }

    /// <summary>
    /// LabTech cập nhật trạng thái chỉ định
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "LabTech,Admin")]
    [ProducesResponseType(typeof(LabOrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LabOrderResponseDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateLabOrderStatusDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var labTechUserId = GetCallerUserId();
        if (!labTechUserId.HasValue)
            return Unauthorized(new { Message = "Không xác định được danh tính người dùng" });

        var result = await _labOrderService.UpdateStatusAsync(id, labTechUserId.Value, dto.Status);
        if (result == null)
            return NotFound(new { Message = $"Lab order {id} không tìm thấy hoặc không thể cập nhật" });

        return Ok(result);
    }

    /// <summary>
    /// LabTech nhập kết quả xét nghiệm có cấu trúc
    /// </summary>
    [HttpPost("{id:guid}/result")]
    [Authorize(Roles = "LabTech,Admin")]
    [ProducesResponseType(typeof(LabOrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LabOrderResponseDto>> SubmitResult(
        Guid id,
        [FromBody] SubmitLabResultDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var labTechUserId = GetCallerUserId();
        if (!labTechUserId.HasValue)
            return Unauthorized(new { Message = "Không xác định được danh tính người dùng" });

        var result = await _labOrderService.SubmitResultAsync(id, labTechUserId.Value, dto);
        if (result == null)
            return NotFound(new { Message = $"Lab order {id} không tìm thấy hoặc không thể nhập kết quả" });

        return Ok(result);
    }

    /// <summary>
    /// Doctor/Admin hủy chỉ định (chỉ khi PENDING)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelLabOrder(Guid id)
    {
        var requesterId = GetCallerUserId();
        if (!requesterId.HasValue)
            return Unauthorized(new { Message = "Không xác định được danh tính người dùng" });

        var cancelled = await _labOrderService.CancelAsync(id, requesterId.Value);
        if (!cancelled)
            return BadRequest(new { Message = $"Lab order {id} không tìm thấy hoặc chỉ hủy được khi ở trạng thái PENDING" });

        return NoContent();
    }
}
