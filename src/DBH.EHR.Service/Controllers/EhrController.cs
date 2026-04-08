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
    /// Cập nhật EHR - Tạo version mới 
    /// </summary>
    [HttpPut("records/{ehrId:guid}")]
    [Authorize(Roles = "Doctor,Admin")]
    [ProducesResponseType(typeof(EhrRecordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrRecordResponseDto>> UpdateEhrRecord(Guid ehrId, [FromBody] UpdateEhrRecordDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _ehrService.UpdateEhrRecordAsync(ehrId, request);
        if (result == null)
            return NotFound(new { Message = $"EHR {ehrId} không tìm thấy" });

        return Ok(result);
    }

    /// <summary>
    /// Lấy EHR theo ID - Nếu có X-Requester-Id header sẽ kiểm tra consent
    /// </summary>
    [HttpGet("records/{ehrId:guid}")]
    [ProducesResponseType(typeof(EhrRecordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrRecordResponseDto>> GetEhrRecord(
        Guid ehrId, 
        [FromHeader(Name = "X-Requester-Id")] Guid? requesterId = null)
    {
        // Nếu có requester ID → kiểm tra consent trước khi trả data
        if (requesterId.HasValue)
        {
            var (record, consentDenied, denyMessage) = await _ehrService.GetEhrRecordWithConsentCheckAsync(
                ehrId, requesterId.Value);
            
            if (consentDenied)
                return StatusCode(StatusCodes.Status403Forbidden, new { Message = denyMessage });
            
            if (record == null)
                return NotFound(new { Message = $"EHR {ehrId} không tìm thấy" });
            
            return Ok(record);
        }

        // Không có requester ID → trả trực tiếp (internal service call)
        var result = await _ehrService.GetEhrRecordAsync(ehrId);
        
        if (result == null)
            return NotFound(new { Message = $"EHR {ehrId} không tìm thấy" });

        return Ok(result);
    }

    /// <summary>
    /// Lấy EHR Payload (Document) theo ID - Bắt buộc có X-Requester-Id
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
        if (!requesterId.HasValue)
            return BadRequest(new { Message = "X-Requester-Id header is required to download EHR document" });

        var (decryptedData, consentDenied, denyMessage) = await _ehrService.GetEhrDocumentAsync(
            ehrId, requesterId.Value);
        
        if (consentDenied)
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = denyMessage });
        
        if (string.IsNullOrEmpty(decryptedData))
            return NotFound(new { Message = denyMessage ?? $"EHR Document {ehrId} not found or extraction failed" });
        
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
            return NotFound(new { Message = message ?? $"EHR Document {ehrId} not found or extraction failed" });

        return Content(decryptedData, "application/json");
    }

    /// <summary>
    /// Tải raw encrypted payload từ IPFS CID
    /// </summary>
    [HttpGet("ipfs/{cid}/download")]
    [ProducesResponseType(typeof(IpfsRawDownloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IpfsRawDownloadResponseDto>> DownloadIpfsRaw(string cid)
    {
        var encryptedData = await _ehrService.DownloadIpfsRawAsync(cid);
        if (string.IsNullOrWhiteSpace(encryptedData))
        {
            return NotFound(new { Message = "IPFS payload not found" });
        }

        return Ok(new IpfsRawDownloadResponseDto
        {
            IpfsCid = cid,
            EncryptedData = encryptedData
        });
    }

    /// <summary>
    /// Tải raw encrypted payload từ IPFS theo ehrId (version mới nhất)
    /// </summary>
    [HttpGet("records/{ehrId:guid}/ipfs/download")]
    [ProducesResponseType(typeof(IpfsRawDownloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IpfsRawDownloadResponseDto>> DownloadLatestIpfsRawByEhrId(Guid ehrId)
    {
        var result = await _ehrService.DownloadLatestIpfsRawByEhrIdAsync(ehrId);
        if (result == null)
        {
            return NotFound(new { Message = "EHR/IPFS payload not found for this ehrId" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Mã hóa payload và upload lên IPFS bằng key của user đăng nhập
    /// </summary>
    [HttpPost("ipfs/encrypt")]
    [ProducesResponseType(typeof(EncryptIpfsPayloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EncryptIpfsPayloadResponseDto>> EncryptToIpfs([FromBody] EncryptIpfsPayloadRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _ehrService.EncryptToIpfsForCurrentUserAsync(request);
        if (result == null)
        {
            return BadRequest(new { Message = "Failed to encrypt payload for current user" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Giải mã payload IPFS bằng key của user đăng nhập
    /// </summary>
    [HttpPost("ipfs/decrypt")]
    [ProducesResponseType(typeof(DecryptIpfsPayloadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DecryptIpfsPayloadResponseDto>> DecryptFromIpfs([FromBody] DecryptIpfsPayloadRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var decrypted = await _ehrService.DecryptIpfsForCurrentUserAsync(request);
        if (string.IsNullOrWhiteSpace(decrypted))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { Message = "Decrypt failed. Ensure the wrapped key belongs to current user." });
        }

        return Ok(new DecryptIpfsPayloadResponseDto { Data = decrypted });
    }

    /// <summary>
    /// Lấy EHR của bệnh nhân
    /// </summary>
    [HttpGet("records/patient/{patientId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<EhrRecordResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EhrRecordResponseDto>>> GetPatientEhrRecords(
        Guid patientId)
    {
        var records = await _ehrService.GetPatientEhrRecordsAsync(patientId);
        return Ok(records);
    }

    /// <summary>
    /// Lấy EHR theo tổ chức (organization)
    /// </summary>
    [HttpGet("records/org/{orgId:guid}")]
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
    /// Lấy chi tiết một version của EHR 
    /// </summary>
    [HttpGet("records/{ehrId:guid}/versions/{versionId:guid}")]
    [ProducesResponseType(typeof(EhrVersionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrVersionDetailDto>> GetEhrVersionById(
        Guid ehrId, Guid versionId)
    {
        var version = await _ehrService.GetVersionByIdAsync(ehrId, versionId);
        if (version == null)
            return NotFound(new { Message = $"Version {versionId} of EHR {ehrId} không tìm thấy" });

        return Ok(version);
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
    [Authorize(Roles = "Doctor,Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EhrFileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrFileDto>> AddEhrFile(Guid ehrId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "File is required" });

        using var stream = file.OpenReadStream();
        var result = await _ehrService.AddFileAsync(ehrId, stream, file.FileName);
        if (result == null)
            return NotFound(new { Message = $"EHR {ehrId} không tìm thấy" });

        return CreatedAtAction(nameof(GetEhrFiles), new { ehrId }, result);
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
