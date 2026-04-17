using DBH.Shared.Contracts.Blockchain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Blockchain.Service.Controllers;

[ApiController]
[Route("api/v1/blockchain/audit")]
[Produces("application/json")]
[Authorize]
public class BlockchainAuditController : ControllerBase
{
    private readonly IAuditBlockchainService _auditService;

    public BlockchainAuditController(IAuditBlockchainService auditService)
    {
        _auditService = auditService;
    }

    [HttpPost("commit")]
    [ProducesResponseType(typeof(BlockchainTransactionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BlockchainTransactionResult>> CommitAuditAsync([FromBody] AuditEntry entry)
    {
        var result = await _auditService.CommitAuditEntryAsync(entry);
        return Ok(result);
    }

    [HttpGet("{auditId}")]
    [ProducesResponseType(typeof(AuditEntry), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditEntry>> GetAuditAsync(string auditId)
    {
        var audit = await _auditService.GetAuditEntryAsync(auditId);
        return audit is null ? NotFound() : Ok(audit);
    }

    [HttpGet("patient/{patientDid}")]
    [ProducesResponseType(typeof(List<AuditEntry>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AuditEntry>>> GetAuditsByPatientAsync(string patientDid)
    {
        var audits = await _auditService.GetAuditsByPatientAsync(patientDid);
        return Ok(audits);
    }

    [HttpGet("actor/{actorDid}")]
    [ProducesResponseType(typeof(List<AuditEntry>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AuditEntry>>> GetAuditsByActorAsync(string actorDid)
    {
        var audits = await _auditService.GetAuditsByActorAsync(actorDid);
        return Ok(audits);
    }
}