namespace DBH.Blockchain.Service.DTOs;

/// <summary>
/// Represents a message in the dead-letter queue
/// </summary>
public class DeadLetterDto
{
    /// <summary>
    /// Unique job identifier
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Type of blockchain sync job
    /// </summary>
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID associated with the job
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Error message from the failure
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the job was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Additional payload (JSON)
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>
    /// Organization ID extracted from job or context
    /// </summary>
    public string? OrganizationId { get; set; }
}

/// <summary>
/// Response containing list of dead-letter messages
/// </summary>
public class DeadLetterListResponseDto
{
    /// <summary>
    /// List of dead-letter messages
    /// </summary>
    public List<DeadLetterDto> DeadLetters { get; set; } = new();

    /// <summary>
    /// Total count of dead-letter messages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Whether there are any dead-letter messages
    /// </summary>
    public bool HasDeadLetters => TotalCount > 0;
}

/// <summary>
/// Request to requeue a dead-letter message
/// </summary>
public class RequeueDeadLetterRequestDto
{
    /// <summary>
    /// Job ID of the message to requeue (optional - requeue all if not provided)
    /// </summary>
    public string? JobId { get; set; }
}

/// <summary>
/// Response for requeue operation
/// </summary>
public class RequeueDeadLetterResponseDto
{
    /// <summary>
    /// Whether the requeue operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Number of messages requeued
    /// </summary>
    public int RequeuedCount { get; set; }

    /// <summary>
    /// List of job IDs that were requeued
    /// </summary>
    public List<string> RequeuedJobIds { get; set; } = new();

    /// <summary>
    /// Timestamp of the operation
    /// </summary>
    public DateTime OperatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response for blockchain status check
/// </summary>
public class BlockchainStatusResponseDto
{
    /// <summary>
    /// Whether the blockchain network is running
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Whether the blockchain is ready to accept requests
    /// </summary>
    public bool IsReady { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// Number of queued blockchain sync jobs
    /// </summary>
    public int QueuedJobs { get; set; }

    /// <summary>
    /// Number of dead-letter messages
    /// </summary>
    public int DeadLetterCount { get; set; }

    /// <summary>
    /// Number of queued jobs grouped by queue name
    /// </summary>
    public Dictionary<string, int> QueuedCountByQueue { get; set; } = new();

    /// <summary>
    /// Number of dead-letter messages grouped by job type
    /// </summary>
    public Dictionary<string, int> DeadLetterCountByType { get; set; } = new();

    /// <summary>
    /// Number of dead-letter messages grouped by originating queue
    /// </summary>
    public Dictionary<string, int> DeadLetterCountByQueue { get; set; } = new();

    /// <summary>
    /// Last health check timestamp
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
