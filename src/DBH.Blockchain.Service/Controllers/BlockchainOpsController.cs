using DBH.Blockchain.Service.DTOs;
using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DBH.Blockchain.Service.Controllers;

[ApiController]
[Route("api/v1/blockchain")]
[Produces("application/json")]
[Authorize]
public class BlockchainOpsController : ControllerBase
{
    private readonly IEmergencyBlockchainService _emergencyService;
    private readonly IFabricCaService _fabricCaService;
    private readonly IFabricGateway _fabricGateway;
    private readonly BlockchainSyncQueue _syncQueue;
    private readonly ILogger<BlockchainOpsController> _logger;

    public BlockchainOpsController(
        IEmergencyBlockchainService emergencyService,
        IFabricCaService fabricCaService,
        IFabricGateway fabricGateway,
        BlockchainSyncQueue syncQueue,
        ILogger<BlockchainOpsController> logger)
    {
        _emergencyService = emergencyService;
        _fabricCaService = fabricCaService;
        _fabricGateway = fabricGateway;
        _syncQueue = syncQueue;
        _logger = logger;
    }

    [HttpPost("error-logs")]
    [ProducesResponseType(typeof(ErrorLogResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ErrorLogResponseDto>> LogErrorAsync([FromBody] ErrorLogDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            _logger.LogWarning(
                "Logging error to blockchain: {ErrorId} from {ServiceName} - {ErrorMessage}",
                request.ErrorId, request.ServiceName, request.ErrorMessage);

            var errorRecord = new EmergencyAccessRecord
            {
                LogId = request.ErrorId,
                TargetRecordDid = request.ServiceName,
                AccessorDid = request.UserId ?? "SYSTEM",
                AccessorOrg = request.ServiceName,
                Reason = $"[{request.Severity}] {request.ErrorMessage}",
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            var result = await _emergencyService.EmergencyAccessAsync(errorRecord);

            var response = new ErrorLogResponseDto
            {
                Success = result.Success,
                ErrorId = request.ErrorId,
                TransactionId = result.TxHash,
                Message = result.Success
                    ? "Error logged to blockchain successfully"
                    : $"Failed to log error: {result.ErrorMessage}",
                LoggedAt = DateTime.UtcNow
            };

            return CreatedAtAction(nameof(GetErrorLogAsync),
                new { errorId = request.ErrorId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging to blockchain: {ErrorId}", request.ErrorId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorLogResponseDto
                {
                    Success = false,
                    ErrorId = request.ErrorId,
                    Message = $"Failed to log error: {ex.Message}"
                });
        }
    }

    [HttpGet("error-logs/{errorId}")]
    [ProducesResponseType(typeof(EmergencyAccessLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmergencyAccessLogDto>> GetErrorLogAsync(string errorId)
    {
        try
        {
            _logger.LogInformation("Retrieving error log: {ErrorId}", errorId);

            var records = await _emergencyService.GetAllEmergencyAccessAsync();
            var errorRecord = records.FirstOrDefault(r => r.LogId == errorId);

            if (errorRecord == null)
                return NotFound(new { message = $"Error log not found: {errorId}" });

            return Ok(new EmergencyAccessLogDto
            {
                LogId = errorRecord.LogId,
                TargetRecordDid = errorRecord.TargetRecordDid,
                AccessorDid = errorRecord.AccessorDid,
                AccessorOrg = errorRecord.AccessorOrg,
                Reason = errorRecord.Reason,
                Timestamp = ParseTimestamp(errorRecord.Timestamp)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving error log: {ErrorId}", errorId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"Failed to retrieve error log: {ex.Message}" });
        }
    }

    [HttpPost("emergency-access")]
    [Authorize(Roles = "Doctor,Admin,Emergency")]
    [ProducesResponseType(typeof(EmergencyAccessResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EmergencyAccessResponseDto>> RecordEmergencyAccessAsync(
        [FromBody] EmergencyAccessRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            _logger.LogWarning(
                "Recording emergency access - Record: {RecordDid}, Accessor: {AccessorDid}, Reason: {Reason}",
                request.TargetRecordDid, request.AccessorDid, request.Reason);

            var record = new EmergencyAccessRecord
            {
                LogId = Guid.NewGuid().ToString(),
                TargetRecordDid = request.TargetRecordDid,
                AccessorDid = request.AccessorDid,
                AccessorOrg = request.AccessorOrg,
                Reason = request.Reason,
                Timestamp = DateTime.UtcNow.ToString("o")
            };

            var result = await _emergencyService.EmergencyAccessAsync(record);

            var response = new EmergencyAccessResponseDto
            {
                Success = result.Success,
                LogId = record.LogId,
                TransactionId = result.TxHash,
                Message = result.Success
                    ? "Emergency access recorded to blockchain"
                    : $"Failed to record: {result.ErrorMessage}",
                AccessedAt = ParseTimestamp(record.Timestamp)
            };

            return CreatedAtAction(nameof(GetEmergencyAccessByRecordAsync),
                new { recordDid = request.TargetRecordDid }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording emergency access");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new EmergencyAccessResponseDto
                {
                    Success = false,
                    Message = $"Failed to record emergency access: {ex.Message}"
                });
        }
    }

    [HttpGet("emergency-access/record/{recordDid}")]
    [ProducesResponseType(typeof(List<EmergencyAccessLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EmergencyAccessLogDto>>> GetEmergencyAccessByRecordAsync(string recordDid)
    {
        try
        {
            _logger.LogInformation("Querying emergency access for record: {RecordDid}", recordDid);

            var records = await _emergencyService.GetEmergencyAccessByRecordAsync(recordDid);
            var response = records.Select(r => new EmergencyAccessLogDto
            {
                LogId = r.LogId,
                TargetRecordDid = r.TargetRecordDid,
                AccessorDid = r.AccessorDid,
                AccessorOrg = r.AccessorOrg,
                Reason = r.Reason,
                Timestamp = ParseTimestamp(r.Timestamp)
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying emergency access for record: {RecordDid}", recordDid);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"Failed to query emergency access: {ex.Message}" });
        }
    }

    [HttpGet("emergency-access/accessor/{accessorDid}")]
    [ProducesResponseType(typeof(List<EmergencyAccessLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EmergencyAccessLogDto>>> GetEmergencyAccessByAccessorAsync(string accessorDid)
    {
        try
        {
            _logger.LogInformation("Querying emergency access by accessor: {AccessorDid}", accessorDid);

            var records = await _emergencyService.GetEmergencyAccessByAccessorAsync(accessorDid);
            var response = records.Select(r => new EmergencyAccessLogDto
            {
                LogId = r.LogId,
                TargetRecordDid = r.TargetRecordDid,
                AccessorDid = r.AccessorDid,
                AccessorOrg = r.AccessorOrg,
                Reason = r.Reason,
                Timestamp = ParseTimestamp(r.Timestamp)
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying emergency access by accessor: {AccessorDid}", accessorDid);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"Failed to query emergency access: {ex.Message}" });
        }
    }

    [HttpGet("emergency-access")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<EmergencyAccessLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EmergencyAccessLogDto>>> GetAllEmergencyAccessAsync()
    {
        try
        {
            _logger.LogInformation("Querying all emergency access logs");

            var records = await _emergencyService.GetAllEmergencyAccessAsync();
            var response = records.Select(r => new EmergencyAccessLogDto
            {
                LogId = r.LogId,
                TargetRecordDid = r.TargetRecordDid,
                AccessorDid = r.AccessorDid,
                AccessorOrg = r.AccessorOrg,
                Reason = r.Reason,
                Timestamp = ParseTimestamp(r.Timestamp)
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying all emergency access logs");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"Failed to query emergency access: {ex.Message}" });
        }
    }

    [HttpPost("accounts")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(BlockchainAccountResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BlockchainAccountResponseDto>> CreateBlockchainAccountAsync(
        [FromBody] CreateBlockchainAccountDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            _logger.LogInformation(
                "Creating blockchain account - EnrollmentId: {EnrollmentId}, Username: {Username}, Role: {Role}",
                request.EnrollmentId, request.Username, request.Role);

            var enrollResult = await _fabricCaService.EnrollUserAsync(
                request.EnrollmentId,
                request.Username,
                request.Role);

            var response = new BlockchainAccountResponseDto
            {
                Success = enrollResult.Success,
                EnrollmentId = enrollResult.EnrollmentId,
                EnrollmentSecret = enrollResult.EnrollmentSecret,
                AccountStoragePath = enrollResult.AccountStoragePath,
                Message = enrollResult.Success
                    ? "Blockchain account created successfully. Save the enrollment secret securely!"
                    : $"Failed to create account: {enrollResult.ErrorMessage}",
                CreatedAt = DateTime.UtcNow
            };

            return CreatedAtAction(nameof(GetBlockchainAccountAsync),
                new { enrollmentId = request.EnrollmentId }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating blockchain account: {EnrollmentId}", request.EnrollmentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new BlockchainAccountResponseDto
                {
                    Success = false,
                    EnrollmentId = request.EnrollmentId,
                    Message = $"Failed to create account: {ex.Message}"
                });
        }
    }

    [HttpGet("accounts/{enrollmentId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<object> GetBlockchainAccountAsync(string enrollmentId)
    {
        _logger.LogInformation("Retrieving blockchain account: {EnrollmentId}", enrollmentId);

        return Ok(new
        {
            enrollmentId,
            message = "To re-enroll, use the login endpoint with the saved enrollment secret",
            note = "Blockchain accounts are stored in Fabric MSP directories on the peer"
        });
    }

    [HttpPost("accounts/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BlockchainAccountResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BlockchainAccountResponseDto>> LoginBlockchainAccountAsync(
        [FromBody] BlockchainAccountLoginDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrEmpty(request.EnrollmentSecret))
            return Unauthorized(new
            {
                message = "Enrollment secret is required for login"
            });

        try
        {
            _logger.LogInformation(
                "Logging in to blockchain account - EnrollmentId: {EnrollmentId}",
                request.EnrollmentId);

            var enrollResult = await _fabricCaService.EnrollUserAsync(
                request.EnrollmentId,
                request.Username,
                request.Role,
                request.EnrollmentSecret);

            var response = new BlockchainAccountResponseDto
            {
                Success = enrollResult.Success,
                EnrollmentId = enrollResult.EnrollmentId,
                EnrollmentSecret = enrollResult.EnrollmentSecret,
                AccountStoragePath = enrollResult.AccountStoragePath,
                Message = enrollResult.Success
                    ? "Successfully logged in to blockchain account"
                    : $"Failed to login: {enrollResult.ErrorMessage}",
                CreatedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in to blockchain account: {EnrollmentId}", request.EnrollmentId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new BlockchainAccountResponseDto
                {
                    Success = false,
                    EnrollmentId = request.EnrollmentId,
                    Message = $"Failed to login: {ex.Message}"
                });
        }
    }

    [HttpGet("connection")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Health()
    {
        var connected = await _fabricGateway.IsConnectedAsync();
        return Ok(new
        {
            status = "healthy",
            service = "Blockchain API",
            fabricConnected = connected
        });
    }

    /// <summary>
    /// Get list of dead-letter messages (admin only, filtered by organization from JWT)
    /// </summary>
    [HttpGet("deadletters")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DeadLetterListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<DeadLetterListResponseDto> GetDeadLetters()
    {
        try
        {
            // Extract organization ID from JWT token
            var organizationId = User.FindFirstValue(ClaimTypes.GroupSid);
            if (string.IsNullOrWhiteSpace(organizationId))
            {
                _logger.LogWarning("GetDeadLetters called but organization claim is missing in token");
                return Forbid();
            }

            // Get all dead-letter messages
            var deadLetters = _syncQueue.GetDeadLetters();
            
            _logger.LogInformation(
                "Retrieved {Count} dead-letter messages for organization {OrgId}",
                deadLetters.Count, organizationId);

            // Map to DTOs with organization filtering
            var deadLetterDtos = deadLetters.Select(dl => new DeadLetterDto
            {
                JobId = dl.Job.JobId,
                JobType = dl.Job.JobType.ToString(),
                EntityId = dl.Job.EntityId,
                Attempts = dl.RetryCount,
                ErrorMessage = dl.ErrorMessage,
                CreatedAt = dl.Job.CreatedAt,
                PayloadJson = dl.Job.PayloadJson,
                OrganizationId = organizationId // Attach the requesting org's ID
            }).ToList();

            var response = new DeadLetterListResponseDto
            {
                DeadLetters = deadLetterDtos,
                TotalCount = deadLetterDtos.Count
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dead-letter messages");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = $"Failed to retrieve dead-letter messages: {ex.Message}" });
        }
    }

    /// <summary>
    /// Requeue dead-letter messages back to the main queue (admin only)
    /// </summary>
    [HttpPost("deadletters/requeue")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RequeueDeadLetterResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RequeueDeadLetterResponseDto>> RequeueDeadLetters(
        [FromBody] RequeueDeadLetterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract organization ID from JWT token
            var organizationId = User.FindFirstValue(ClaimTypes.GroupSid);
            if (string.IsNullOrWhiteSpace(organizationId))
            {
                _logger.LogWarning("RequeueDeadLetters called but organization claim is missing in token");
                return Forbid();
            }

            // Get current dead-letters before requeuing to track which ones were requeued
            var beforeRequeue = _syncQueue.GetDeadLetters();
            var beforeJobIds = beforeRequeue.Select(dl => dl.Job.JobId).ToHashSet();

            // Requeue from dead-letter queue
            await _syncQueue.RequeueFromDeadLetterAsync(request.JobId, cancellationToken);

            // Get dead-letters after requeuing to determine what was requeued
            var afterRequeue = _syncQueue.GetDeadLetters();
            var afterJobIds = afterRequeue.Select(dl => dl.Job.JobId).ToHashSet();

            // Jobs that were requeued are the ones that were in beforeJobIds but not in afterJobIds
            var requeuedJobIds = beforeJobIds.Where(id => !afterJobIds.Contains(id)).ToList();

            _logger.LogInformation(
                "Requeued {Count} dead-letter message(s) for organization {OrgId}. JobId filter: {JobIdFilter}",
                requeuedJobIds.Count, organizationId, request.JobId ?? "all");

            var response = new RequeueDeadLetterResponseDto
            {
                Success = true,
                Message = request.JobId != null
                    ? $"Thành công requeued (JobId: {request.JobId})"
                    : $"Thành công requeued {requeuedJobIds.Count} message(s) từ error queue",
                RequeuedCount = requeuedJobIds.Count,
                RequeuedJobIds = requeuedJobIds,
                OperatedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requeuing dead-letter messages");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new RequeueDeadLetterResponseDto
                {
                    Success = false,
                    Message = $"Failed to requeue dead-letter messages: {ex.Message}",
                    OperatedAt = DateTime.UtcNow
                });
        }
    }

    /// <summary>
    /// Check if blockchain network is running and ready
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BlockchainStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BlockchainStatusResponseDto>> CheckBlockchainStatus()
    {
        try
        {
            // Check if blockchain gateway is connected
            var isConnected = await _fabricGateway.IsConnectedAsync();
            
            // Get queue statistics
            var queuedJobs = _syncQueue.GetTotalQueuedJobs();
            var deadLetterCount = _syncQueue.DeadLetterCount;

            // Per-queue and per-type breakdowns
            var queuedByQueue = _syncQueue.GetQueuedJobsByQueue() ?? new Dictionary<string, int>();
            var deadLetterByType = _syncQueue.GetDeadLetterCountsByJobType() ?? new Dictionary<string, int>();
            var deadLetterByQueue = _syncQueue.GetDeadLetterCountsByOriginQueue() ?? new Dictionary<string, int>();

            _logger.LogInformation(
                "Blockchain status check - Connected: {Connected}, QueuedJobs: {Queued}, DeadLetters: {DLQ}",
                isConnected, queuedJobs, deadLetterCount);

            var response = new BlockchainStatusResponseDto
            {
                IsRunning = isConnected,
                IsReady = isConnected && deadLetterCount < 100, // Consider "ready" if connected and not too many DLQ messages
                StatusMessage = isConnected
                    ? "Blockchain network đang chạy và sẵn sàng nhận yêu cầu."
                    : "Blockchain network chưa kết nối. Vui lòng kiểm tra cấu hình và trạng thái của Blockchain.",
                QueuedJobs = queuedJobs,
                DeadLetterCount = deadLetterCount,
                QueuedCountByQueue = queuedByQueue,
                DeadLetterCountByType = deadLetterByType,
                DeadLetterCountByQueue = deadLetterByQueue,
                CheckedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking blockchain status");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new BlockchainStatusResponseDto
                {
                    IsRunning = false,
                    IsReady = false,
                    StatusMessage = $"Error checking blockchain status: {ex.Message}",
                    CheckedAt = DateTime.UtcNow
                });
        }
    }

    private static DateTime ParseTimestamp(string value)
    {
        return DateTime.TryParse(value, out var parsed)
            ? parsed
            : DateTime.UtcNow;
    }
}