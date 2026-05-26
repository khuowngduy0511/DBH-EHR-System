using DBH.Shared.Contracts.Blockchain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

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

    /// <summary>Enqueue Fabric CA enrollment job</summary>
    void EnqueueFabricCaEnrollment(
        string enrollmentId,
        string username,
        string role,
        Func<string, Task>? onFailure = null);

    // ─── Direct commit methods (connection check + retry + DLQ) ───
    // These are called during an HTTP request so the result is returned in the API response.

    /// <summary>
    /// Directly commits an EHR hash to blockchain with connection check and retry (3 attempts).
    /// On failure, the job is automatically moved to the dead-letter queue for later replay.
    /// Returns (Success, Message) so callers can surface blockchain status in API responses.
    /// </summary>
    Task<(bool Success, string? Message)> TryCommitEhrHashAsync(EhrHashRecord record);

    /// <summary>
    /// Directly commits a consent grant to blockchain with connection check and retry.
    /// </summary>
    Task<(bool Success, string? Message)> TryCommitConsentGrantAsync(ConsentRecord record);

    /// <summary>
    /// Directly commits a consent revoke to blockchain with connection check and retry.
    /// </summary>
    Task<(bool Success, string? Message)> TryCommitConsentRevokeAsync(string consentId, string revokedAt, string? reason);

    /// <summary>
    /// Directly commits an audit entry to blockchain with connection check and retry.
    /// </summary>
    Task<(bool Success, string? Message)> TryCommitAuditEntryAsync(AuditEntry entry);

    /// <summary>Kiểm tra queue size</summary>
    int PendingCount { get; }
    
    /// <summary>
    /// Timestamp when an emergency move of queued messages to DLQ was performed (or null).
    /// Services can read this to surface status to callers.
    /// </summary>
    DateTimeOffset? LastEmergencyDlqMove { get; }

    /// <summary>
    /// Notify the sync service that an emergency DLQ move occurred at the specified time.
    /// </summary>
    void NotifyEmergencyDlqMove(DateTimeOffset when);
    
    /// <summary>
    /// Clear the emergency DLQ move timestamp (called when blockchain is back online).
    /// </summary>
    void ClearEmergencyDlqMove();
}

/// <summary>
/// Implementation: Helper cho enqueuing blockchain sync jobs
/// </summary>
public class BlockchainSyncService : IBlockchainSyncService
{
    private DateTimeOffset? _lastEmergencyDlqMove;
    private readonly BlockchainSyncQueue _queue;
    private readonly FabricOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BlockchainSyncService> _logger;

    public BlockchainSyncService(
        BlockchainSyncQueue queue,
        IOptions<FabricOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<BlockchainSyncService> logger)
    {
        _queue = queue;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public int PendingCount => _queue.Count;

    public DateTimeOffset? LastEmergencyDlqMove => _lastEmergencyDlqMove;

    public void NotifyEmergencyDlqMove(DateTimeOffset when)
    {
        _lastEmergencyDlqMove = when;
    }

    public void ClearEmergencyDlqMove()
    {
        _lastEmergencyDlqMove = null;
    }

    // ─── Fire-and-forget Enqueue methods ───

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

    public void EnqueueFabricCaEnrollment(
        string enrollmentId,
        string username,
        string role,
        Func<string, Task>? onFailure = null)
    {
        var payload = new FabricCaEnrollPayload
        {
            EnrollmentId = enrollmentId,
            Username = username,
            Role = role
        };

        _queue.Enqueue(new BlockchainSyncJob
        {
            JobType = BlockchainSyncJobType.FabricCaEnrollment,
            EntityId = enrollmentId,
            PayloadJson = JsonSerializer.Serialize(payload),
            OnFailureCallback = onFailure
        });

        _logger.LogInformation(
            "Enqueued Fabric CA enrollment: EnrollmentId={EnrollmentId}, Role={Role}",
            enrollmentId, role);
    }

    // ─── Direct commit methods: connection check + retry + DLQ ───
    // All share the same core logic via TryCommitDirectAsync.

    /// <inheritdoc />
    public Task<(bool Success, string? Message)> TryCommitEhrHashAsync(EhrHashRecord record)
    {
        return TryCommitDirectAsync(
            entityId: record.EhrId,
            commitFunc: async sp =>
            {
                var svc = sp.GetRequiredService<IEhrBlockchainService>();
                return await svc.CommitEhrHashAsync(record);
            },
            enqueueFallback: () => EnqueueEhrHash(record,
                onFailure: err =>
                {
                    _logger.LogWarning("DLQ EHR hash for {EhrId}: {Error}", record.EhrId, err);
                    return Task.CompletedTask;
                })
        );
    }

    /// <inheritdoc />
    public Task<(bool Success, string? Message)> TryCommitConsentGrantAsync(ConsentRecord record)
    {
        return TryCommitDirectAsync(
            entityId: record.ConsentId,
            commitFunc: async sp =>
            {
                var svc = sp.GetRequiredService<IConsentBlockchainService>();
                return await svc.GrantConsentAsync(record);
            },
            enqueueFallback: () => EnqueueConsentGrant(record,
                onFailure: err =>
                {
                    _logger.LogWarning("DLQ consent grant for {ConsentId}: {Error}", record.ConsentId, err);
                    return Task.CompletedTask;
                })
        );
    }

    /// <inheritdoc />
    public Task<(bool Success, string? Message)> TryCommitConsentRevokeAsync(string consentId, string revokedAt, string? reason)
    {
        return TryCommitDirectAsync(
            entityId: consentId,
            commitFunc: async sp =>
            {
                var svc = sp.GetRequiredService<IConsentBlockchainService>();
                return await svc.RevokeConsentAsync(consentId, revokedAt, reason);
            },
            enqueueFallback: () => EnqueueConsentRevoke(consentId, revokedAt, reason,
                onFailure: err =>
                {
                    _logger.LogWarning("DLQ consent revoke for {ConsentId}: {Error}", consentId, err);
                    return Task.CompletedTask;
                })
        );
    }

    /// <inheritdoc />
    public Task<(bool Success, string? Message)> TryCommitAuditEntryAsync(AuditEntry entry)
    {
        return TryCommitDirectAsync(
            entityId: entry.AuditId,
            commitFunc: async sp =>
            {
                var svc = sp.GetRequiredService<IAuditBlockchainService>();
                return await svc.CommitAuditEntryAsync(entry);
            },
            enqueueFallback: () => EnqueueAuditEntry(entry,
                onFailure: err =>
                {
                    _logger.LogWarning("DLQ audit entry for {AuditId}: {Error}", entry.AuditId, err);
                    return Task.CompletedTask;
                })
        );
    }

    // ─── Generic core logic: shared by all TryCommit* methods ───

    /// <summary>
    /// Core logic: check blockchain connection → commit with 3 retries → DLQ on failure.
    /// Every TryCommit* method delegates here so the logic is written ONCE.
    /// </summary>
    private async Task<(bool Success, string? Message)> TryCommitDirectAsync(
        string entityId,
        Func<IServiceProvider, Task<BlockchainTransactionResult>> commitFunc,
        Action enqueueFallback)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Blockchain disabled — skipping commit for {EntityId}", entityId);
            return (true, null);
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var fabricGateway = scope.ServiceProvider.GetService<IFabricGateway>();

        if (fabricGateway == null)
        {
            _logger.LogDebug("IFabricGateway not registered. Skipping commit for {EntityId}", entityId);
            return (true, null);
        }

        // Step 1: Check blockchain connection (wait up to 5 seconds)
        bool connected = false;
        try
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline)
            {
                if (await fabricGateway.IsConnectedAsync())
                {
                    connected = true;
                    break;
                }
                await Task.Delay(1000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking blockchain connection for {EntityId}", entityId);
        }

        if (!connected)
        {
            _logger.LogWarning("FALLBACK TRIGGERED: Blockchain not connected for {EntityId}. Moving to dead-letter queue.", entityId);
            enqueueFallback();
            return (false, "Blockchain chưa kết nối được. Dữ liệu đã được lưu, blockchain sẽ được đồng bộ sau khi service khởi động lại.");
        }

        // Step 2: Commit with up to 3 attempts
        const int maxAttempts = 3;
        string? lastErrorFull = null;
        string? lastErrorShort = null;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "Committing to blockchain: EntityId={EntityId}, attempt {Attempt}/{MaxAttempts}",
                    entityId, attempt, maxAttempts);

                var result = await commitFunc(scope.ServiceProvider);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Blockchain commit succeeded: EntityId={EntityId}, TxHash={TxHash}, Block={Block}",
                        entityId, result.TxHash, result.BlockNumber);
                    return (true, null);
                }

                var fullError = result.ErrorMessage ?? "Unknown blockchain commit error";
                lastErrorFull = fullError;
                lastErrorShort = ExtractStatusDetail(fullError) ?? fullError;

                _logger.LogWarning(
                    "Blockchain commit failed for {EntityId} (attempt {Attempt}/{MaxAttempts}): {Error}",
                    entityId, attempt, maxAttempts, lastErrorFull);
            }
            catch (Exception ex)
            {
                lastErrorFull = ex.Message;
                lastErrorShort = ex.Message;
                _logger.LogWarning(ex,
                    "Blockchain commit exception for {EntityId} (attempt {Attempt}/{MaxAttempts})",
                    entityId, attempt, maxAttempts);
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(1000 * attempt);
            }
        }

        // All attempts failed → enqueue to DLQ
        _logger.LogError(
            "FALLBACK TRIGGERED: Blockchain commit PERMANENTLY FAILED after {MaxAttempts} attempts for {EntityId}: {Error}. Moving to dead-letter queue.",
            maxAttempts, entityId, lastErrorFull);

        enqueueFallback();

        return (false, $"Blockchain commit thất bại sau {maxAttempts} lần thử: {lastErrorShort}. Dữ liệu đã được lưu, blockchain sẽ được đồng bộ lại sau.");
    }

    private static string? ExtractStatusDetail(string? statusString)
    {
        if (string.IsNullOrEmpty(statusString)) return null;

        // Try common patterns: Detail="..." or "detail": "..."
        var m = Regex.Match(statusString, "Detail\\s*=\\s*\"(?<d>.*?)\"");
        if (m.Success) return m.Groups["d"].Value;

        m = Regex.Match(statusString, "\"detail\"\\s*[:=]\\s*\"(?<d>.*?)\"", RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups["d"].Value;

        m = Regex.Match(statusString, "Detail\\s*[:=]\\s*(?<d>[^,\\)]+)");
        if (m.Success) return m.Groups["d"].Value.Trim().Trim('"');

        return null;
    }
}
