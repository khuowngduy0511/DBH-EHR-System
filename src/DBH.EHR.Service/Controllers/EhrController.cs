using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBH.EHR.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    /// <summary>
    /// Create a new EHR change request.
    /// Stores document in MongoDB (primary) and creates PENDING request in PostgreSQL (primary).
    /// Requires approval from both HospitalA and HospitalB before being applied to ehr_index.
    /// </summary>
    /// <param name="request">The EHR request containing patient info and document</param>
    /// <returns>Created change request with offchain document ID and node metadata</returns>
    [HttpPost("requests")]
    [ProducesResponseType(typeof(CreateEhrResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateEhrResponseDto>> CreateRequest([FromBody] CreateEhrRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation(
            "POST /api/ehr/requests - Creating request for patient {PatientId}",
            request.PatientId);

        var result = await _ehrService.CreateChangeRequestAsync(request);

        _logger.LogInformation(
            "Created change request {RequestId} with offchain doc {DocId}",
            result.ChangeRequestId, result.OffchainDocId);

        return CreatedAtAction(
            nameof(GetRequest),
            new { id = result.ChangeRequestId },
            result);
    }

    /// <summary>
    /// Get a change request by ID
    /// </summary>
    [HttpGet("requests/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetRequest(Guid id)
    {
        var request = await _ehrService.GetChangeRequestAsync(id);
        
        if (request == null)
        {
            return NotFound(new { Message = $"Change request {id} not found" });
        }

        return Ok(new
        {
            request.Id,
            request.PatientId,
            request.Purpose,
            request.RequestedScope,
            request.TtlMinutes,
            Status = request.Status.ToString(),
            Approvals = request.ApprovalsList,
            request.OffchainDocId,
            request.CreatedAt,
            request.UpdatedAt
        });
    }

    /// <summary>
    /// Get all change requests for a patient
    /// </summary>
    [HttpGet("requests/patient/{patientId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetRequestsByPatient(string patientId)
    {
        var requests = await _ehrService.GetChangeRequestsByPatientAsync(patientId);
        
        return Ok(requests.Select(r => new
        {
            r.Id,
            r.PatientId,
            r.Purpose,
            Status = r.Status.ToString(),
            Approvals = r.ApprovalsList,
            r.OffchainDocId,
            r.CreatedAt
        }));
    }
}
