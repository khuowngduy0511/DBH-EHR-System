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
    [ProducesResponseType(typeof(EhrResponse<CreateEhrRecordResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EhrResponse<CreateEhrRecordResponseDto>>> CreateEhrRecord([FromBody] CreateEhrRecordDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        _logger.LogInformation(
            "POST /api/v1/ehr/records - Tạo EHR cho bệnh nhân {PatientId}",
            request.PatientId);

        var result = await _ehrService.CreateEhrRecordAsync(request);

        if (!result.Success || result.Data == null)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetEhrRecord), new { ehrId = result.Data.EhrId }, result);
    }

    /// <summary>
    /// Lấy danh sách EHR mà người dùng hiện tại có quyền xem (kết hợp OrgId và Consents).
    /// Hỗ trợ phân trang và tìm kiếm.
    /// </summary>
    [HttpGet("records/my-visible")]
    [ProducesResponseType(typeof(PaginatedResult<EhrRecordResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<EhrRecordResponseDto>>> GetMyVisibleRecords(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _ehrService.GetMyVisibleRecordsAsync(page, pageSize, search);
        return Ok(result);
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
            return NotFound(new { Message = $"Không tìm thấy hồ sơ EHR {ehrId}" });

        return Ok(record);
    }

    /// <summary>
    /// Lấy EHR theo ID — Admin không cần consent; các role khác bắt buộc kiểm tra consent.
    /// </summary>
    [HttpGet("records/{ehrId:guid}")]
    [ProducesResponseType(typeof(EhrResponse<EhrRecordResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrResponse<EhrRecordResponseDto>>> GetEhrRecord(
        Guid ehrId,
        [FromHeader(Name = "X-Requester-Id")] Guid? requesterId = null)
    {
        // Admin không cần kiểm tra consent
        if (User.IsInRole("Admin"))
        {
            var adminResult = await _ehrService.GetEhrRecordAsync(ehrId);
            if (!adminResult.Success || adminResult.Data == null)
                return NotFound(adminResult);
            return Ok(adminResult);
        }

        // Xác định requester: header > JWT
        var effectiveId = requesterId ?? GetCallerUserId();
        if (!effectiveId.HasValue)
        {
            return Unauthorized(new EhrResponse<EhrRecordResponseDto>
            {
                Success = false,
                Message = "Không xác định được danh tính người yêu cầu",
                Data = null
            });
        }

        var (record, consentDenied, denyMessage) = await _ehrService.GetEhrRecordWithConsentCheckAsync(ehrId, effectiveId.Value);

        if (consentDenied)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new EhrResponse<EhrRecordResponseDto>
            {
                Success = false,
                Message = denyMessage ?? "Không có quyền truy cập (consent)",
                Data = null
            });
        }

        if (record == null)
        {
            return NotFound(new EhrResponse<EhrRecordResponseDto>
            {
                Success = false,
                Message = denyMessage ?? $"Không tìm thấy hồ sơ EHR với Id {ehrId}",
                Data = null
            });
        }

        return Ok(new EhrResponse<EhrRecordResponseDto>
        {
            Success = true,
            Message = "Lấy hồ sơ EHR thành công",
            Data = record
        });
    }

    /// <summary>
    /// Lấy EHR Payload (Document) theo ID - Bắt buộc có X-Requester-Id. Bệnh nhân chủ sở hữu không cần consent; người khác cần consent DOWNLOAD.
    /// </summary>
    [HttpGet("records/{ehrId:guid}/document")]
    [ProducesResponseType(typeof(EhrResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrResponse<string>>> GetEhrDocument(
        Guid ehrId,
        [FromHeader(Name = "X-Requester-Id")] Guid? requesterId = null)
    {
        var effectiveId = requesterId ?? GetCallerUserId();
        if (!effectiveId.HasValue)
            return Unauthorized(new EhrResponse<string>
            {
                Success = false,
                Message = "Không xác định được danh tính người yêu cầu",
                Data = null
            });

        try
        {
            var (decryptedData, consentDenied, denyMessage) = await _ehrService.DownloadEhrDocumentAsync(
                ehrId, effectiveId.Value);

            if (consentDenied)
                return StatusCode(StatusCodes.Status403Forbidden, new EhrResponse<string>
                {
                    Success = false,
                    Message = denyMessage ?? "Không có quyền truy cập (consent)",
                    Data = null
                });

            if (string.IsNullOrEmpty(decryptedData))
                return NotFound(new EhrResponse<string>
                {
                    Success = false,
                    Message = denyMessage ?? $"Không tìm thấy tài liệu của hồ sơ EHR {ehrId} hoặc giải mã thất bại",
                    Data = null
                });

            return Ok(new EhrResponse<string>
            {
                Success = true,
                Message = "Lấy tài liệu EHR thành công",
                Data = decryptedData
            });
        }
        catch (EhrException ex)
        {
            _logger.LogError(ex, "Tampering detected during document retrieval for EHR {EhrId}", ehrId);
            return StatusCode(StatusCodes.Status500InternalServerError, new EhrResponse<object>
            {
                Success = ex.Success,
                Message = ex.Message,
                Data = ex.Data
            });
        }
        catch (Exception ex) when (ex.Message.Contains("Tampering Detected"))
        {
            _logger.LogError(ex, "Tampering detected during document retrieval for EHR {EhrId}", ehrId);
            return StatusCode(StatusCodes.Status500InternalServerError, new EhrResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
    }

    /// <summary>
    /// Lấy EHR Document theo user đăng nhập hiện tại (không cần X-Requester-Id)
    /// </summary>
    [HttpGet("records/{ehrId:guid}/document/self")]
    [ProducesResponseType(typeof(EhrResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrResponse<string>>> GetEhrDocumentForCurrentUser(Guid ehrId)
    {
        try
        {
            var (decryptedData, forbidden, message) = await _ehrService.GetEhrDocumentForCurrentUserAsync(ehrId);

            if (forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, new EhrResponse<string>
                {
                    Success = false,
                    Message = message ?? "Không có quyền truy cập",
                    Data = null
                });

            if (string.IsNullOrEmpty(decryptedData))
                return NotFound(new EhrResponse<string>
                {
                    Success = false,
                    Message = message ?? $"Không tìm thấy tài liệu của hồ sơ EHR {ehrId} hoặc giải mã thất bại",
                    Data = null
                });

            return Ok(new EhrResponse<string>
            {
                Success = true,
                Message = "Lấy tài liệu EHR thành công",
                Data = decryptedData
            });
        }
        catch (EhrException ex)
        {
            _logger.LogError(ex, "Tampering detected during document retrieval for EHR {EhrId}", ehrId);
            return StatusCode(StatusCodes.Status500InternalServerError, new EhrResponse<object>
            {
                Success = ex.Success,
                Message = ex.Message,
                Data = ex.Data
            });
        }
        catch (Exception ex) when (ex.Message.Contains("Tampering Detected"))
        {
            _logger.LogError(ex, "Tampering detected during document retrieval for EHR {EhrId}", ehrId);
            return StatusCode(StatusCodes.Status500InternalServerError, new EhrResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
    }

    /// <summary>
    /// Lấy metadata tối thiểu của EHR patient (ehrId, orgId, createdAt...) mà KHÔNG cần consent.
    /// Dùng để frontend biết patient có hồ sơ không và lấy ehrId trước khi gửi access request.
    /// Chỉ trả về thông tin định danh, không chứa data y tế nhạy cảm.
    /// </summary>
    [HttpGet("records/patient/{patientId:guid}/metadata")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<EhrMetadataDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<EhrMetadataDto>>> GetPatientEhrMetadata(Guid patientId)
    {
        var metadata = await _ehrService.GetPatientEhrMetadataAsync(patientId);
        return Ok(metadata);
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
            return NotFound(new { Message = $"Không tìm thấy phiên bản {versionId} của hồ sơ EHR {ehrId}" });

        return Ok(version);
    }

    /// <summary>
    /// Lấy nội dung đã giải mã của một version EHR cụ thể
    /// </summary>
    [HttpGet("records/{ehrId:guid}/versions/{versionId:guid}/document")]
    [ProducesResponseType(typeof(EhrResponse<EhrVersionDocumentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrResponse<EhrVersionDocumentResponseDto>>> GetVersionDocument(
        Guid ehrId, Guid versionId,
        [FromHeader(Name = "X-Requester-Id")] Guid? requesterId = null)
    {
        var effectiveId = requesterId ?? GetCallerUserId();
        if (!effectiveId.HasValue)
            return Unauthorized(new EhrResponse<EhrVersionDocumentResponseDto>
            {
                Success = false,
                Message = "Không xác định được danh tính người yêu cầu",
                Data = null
            });

        try
        {
            var (result, consentDenied, denyMessage) = await _ehrService.GetVersionDocumentAsync(
                ehrId, versionId, effectiveId.Value);

            if (consentDenied)
                return StatusCode(StatusCodes.Status403Forbidden, new EhrResponse<EhrVersionDocumentResponseDto>
                {
                    Success = false,
                    Message = denyMessage ?? "Không có quyền truy cập (consent)",
                    Data = null
                });

            if (result == null)
                return NotFound(new EhrResponse<EhrVersionDocumentResponseDto>
                {
                    Success = false,
                    Message = denyMessage ?? $"Không tìm thấy phiên bản {versionId} hoặc giải mã thất bại",
                    Data = null
                });

            return Ok(new EhrResponse<EhrVersionDocumentResponseDto>
            {
                Success = true,
                Message = "Lấy tài liệu phiên bản EHR thành công",
                Data = result
            });
        }
        catch (EhrException ex)
        {
            _logger.LogError(ex, "Tampering detected during version document retrieval for EHR {EhrId} Version {VersionId}", ehrId, versionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new EhrResponse<object>
            {
                Success = ex.Success,
                Message = ex.Message,
                Data = ex.Data
            });
        }
        catch (Exception ex) when (ex.Message.Contains("Tampering Detected"))
        {
            _logger.LogError(ex, "Tampering detected during version document retrieval for EHR {EhrId} Version {VersionId}", ehrId, versionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new EhrResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = null
            });
        }
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
            return BadRequest(new { Message = "Tệp tin đính kèm là bắt buộc" });

        using var stream = file.OpenReadStream();
        var result = await _ehrService.AddFileAsync(ehrId, stream, file.FileName);
        if (result == null)
            return NotFound(new { Message = $"Không tìm thấy hồ sơ EHR {ehrId}" });

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
            return Unauthorized(new { Message = "Không xác định được danh tính người yêu cầu" });

        var (content, fileName, consentDenied, message) =
            await _ehrService.DownloadFileAsync(ehrId, fileId, effectiveId.Value);

        if (consentDenied)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = message });

        if (content == null)
            return NotFound(new { Message = message ?? $"Không thể tải tệp tin {fileId}" });

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
            return NotFound(new { Message = $"Không tìm thấy tệp tin {fileId} trong hồ sơ EHR {ehrId}" });

        return NoContent();
    }

}
