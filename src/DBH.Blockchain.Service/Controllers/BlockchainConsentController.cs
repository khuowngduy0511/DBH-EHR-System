using DBH.Blockchain.Service.DTOs;
using DBH.Shared.Contracts;
using DBH.Shared.Contracts.Blockchain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Blockchain.Service.Controllers;

[ApiController]
[Route("api/v1/blockchain/consent")]
[Produces("application/json")]
[Authorize]
public class BlockchainConsentController : ControllerBase
{
    private readonly IConsentBlockchainService _consentService;

    public BlockchainConsentController(IConsentBlockchainService consentService)
    {
        _consentService = consentService;
    }

    [HttpPost("grant")]
    [ProducesResponseType(typeof(BlockchainTransactionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BlockchainTransactionResult>> GrantConsentAsync([FromBody] ConsentRecord record)
    {
        var result = await _consentService.GrantConsentAsync(record);
        return Ok(result);
    }

    [HttpPost("revoke")]
    [ProducesResponseType(typeof(BlockchainTransactionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<BlockchainTransactionResult>> RevokeConsentAsync([FromBody] ConsentRevokeRequestDto request)
    {
        var revokedAt = string.IsNullOrWhiteSpace(request.RevokedAt) ? VietnamTimeHelper.Now.ToString("o") : request.RevokedAt;
        var result = await _consentService.RevokeConsentAsync(request.ConsentId, revokedAt!, request.Reason);
        return Ok(result);
    }

    [HttpGet("{consentId}")]
    [ProducesResponseType(typeof(ConsentRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsentRecord>> GetConsentAsync(string consentId)
    {
        var consent = await _consentService.GetConsentAsync(consentId);
        return consent is null ? NotFound() : Ok(consent);
    }

    [HttpGet("{consentId}/verify/{granteeDid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> VerifyConsentAsync(string consentId, string granteeDid)
    {
        var isValid = await _consentService.VerifyConsentAsync(consentId, granteeDid);
        return Ok(new { consentId, granteeDid, isValid });
    }

    [HttpGet("patient/{patientDid}")]
    [ProducesResponseType(typeof(List<ConsentRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConsentRecord>>> GetPatientConsentsAsync(string patientDid)
    {
        var consents = await _consentService.GetPatientConsentsAsync(patientDid);
        return Ok(consents);
    }

    [HttpGet("{consentId}/history")]
    [ProducesResponseType(typeof(List<ConsentRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConsentRecord>>> GetConsentHistoryAsync(string consentId)
    {
        var history = await _consentService.GetConsentHistoryAsync(consentId);
        return Ok(history);
    }
}