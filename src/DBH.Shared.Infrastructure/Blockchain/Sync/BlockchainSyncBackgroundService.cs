using DBH.Shared.Contracts.Blockchain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace DBH.Shared.Infrastructure.Blockchain.Sync;

/// <summary>
/// Background service xử lý blockchain transactions async
/// Khi service ghi data vào DB, đồng thời đẩy vào queue
/// Background worker lấy từ queue và submit lên blockchain
/// Nếu fail → retry, nếu success → update tx_hash trong DB
/// </summary>
public class BlockchainSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BlockchainSyncBackgroundService> _logger;
    private readonly FabricOptions _options;
    private readonly BlockchainSyncQueue _syncQueue;

    // Stats
    private long _totalProcessed;
    private long _totalFailed;
    private long _totalSuccess;

    public BlockchainSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BlockchainSyncBackgroundService> logger,
        IOptions<FabricOptions> options,
        BlockchainSyncQueue syncQueue)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _syncQueue = syncQueue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Blockchain sync is disabled. Background service will not process transactions.");
            return;
        }

        _logger.LogInformation("Blockchain sync background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _syncQueue.DequeueAsync(stoppingToken);

                if (job != null)
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in blockchain sync background service");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation(
            "Blockchain sync background service stopped. Total: {Processed}, Success: {Success}, Failed: {Failed}",
            _totalProcessed, _totalSuccess, _totalFailed);
    }

    private async Task ProcessJobAsync(BlockchainSyncJob job, CancellationToken ct)
    {
        Interlocked.Increment(ref _totalProcessed);

        _logger.LogInformation(
            "Processing blockchain sync job: Type={Type}, EntityId={EntityId}, Attempt={Attempt}",
            job.JobType, job.EntityId, job.Attempts + 1);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var gateway = scope.ServiceProvider.GetRequiredService<IFabricGateway>();

            BlockchainTransactionResult result;

            switch (job.JobType)
            {
                case BlockchainSyncJobType.EhrHash:
                    var ehrService = scope.ServiceProvider.GetRequiredService<IEhrBlockchainService>();
                    var ehrRecord = System.Text.Json.JsonSerializer.Deserialize<EhrHashRecord>(job.PayloadJson);
                    result = ehrRecord != null
                        ? await ehrService.CommitEhrHashAsync(ehrRecord)
                        : new BlockchainTransactionResult { Success = false, ErrorMessage = "Invalid payload" };
                    break;

                case BlockchainSyncJobType.ConsentGrant:
                    var consentService = scope.ServiceProvider.GetRequiredService<IConsentBlockchainService>();
                    var consentRecord = System.Text.Json.JsonSerializer.Deserialize<ConsentRecord>(job.PayloadJson);
                    result = consentRecord != null
                        ? await consentService.GrantConsentAsync(consentRecord)
                        : new BlockchainTransactionResult { Success = false, ErrorMessage = "Invalid payload" };
                    break;

                case BlockchainSyncJobType.ConsentRevoke:
                    var consentSvc = scope.ServiceProvider.GetRequiredService<IConsentBlockchainService>();
                    var revokeData = System.Text.Json.JsonSerializer.Deserialize<ConsentRevokePayload>(job.PayloadJson);
                    result = revokeData != null
                        ? await consentSvc.RevokeConsentAsync(revokeData.ConsentId, revokeData.RevokedAt, revokeData.Reason)
                        : new BlockchainTransactionResult { Success = false, ErrorMessage = "Invalid payload" };
                    break;

                case BlockchainSyncJobType.AuditLog:
                    var auditService = scope.ServiceProvider.GetRequiredService<IAuditBlockchainService>();
                    var auditEntry = System.Text.Json.JsonSerializer.Deserialize<AuditEntry>(job.PayloadJson);
                    result = auditEntry != null
                        ? await auditService.CommitAuditEntryAsync(auditEntry)
                        : new BlockchainTransactionResult { Success = false, ErrorMessage = "Invalid payload" };
                    break;

                default:
                    _logger.LogWarning("Unknown job type: {JobType}", job.JobType);
                    return;
            }

            if (result.Success)
            {
                Interlocked.Increment(ref _totalSuccess);

                _logger.LogInformation(
                    "Blockchain sync success: Type={Type}, EntityId={EntityId}, TxHash={TxHash}",
                    job.JobType, job.EntityId, result.TxHash);

                // Notify callback to update DB with tx_hash
                if (job.OnSuccessCallback != null)
                {
                    await job.OnSuccessCallback(result);
                }
            }
            else
            {
                await HandleFailureAsync(job, result.ErrorMessage, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception processing blockchain sync job: Type={Type}, EntityId={EntityId}",
                job.JobType, job.EntityId);

            await HandleFailureAsync(job, ex.Message, ct);
        }
    }

    private async Task HandleFailureAsync(BlockchainSyncJob job, string? errorMessage, CancellationToken ct)
    {
        job.Attempts++;
        job.LastError = errorMessage;

        if (job.Attempts < _options.MaxRetries)
        {
            var delay = _options.RetryDelayMs * (int)Math.Pow(2, job.Attempts - 1);
            _logger.LogWarning(
                "Blockchain sync failed, retrying in {Delay}ms: Type={Type}, EntityId={EntityId}, Error={Error}",
                delay, job.JobType, job.EntityId, errorMessage);

            await Task.Delay(delay, ct);
            _syncQueue.Enqueue(job);
        }
        else
        {
            Interlocked.Increment(ref _totalFailed);
            _logger.LogError(
                "Blockchain sync PERMANENTLY FAILED after {MaxRetries} attempts: Type={Type}, EntityId={EntityId}, Error={Error}",
                _options.MaxRetries, job.JobType, job.EntityId, errorMessage);

            // Notify failure callback
            if (job.OnFailureCallback != null)
            {
                await job.OnFailureCallback(errorMessage ?? "Unknown error");
            }
        }
    }
}

// ============================================================================
// Sync Queue
// ============================================================================

/// <summary>
/// Thread-safe queue cho blockchain sync jobs
/// </summary>
public class BlockchainSyncQueue
{
    private readonly ConcurrentQueue<BlockchainSyncJob> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public void Enqueue(BlockchainSyncJob job)
    {
        _queue.Enqueue(job);
        _signal.Release();
    }

    public async Task<BlockchainSyncJob?> DequeueAsync(CancellationToken ct)
    {
        await _signal.WaitAsync(ct);
        _queue.TryDequeue(out var job);
        return job;
    }

    public int Count => _queue.Count;
}

// ============================================================================
// Sync Job Models
// ============================================================================

public enum BlockchainSyncJobType
{
    EhrHash,
    ConsentGrant,
    ConsentRevoke,
    AuditLog
}

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
    /// Callback khi transaction thành công - để update tx_hash vào DB
    /// </summary>
    public Func<BlockchainTransactionResult, Task>? OnSuccessCallback { get; init; }

    /// <summary>
    /// Callback khi transaction thất bại vĩnh viễn
    /// </summary>
    public Func<string, Task>? OnFailureCallback { get; init; }
}

/// <summary>
/// Payload cho consent revoke job
/// </summary>
public class ConsentRevokePayload
{
    public string ConsentId { get; set; } = string.Empty;
    public string RevokedAt { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
