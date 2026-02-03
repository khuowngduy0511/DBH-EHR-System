using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBH.EHR.Service.Controllers;


[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class EhrController : ControllerBase
{
    private readonly IEhrService _ehrService;
    private readonly ILogger<EhrController> _logger;

    public EhrController(IEhrService ehrService, ILogger<EhrController> logger)
    {
        _ehrService = ehrService;
        _logger = logger;
    }

    //EHR Records

    /// <summary>
    /// Tạo EHR mới - Ghi PG Primary + Mongo Primary
    /// </summary>
    [HttpPost("records")]
    [ProducesResponseType(typeof(CreateEhrRecordResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateEhrRecordResponseDto>> CreateEhrRecord([FromBody] CreateEhrRecordDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation(
            "POST /api/ehr/records - Tạo EHR cho bệnh nhân {PatientId} bởi bác sĩ {DoctorId}",
            request.PatientId, request.CreatedByDoctorId);

        var result = await _ehrService.CreateEhrRecordAsync(request);

        return CreatedAtAction(nameof(GetEhrRecord), new { ehrId = result.EhrId }, result);
    }

    /// <summary>
    /// Lấy EHR theo ID
    /// </summary>
    [HttpGet("records/{ehrId:guid}")]
    [ProducesResponseType(typeof(EhrRecordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrRecordResponseDto>> GetEhrRecord(Guid ehrId, [FromQuery] bool useReplica = false)
    {
        var record = await _ehrService.GetEhrRecordAsync(ehrId, useReplica);
        
        if (record == null)
            return NotFound(new { Message = $"EHR {ehrId} không tìm thấy" });

        return Ok(record);
    }

    /// <summary>
    /// Lấy EHR của bệnh nhân
    /// </summary>
    [HttpGet("records/patient/{patientId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<EhrRecordResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EhrRecordResponseDto>>> GetPatientEhrRecords(
        Guid patientId, 
        [FromQuery] bool useReplica = false)
    {
        var records = await _ehrService.GetPatientEhrRecordsAsync(patientId, useReplica);
        return Ok(records);
    }

    /// <summary>
    /// Lấy EHR theo bệnh viện
    /// </summary>
    [HttpGet("records/hospital/{hospitalId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<EhrRecordResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EhrRecordResponseDto>>> GetHospitalEhrRecords(
        Guid hospitalId, 
        [FromQuery] bool useReplica = false)
    {
        var records = await _ehrService.GetHospitalEhrRecordsAsync(hospitalId, useReplica);
        return Ok(records);
    }

    //  EHR Versions 

    /// <summary>
    /// Lấy tất cả versions của EHR
    /// </summary>
    [HttpGet("records/{ehrId:guid}/versions")]
    [ProducesResponseType(typeof(IEnumerable<EhrVersionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EhrVersionDto>>> GetEhrVersions(
        Guid ehrId, 
        [FromQuery] bool useReplica = false)
    {
        var versions = await _ehrService.GetEhrVersionsAsync(ehrId, useReplica);
        return Ok(versions);
    }

    // EHR Files

    /// <summary>
    /// Lấy files của EHR
    /// </summary>
    [HttpGet("records/{ehrId:guid}/files")]
    [ProducesResponseType(typeof(IEnumerable<EhrFileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EhrFileDto>>> GetEhrFiles(
        Guid ehrId,
        [FromQuery] int? version = null,
        [FromQuery] bool useReplica = false)
    {
        var files = await _ehrService.GetEhrFilesAsync(ehrId, version, useReplica);
        return Ok(files);
    }
}
