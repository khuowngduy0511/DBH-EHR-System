using DBH.Shared.Contracts.Blockchain;
using System.Text.Json.Serialization;

namespace DBH.Shared.Infrastructure.Blockchain.Sync;

/// <summary>
/// Describes the kind of blockchain work a queued job represents.
/// </summary>
public enum BlockchainSyncJobType
{
    EhrHash,
    ConsentGrant,
    ConsentRevoke,
    AuditLog,
    FabricCaEnrollment
}

/// <summary>
/// A blockchain sync job stored in RabbitMQ.
/// </summary>
public class BlockchainSyncJob
{
    public string JobId { get; init; } = Guid.NewGuid().ToString();
    public BlockchainSyncJobType JobType { get; init; }
    public string EntityId { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Called after the blockchain transaction succeeds so the caller can update the DB state.
    /// </summary>
    [JsonIgnore]
    public Func<BlockchainTransactionResult, Task>? OnSuccessCallback { get; init; }

    /// <summary>
    /// Called after the job is permanently moved to the DLQ.
    /// </summary>
    [JsonIgnore]
    public Func<string, Task>? OnFailureCallback { get; init; }
}

/// <summary>
/// Represents the dequeued RabbitMQ message and its retry metadata.
/// </summary>
public class BlockchainSyncDequeuedItem
{
    public BlockchainSyncJob Job { get; init; } = new();
    public ulong DeliveryTag { get; init; }
    public int RetryCount { get; init; }
}

/// <summary>
/// Payload used when a consent is revoked.
/// </summary>
public class ConsentRevokePayload
{
    public string ConsentId { get; set; } = string.Empty;
    public string RevokedAt { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>
/// Payload used for Fabric CA enrollment jobs.
/// </summary>
public class FabricCaEnrollPayload
{
    public string EnrollmentId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}