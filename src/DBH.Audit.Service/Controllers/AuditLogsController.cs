using DBH.Audit.Service.DTOs;
using DBH.Audit.Service.Models.Enums;
using DBH.Audit.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Audit.Service.Controllers;

[ApiController]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// POST /api/audit-logs - Tạo audit log (internal use từ các service khác)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAuditLog([FromBody] CreateAuditLogRequest request)
    {
        var result = await _auditService.CreateAuditLogAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// GET /api/audit-logs/{id} - Lấy audit log theo ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAuditLog(Guid id)
    {
        var result = await _auditService.GetAuditLogByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// GET /api/audit-logs/search - Search với filters
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchAuditLogs([FromQuery] AuditLogQueryParams query)
    {
        var result = await _auditService.SearchAuditLogsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/audit-logs/by-patient/{patientId} - Logs của patient
    /// </summary>
    [HttpGet("by-patient/{patientId:guid}")]
    public async Task<IActionResult> GetByPatient(Guid patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _auditService.GetAuditLogsByPatientAsync(patientId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/audit-logs/by-actor/{actorUserId} - Logs của user
    /// </summary>
    [HttpGet("by-actor/{actorUserId:guid}")]
    public async Task<IActionResult> GetByActor(Guid actorUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _auditService.GetAuditLogsByActorAsync(actorUserId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/audit-logs/by-target/{targetId} - Logs của target object
    /// </summary>
    [HttpGet("by-target/{targetId:guid}")]
    public async Task<IActionResult> GetByTarget(Guid targetId, [FromQuery] TargetType targetType, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _auditService.GetAuditLogsByTargetAsync(targetId, targetType, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/audit-logs/stats - Thống kê audit
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] Guid? organizationId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var result = await _auditService.GetAuditStatsAsync(organizationId, fromDate, toDate);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/audit-logs/sync/{blockchainAuditId} - Sync từ blockchain
    /// </summary>
    [HttpPost("sync/{blockchainAuditId}")]
    public async Task<IActionResult> SyncFromBlockchain(string blockchainAuditId)
    {
        var result = await _auditService.SyncFromBlockchainAsync(blockchainAuditId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
