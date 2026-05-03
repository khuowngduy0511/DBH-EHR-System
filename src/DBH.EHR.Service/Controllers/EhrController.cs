using System.Security.Claims;
using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.EHR.Service.Controllers;


[ApiController]
[Route("api/v1/ehr")]
[Produces("application/json")]
[Authorize]
public class EhrController : ControllerBase
{
    private readonly IEhrService _ehrService;
    private readonly ILogger<EhrController> _logger;

    public EhrController(IEhrService ehrService, ILogger<EhrController> logger)
    {
        _ehrService = ehrService;
        _logger = logger;
    }

    // Helper: lấy userId của user đang đăng nhập từ JWT claims
    private Guid? GetCallerUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? User.FindFirstValue("userId");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    // EHR Records

    /// <summary>
    /// Tạo EHR mới - Ghi PG Primary + IPFS
    /// </summary>
    [HttpPost("records")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(typeof(CreateEhrRecordResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateEhrRecordResponseDto>> CreateEhrRecord([FromBody] CreateEhrRecordDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation(
            "POST /api/v1/ehr/records - Tạo EHR cho bệnh nhân {PatientId}",
            request.PatientId);

        var result = await _ehrService.CreateEhrRecordAsync(request);

        return CreatedAtAction(nameof(GetEhrRecord), new { ehrId = result.EhrId }, result);
    }

    /// <summary>
    /// Cập nhật EHR - Tạo version mới. Tất cả role (kể cả Admin) đều cần consent WRITE.
    /// </summary>
    [HttpPut("records/{ehrId:guid}")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(typeof(EhrRecordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrRecordResponseDto>> UpdateEhrRecord(
        Guid ehrId,
        [FromBody] UpdateEhrRecordDto request,
        [FromHeader(Name = "X-Requester-Id")] Guid? requesterId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var effectiveRequesterId = requesterId ?? GetCallerUserId();
        if (!effectiveRequesterId.HasValue)
            return Unauthorized(new { Message = "Không xác định được danh tính người yêu cầu" });

        var (record, consentDenied, denyMessage) = await _ehrService.UpdateEhrRecordWithConsentCheckAsync(
            ehrId, request, effectiveRequesterId.Value);

        if (consentDenied)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = denyMessage });

        if (record == null)
            return NotFound(new { Message = $"EHR {ehrId} không tìm thấy" });

        return Ok(record);
    }

    /// <summary>
    /// Lấy EHR theo ID — Admin không cần consent; các role khác bắt buộc kiểm tra consent.
    /// </summary>
    [HttpGet("records/{ehrId:guid}")]
    [ProducesResponseType(typeof(EhrRecordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrRecordResponseDto>> GetEhrRecord(
        Guid ehrId,
        [FromHeader(Name = "X-Requester-Id")] Guid? requesterId = null)
    {
        // Admin không cần kiểm tra consent
        if (User.IsInRole("Admin"))
        {
            var adminResult = await _ehrService.GetEhrRecordAsync(ehrId);
            if (adminResult == null)
                return NotFound(new { Message = $"EHR {ehrId} không tìm thấy" });
            return Ok(adminResult);
        }

        // Xác định requester: header > JWT
        var effectiveId = requesterId ?? GetCallerUserId();
        if (!effectiveId.HasValue)
            return Unauthorized(new { Message = "Không xác định được danh tính người yêu cầu" });

        var (record, consentDenied, denyMessage) = await _ehrService.GetEhrRecordWithConsentCheckAsync(
            ehrId, effectiveId.Value);

        if (consentDenied)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = denyMessage });

        if (record == null)
            return NotFound(new { Message = $"EHR {ehrId} không tìm thấy" });

        return Ok(record);
    }

    /// <summary>
    /// Lấy EHR Payload (Document) theo ID - Bắt buộc có X-Requester-Id. Bệnh nhân chủ sở hữu không cần consent; người khác cần consent DOWNLOAD.
    /// </summary>
    [HttpGet("records/{ehrId:guid}/document")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetEhrDocument(
        Guid ehrId,
        [FromHeader(Name = "X-Requester-Id")] Guid? requesterId = null)
    {
        var effectiveId = requesterId ?? GetCallerUserId();
        if (!effectiveId.HasValue)
            return Unauthorized(new { Message = "Không xác định được danh tính người yêu cầu" });

        var (decryptedData, consentDenied, denyMessage) = await _ehrService.DownloadEhrDocumentAsync(
            ehrId, effectiveId.Value);

        if (consentDenied)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = denyMessage });

        if (string.IsNullOrEmpty(decryptedData))
            return NotFound(new { Message = denyMessage ?? $"Không tìm thấy tài liệu EHR {ehrId} hoặc trích xuất thất bại" });

        return Content(decryptedData, "application/json");
    }

    /// <summary>
    /// Lấy EHR Document theo user đăng nhập hiện tại (không cần X-Requester-Id)
    /// </summary>
    [HttpGet("records/{ehrId:guid}/document/self")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetEhrDocumentForCurrentUser(Guid ehrId)
    {
        var (decryptedData, forbidden, message) = await _ehrService.GetEhrDocumentForCurrentUserAsync(ehrId);

        if (forbidden)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = message });

        if (string.IsNullOrEmpty(decryptedData))
            return NotFound(new { Message = message ?? $"Không tìm thấy tài liệu EHR {ehrId} hoặc trích xuất thất bại" });

        return Content(decryptedData, "application/json");
    }

    /// <summary>
    /// Lấy EHR của bệnh nhân — Admin thấy tất cả; bệnh nhân thấy của mình; bác sĩ/nhân viên chỉ thấy record mình có consent.
    /// </summary>
    [HttpGet("records/patient/{patientId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<EhrRecordResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<EhrRecordResponseDto>>> GetPatientEhrRecords(
        Guid patientId,
        [FromHeader(Name = "X-Requester-Id")] Guid? requesterId = null)
    {
        // Admin thấy tất cả
        if (User.IsInRole("Admin"))
        {
            var records = await _ehrService.GetPatientEhrRecordsAsync(patientId, null);
            return Ok(records);
        }

        var callerId = GetCallerUserId();
        if (!callerId.HasValue)
            return Unauthorized(new { Message = "Không xác định được danh tính người yêu cầu" });

        var result = await _ehrService.GetPatientEhrRecordsAsync(patientId, callerId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Lấy EHR theo tổ chức — chỉ Admin, Doctor, Nurse, Staff trong org mới được phép.
    /// </summary>
    [HttpGet("records/org/{orgId:guid}")]
    [Authorize(Roles = "Admin,Doctor,Nurse,Receptionist,Pharmacist,LabTech")]
    [ProducesResponseType(typeof(IEnumerable<EhrRecordResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EhrRecordResponseDto>>> GetOrgEhrRecords(
        Guid orgId)
    {
        var records = await _ehrService.GetOrgEhrRecordsAsync(orgId);
        return Ok(records);
    }

    // EHR Versions 

    /// <summary>
    /// Lấy tất cả versions của EHR
    /// </summary>
    [HttpGet("records/{ehrId:guid}/versions")]
    [ProducesResponseType(typeof(IEnumerable<EhrVersionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EhrVersionDto>>> GetEhrVersions(
        Guid ehrId)
    {
        var versions = await _ehrService.GetEhrVersionsAsync(ehrId);
        return Ok(versions);
    }

    /// <summary>
    /// Lấy chi tiết một version của EHR (metadata)
    /// </summary>
    [HttpGet("records/{ehrId:guid}/versions/{versionId:guid}")]
    [ProducesResponseType(typeof(EhrVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrVersionDetailDto>> GetEhrVersionById(
        Guid ehrId, Guid versionId)
    {
        var version = await _ehrService.GetVersionByIdAsync(ehrId, versionId);
        if (version == null)
            return NotFound(new { Message = $"Không tìm thấy phiên bản {versionId} của EHR {ehrId}" });

        return Ok(version);
    }

    /// <summary>
    /// Lấy nội dung đã giải mã của một version EHR cụ thể
    /// </summary>
    [HttpGet("records/{ehrId:guid}/versions/{versionId:guid}/document")]
    [ProducesResponseType(typeof(EhrVersionDocumentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrVersionDocumentResponseDto>> GetVersionDocument(
        Guid ehrId, Guid versionId,
        [FromHeader(Name = "X-Requester-Id")] Guid? requesterId = null)
    {
        var effectiveId = requesterId ?? GetCallerUserId();
        if (!effectiveId.HasValue)
            return Unauthorized(new { Message = "Không xác định được danh tính người yêu cầu" });

        var (result, consentDenied, denyMessage) = await _ehrService.GetVersionDocumentAsync(
            ehrId, versionId, effectiveId.Value);

        if (consentDenied)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = denyMessage });

        if (result == null)
            return NotFound(new { Message = denyMessage ?? $"Không tìm thấy phiên bản {versionId} hoặc giải mã thất bại" });

        return Ok(result);
    }

    // EHR Files

    /// <summary>
    /// Lấy files của EHR
    /// </summary>
    [HttpGet("records/{ehrId:guid}/files")]
    [ProducesResponseType(typeof(IEnumerable<EhrFileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EhrFileDto>>> GetEhrFiles(
        Guid ehrId)
    {
        var files = await _ehrService.GetEhrFilesAsync(ehrId);
        return Ok(files);
    }

    /// <summary>
    /// Thêm file vào EHR (Flow : upload kết quả xét nghiệm, hình ảnh, đơn thuốc)
    /// </summary>
    [HttpPost("records/{ehrId:guid}/files")]
    [Authorize(Roles = "Doctor,Admin,LabTech,Nurse")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EhrFileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrFileDto>> AddEhrFile(Guid ehrId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "Bắt buộc phải có tệp" });

        using var stream = file.OpenReadStream();
        var result = await _ehrService.AddFileAsync(ehrId, stream, file.FileName);
        if (result == null)
            return NotFound(new { Message = $"EHR {ehrId} không tìm thấy" });

        return CreatedAtAction(nameof(GetEhrFiles), new { ehrId }, result);
    }

    /// <summary>
    /// Tải file đính kèm đã giải mã — bệnh nhân chủ sở hữu không cần consent; người khác cần DOWNLOAD
    /// </summary>
    [HttpGet("records/{ehrId:guid}/files/{fileId:guid}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadEhrFile(
        Guid ehrId, Guid fileId,
        [FromHeader(Name = "X-Requester-Id")] Guid? requesterId = null)
    {
        var effectiveId = requesterId ?? GetCallerUserId();
        if (!effectiveId.HasValue)
            return Unauthorized(new { Message = "Khong xac dinh duoc danh tinh nguoi yeu cau" });

        var (content, fileName, consentDenied, message) =
            await _ehrService.DownloadFileAsync(ehrId, fileId, effectiveId.Value);

        if (consentDenied)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = message });

        if (content == null)
            return NotFound(new { Message = message ?? $"Khong the tai file {fileId}" });

        // Detect content type from file extension
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };

        return File(content, contentType, fileName ?? "ehr_file");
    }

    /// <summary>
    /// Xóa file khỏi EHR
    /// </summary>
    [HttpDelete("records/{ehrId:guid}/files/{fileId:guid}")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEhrFile(Guid ehrId, Guid fileId)
    {
        var deleted = await _ehrService.DeleteFileAsync(ehrId, fileId);
        if (!deleted)
            return NotFound(new { Message = $"File {fileId} trong EHR {ehrId} không tìm thấy" });

        return NoContent();
    }

}
