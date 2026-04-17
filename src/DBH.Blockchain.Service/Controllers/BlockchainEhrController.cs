using DBH.Blockchain.Service.DTOs;
using DBH.Shared.Contracts.Blockchain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Blockchain.Service.Controllers;

[ApiController]
[Route("api/v1/blockchain/ehr")]
[Produces("application/json")]
[Authorize]
public class BlockchainEhrController : ControllerBase
{
    private readonly IEhrBlockchainService _ehrService;

    public BlockchainEhrController(IEhrBlockchainService ehrService)
    {
        _ehrService = ehrService;
    }

    [HttpPost("commit")]
    [ProducesResponseType(typeof(BlockchainTransactionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BlockchainTransactionResult>> CommitEhrAsync([FromBody] EhrHashRecord record)
    {
        var result = await _ehrService.CommitEhrHashAsync(record);
        return Ok(result);
    }

    [HttpGet("{ehrId}/{version:int}")]
    [ProducesResponseType(typeof(EhrHashRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EhrHashRecord>> GetEhrAsync(string ehrId, int version)
    {
        var record = await _ehrService.GetEhrHashAsync(ehrId, version);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpGet("{ehrId}/history")]
    [ProducesResponseType(typeof(List<EhrHashRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EhrHashRecord>>> GetEhrHistoryAsync(string ehrId)
    {
        var history = await _ehrService.GetEhrHistoryAsync(ehrId);
        return Ok(history);
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> VerifyEhrAsync([FromBody] EhrVerifyRequestDto request)
    {
        var isValid = await _ehrService.VerifyEhrIntegrityAsync(request.EhrId, request.Version, request.CurrentHash);
        return Ok(new { request.EhrId, request.Version, isValid });
    }
}