using DBH.Blockchain.Service.DTOs;
using DBH.Shared.Contracts.Blockchain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    private readonly ILogger<BlockchainOpsController> _logger;

    public BlockchainOpsController(
        IEmergencyBlockchainService emergencyService,
        IFabricCaService fabricCaService,
        IFabricGateway fabricGateway,
        ILogger<BlockchainOpsController> logger)
    {
        _emergencyService = emergencyService;
        _fabricCaService = fabricCaService;
        _fabricGateway = fabricGateway;
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


    private static DateTime ParseTimestamp(string value)
    {
        return DateTime.TryParse(value, out var parsed)
            ? parsed
            : DateTime.UtcNow;
    }
}