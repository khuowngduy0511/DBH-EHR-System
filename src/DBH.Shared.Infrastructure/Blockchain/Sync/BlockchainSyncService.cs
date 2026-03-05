using DBH.Shared.Contracts.Blockchain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DBH.Shared.Infrastructure.Blockchain.Sync;

/// <summary>
/// Helper service để dễ dàng enqueue blockchain sync jobs từ các service khác.
/// Inject IBlockchainSyncService và gọi các method để schedule sync.
/// </summary>
public interface IBlockchainSyncService
{
    /// <summary>Enqueue EHR hash commit job</summary>
    void EnqueueEhrHash(
        EhrHashRecord record,
        Func<BlockchainTransactionResult, Task>? onSuccess = null,
        Func<string, Task>? onFailure = null);

    /// <summary>Enqueue consent grant job</summary>
    void EnqueueConsentGrant(
        ConsentRecord record,
        Func<BlockchainTransactionResult, Task>? onSuccess = null,
        Func<string, Task>? onFailure = null);

    /// <summary>Enqueue consent revoke job</summary>
    void EnqueueConsentRevoke(
        string consentId, string revokedAt, string? reason,
        Func<BlockchainTransactionResult, Task>? onSuccess = null,
        Func<string, Task>? onFailure = null);

    /// <summary>Enqueue audit entry job</summary>
    void EnqueueAuditEntry(
        AuditEntry entry,
        Func<BlockchainTransactionResult, Task>? onSuccess = null,
        Func<string, Task>? onFailure = null);

    /// <summary>Kiểm tra queue size</summary>
    int PendingCount { get; }
}

/// <summary>
/// Implementation: Helper cho enqueuing blockchain sync jobs
/// </summary>
public class BlockchainSyncService : IBlockchainSyncService
{
    private readonly BlockchainSyncQueue _queue;
    private readonly FabricOptions _options;
    private readonly ILogger<BlockchainSyncService> _logger;

    public BlockchainSyncService(
        BlockchainSyncQueue queue,
        IOptions<FabricOptions> options,
        ILogger<BlockchainSyncService> logger)
    {
        _queue = queue;
        _options = options.Value;
        _logger = logger;
    }

    public int PendingCount => _queue.Count;

    public void EnqueueEhrHash(
        EhrHashRecord record,
        Func<BlockchainTransactionResult, Task>? onSuccess = null,
        Func<string, Task>? onFailure = null)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Blockchain disabled - skipping EHR hash commit for {EhrId}", record.EhrId);
            return;
        }

        _queue.Enqueue(new BlockchainSyncJob
        {
            JobType = BlockchainSyncJobType.EhrHash,
            EntityId = record.EhrId,
            PayloadJson = JsonSerializer.Serialize(record),
            OnSuccessCallback = onSuccess,
            OnFailureCallback = onFailure
        });

        _logger.LogInformation("Enqueued EHR hash sync: EhrId={EhrId}, Version={Version}",
            record.EhrId, record.Version);
    }

    public void EnqueueConsentGrant(
        ConsentRecord record,
        Func<BlockchainTransactionResult, Task>? onSuccess = null,
        Func<string, Task>? onFailure = null)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Blockchain disabled - skipping consent grant for {ConsentId}", record.ConsentId);
            return;
        }

        _queue.Enqueue(new BlockchainSyncJob
        {
            JobType = BlockchainSyncJobType.ConsentGrant,
            EntityId = record.ConsentId,
            PayloadJson = JsonSerializer.Serialize(record),
            OnSuccessCallback = onSuccess,
            OnFailureCallback = onFailure
        });

        _logger.LogInformation("Enqueued consent grant sync: ConsentId={ConsentId}", record.ConsentId);
    }

    public void EnqueueConsentRevoke(
        string consentId, string revokedAt, string? reason,
        Func<BlockchainTransactionResult, Task>? onSuccess = null,
        Func<string, Task>? onFailure = null)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Blockchain disabled - skipping consent revoke for {ConsentId}", consentId);
            return;
        }

        var payload = new ConsentRevokePayload
        {
            ConsentId = consentId,
            RevokedAt = revokedAt,
            Reason = reason
        };

        _queue.Enqueue(new BlockchainSyncJob
        {
            JobType = BlockchainSyncJobType.ConsentRevoke,
            EntityId = consentId,
            PayloadJson = JsonSerializer.Serialize(payload),
            OnSuccessCallback = onSuccess,
            OnFailureCallback = onFailure
        });

        _logger.LogInformation("Enqueued consent revoke sync: ConsentId={ConsentId}", consentId);
    }

    public void EnqueueAuditEntry(
        AuditEntry entry,
        Func<BlockchainTransactionResult, Task>? onSuccess = null,
        Func<string, Task>? onFailure = null)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Blockchain disabled - skipping audit entry for {AuditId}", entry.AuditId);
            return;
        }

        _queue.Enqueue(new BlockchainSyncJob
        {
            JobType = BlockchainSyncJobType.AuditLog,
            EntityId = entry.AuditId,
            PayloadJson = JsonSerializer.Serialize(entry),
            OnSuccessCallback = onSuccess,
            OnFailureCallback = onFailure
        });

        _logger.LogInformation("Enqueued audit sync: AuditId={AuditId}, Action={Action}",
            entry.AuditId, entry.Action);
    }
}
