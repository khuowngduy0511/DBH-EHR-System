using DBH.Consent.Service.DTOs;
using DBH.Consent.Service.Models.Enums;
using DBH.Consent.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Consent.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsentsController : ControllerBase
{
    private readonly IConsentService _consentService;

    public ConsentsController(IConsentService consentService)
    {
        _consentService = consentService;
    }

    // =========================================================================
    // CONSENT ENDPOINTS
    // =========================================================================

    /// <summary>
    /// Grant consent for EHR access (patient action)
    /// Creates blockchain record
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> GrantConsent([FromBody] GrantConsentRequest request)
    {
        // TODO: Verify request.PatientId matches authenticated user
        var result = await _consentService.GrantConsentAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetConsent), new { id = result.Data!.ConsentId }, result);
    }

    /// <summary>
    /// Get consent by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetConsent(Guid id)
    {
        var result = await _consentService.GetConsentByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Get consents granted by a patient
    /// </summary>
    [HttpGet("by-patient/{patientId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetConsentsByPatient(
        Guid patientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // TODO: Verify patientId matches authenticated user or admin
        var result = await _consentService.GetConsentsByPatientAsync(patientId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get consents granted to a grantee (doctor/org)
    /// </summary>
    [HttpGet("by-grantee/{granteeId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetConsentsByGrantee(
        Guid granteeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // TODO: Verify granteeId matches authenticated user or admin
        var result = await _consentService.GetConsentsByGranteeAsync(granteeId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Search consents with filters
    /// </summary>
    [HttpGet("search")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<IActionResult> SearchConsents([FromQuery] ConsentQueryParams query)
    {
        var result = await _consentService.SearchConsentsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Revoke consent (patient action)
    /// Updates blockchain record
    /// </summary>
    [HttpPost("{id:guid}/revoke")]
    [Authorize]
    public async Task<IActionResult> RevokeConsent(Guid id, [FromBody] RevokeConsentRequest request)
    {
        // TODO: Verify authenticated user is the patient who granted this consent
        var result = await _consentService.RevokeConsentAsync(id, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Verify if a grantee has consent to access patient's EHR
    /// Used by EHR Service before returning records
    /// </summary>
    [HttpPost("verify")]
    [Authorize]
    public async Task<IActionResult> VerifyConsent([FromBody] VerifyConsentRequest request)
    {
        var result = await _consentService.VerifyConsentAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Sync consent from blockchain (admin action)
    /// </summary>
    [HttpPost("sync/{blockchainConsentId}")]
    [Authorize(Roles = "Admin,SystemAdmin")]
    public async Task<IActionResult> SyncFromBlockchain(string blockchainConsentId)
    {
        var result = await _consentService.SyncFromBlockchainAsync(blockchainConsentId);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }
}

[ApiController]
[Route("api/access-requests")]
public class AccessRequestsController : ControllerBase
{
    private readonly IConsentService _consentService;

    public AccessRequestsController(IConsentService consentService)
    {
        _consentService = consentService;
    }

    // =========================================================================
    // ACCESS REQUEST ENDPOINTS
    // =========================================================================

    /// <summary>
    /// Create access request (doctor/org requests patient consent)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor,Nurse,OrgAdmin")]
    public async Task<IActionResult> CreateAccessRequest([FromBody] CreateAccessRequestDto request)
    {
        // TODO: Verify request.RequesterId matches authenticated user
        var result = await _consentService.CreateAccessRequestAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetAccessRequest), new { id = result.Data!.RequestId }, result);
    }

    /// <summary>
    /// Get access request by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetAccessRequest(Guid id)
    {
        var result = await _consentService.GetAccessRequestByIdAsync(id);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    /// <summary>
    /// Get pending access requests for a patient
    /// </summary>
    [HttpGet("by-patient/{patientId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetAccessRequestsByPatient(
        Guid patientId,
        [FromQuery] AccessRequestStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // TODO: Verify patientId matches authenticated user
        var result = await _consentService.GetAccessRequestsByPatientAsync(patientId, status, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get access requests made by a requester
    /// </summary>
    [HttpGet("by-requester/{requesterId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetAccessRequestsByRequester(
        Guid requesterId,
        [FromQuery] AccessRequestStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // TODO: Verify requesterId matches authenticated user
        var result = await _consentService.GetAccessRequestsByRequesterAsync(requesterId, status, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Respond to access request (patient approves/denies)
    /// </summary>
    [HttpPost("{id:guid}/respond")]
    [Authorize]
    public async Task<IActionResult> RespondToAccessRequest(
        Guid id, 
        [FromBody] RespondAccessRequestDto response)
    {
        // TODO: Verify authenticated user is the patient who received this request
        var result = await _consentService.RespondToAccessRequestAsync(id, response);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Cancel access request (requester cancels their own request)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> CancelAccessRequest(Guid id)
    {
        // TODO: Verify authenticated user is the requester
        var result = await _consentService.CancelAccessRequestAsync(id);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
