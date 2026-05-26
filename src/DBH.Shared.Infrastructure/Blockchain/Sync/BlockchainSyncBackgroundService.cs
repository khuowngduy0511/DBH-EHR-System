using DBH.Shared.Contracts;
using DBH.Shared.Contracts.Blockchain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DBH.Shared.Infrastructure.Blockchain.Sync;

/// <summary>
/// Background worker xử lý các job blockchain đã được đẩy vào queue.
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

    /// <summary>
    /// Lắng nghe queue blockchain và xử lý từng job theo thứ tự.
    /// Waits for blockchain to be ready before starting to process jobs.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Blockchain sync background service started. Waiting for blockchain to be ready...");

        // Wait for blockchain to be ready before processing
        await WaitForBlockchainReadinessAsync(stoppingToken);

        _logger.LogInformation("Blockchain is ready. Starting to process queued jobs.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var dequeued = await _syncQueue.DequeueAsync(stoppingToken);
                if (dequeued?.Job != null)
                {
                    await ProcessJobAsync(dequeued, stoppingToken);
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

    /// <summary>
    /// Waits for blockchain (Hyperledger Fabric) to be ready and connected.
    /// Polls the IFabricGateway until it confirms connection is established.
    /// </summary>
    private async Task WaitForBlockchainReadinessAsync(CancellationToken stoppingToken)
    {
        const double maxWaitMinutes = 1.0 / 6.0;
        const int pollIntervalSeconds = 2;
        var deadline = DateTime.UtcNow.AddMinutes(maxWaitMinutes);

        while (DateTime.UtcNow < deadline && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var fabricGateway = scope.ServiceProvider.GetService<IFabricGateway>();

                if (fabricGateway != null && await fabricGateway.IsConnectedAsync())
                {
                    _logger.LogInformation("Blockchain (Hyperledger Fabric) is now ready and connected.");
                    return;
                }

                _logger.LogInformation("Waiting for blockchain connection... ({ElapsedSeconds}s)", 
                    (DateTime.UtcNow - (deadline - TimeSpan.FromMinutes(maxWaitMinutes))).TotalSeconds);
                
                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking blockchain readiness. Retrying...");
                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), stoppingToken);
            }
        }

        if (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Blockchain readiness check cancelled.");
            return;
        }

        _logger.LogWarning(
            "Blockchain did not connect within {MaxWaitMinutes} minutes. Proceeding with queue processing anyway.",
            maxWaitMinutes);
        
        try
        {
            _logger.LogWarning("Moving all queued blockchain jobs to dead-letter queue because blockchain is unavailable.");
            await _syncQueue.MoveAllToDeadLetterAsync(stoppingToken);
            _logger.LogInformation("Moved queued blockchain jobs to DLQ successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move queued blockchain jobs to DLQ after readiness timeout.");
        }
        
        try
        {
            // Notify the sync service so other components (e.g. EHR service) can surface this in responses
            await using var scope2 = _scopeFactory.CreateAsyncScope();
            var syncService = scope2.ServiceProvider.GetService<IBlockchainSyncService>();
            if (syncService != null)
            {
                syncService.NotifyEmergencyDlqMove(DateTimeOffset.UtcNow);
                _logger.LogInformation("Notified IBlockchainSyncService of emergency DLQ move.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify IBlockchainSyncService about emergency DLQ move.");
        }
        
        // Start a background monitor that will clear the emergency flag when blockchain becomes reachable again
        _ = Task.Run(async () => await MonitorForFabricRecoveryAsync(stoppingToken));
    }

    private async Task MonitorForFabricRecoveryAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var fabricGateway = scope.ServiceProvider.GetService<IFabricGateway>();
                    if (fabricGateway != null && await fabricGateway.IsConnectedAsync())
                    {
                        // Clear the emergency DLQ move timestamp
                        var syncService = scope.ServiceProvider.GetService<IBlockchainSyncService>();
                        if (syncService != null)
                        {
                            syncService.ClearEmergencyDlqMove();
                            _logger.LogInformation("Blockchain recovered; cleared emergency DLQ move timestamp.");
                        }
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error while monitoring Fabric recovery");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // ignore
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MonitorForFabricRecoveryAsync terminated unexpectedly");
        }
    }

    /// <summary>
    /// Thực thi một blockchain job đã lấy từ queue.
    /// Step 1: Check connection → DLQ if not connected.
    /// Step 2: Execute job (retry handled by HandleFailureAsync).
    /// Step 3: After successful EHR hash commit → auto-enqueue audit entry.
    /// </summary>
    private async Task ProcessJobAsync(BlockchainSyncDequeuedItem dequeued, CancellationToken ct)
    {
        var job = dequeued.Job;
        Interlocked.Increment(ref _totalProcessed);

        _logger.LogInformation(
            "Processing blockchain sync job: Type={Type}, EntityId={EntityId}, Attempt={Attempt}",
            job.JobType, job.EntityId, job.Attempts + 1);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            // ── Step 1: Check blockchain connection before processing ──
            // Skip connection check for FabricCaEnrollment (uses HTTP, not gRPC peer)
            if (job.JobType != BlockchainSyncJobType.FabricCaEnrollment)
            {
                var fabricGateway = scope.ServiceProvider.GetService<IFabricGateway>();
                if (fabricGateway != null && !await fabricGateway.IsConnectedAsync())
                {
                    _logger.LogWarning(
                        "Blockchain is not connected. Moving job to dead-letter queue: Type={Type}, EntityId={EntityId}",
                        job.JobType, job.EntityId);

                    Interlocked.Increment(ref _totalFailed);

                    if (job.OnFailureCallback != null)
                    {
                        await job.OnFailureCallback("Blockchain is not connected. Job moved to dead-letter queue for later replay.");
                    }

                    await _syncQueue.MoveToDeadLetterAsync(
                        dequeued, job,
                        "Blockchain not connected — moved to DLQ immediately",
                        ct);

                    // Notify sync service so API responses can surface status
                    var syncService = scope.ServiceProvider.GetService<IBlockchainSyncService>();
                    syncService?.NotifyEmergencyDlqMove(DateTimeOffset.UtcNow);

                    return;
                }
            }

            // ── Step 2: Execute the blockchain job ──
            BlockchainTransactionResult? result;

            switch (job.JobType)
            {
                case BlockchainSyncJobType.EhrHash:
                    result = await HandleEhrHashAsync(scope.ServiceProvider, job.EntityId, job.PayloadJson, dequeued, ct);
                    break;

                case BlockchainSyncJobType.ConsentGrant:
                    result = await HandleConsentGrantAsync(scope.ServiceProvider, job.EntityId, job.PayloadJson, dequeued, ct);
                    break;

                case BlockchainSyncJobType.ConsentRevoke:
                    result = await HandleConsentRevokeAsync(scope.ServiceProvider, job.EntityId, job.PayloadJson, dequeued, ct);
                    break;

                case BlockchainSyncJobType.AuditLog:
                    result = await HandleAuditLogAsync(scope.ServiceProvider, job.EntityId, job.PayloadJson, dequeued, ct);
                    break;

                case BlockchainSyncJobType.FabricCaEnrollment:
                    result = await HandleFabricCaEnrollmentAsync(scope.ServiceProvider, job.PayloadJson, dequeued, ct);
                    break;

                default:
                    _logger.LogWarning("Unknown job type: {JobType}", job.JobType);
                    return;
            }

            if (result == null)
            {
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

                await _syncQueue.AckAsync(dequeued, ct);

                // ── Step 3: After successful EHR hash → auto-enqueue audit entry ──
                if (job.JobType == BlockchainSyncJobType.EhrHash)
                {
                    await EnqueueAuditAfterEhrSuccessAsync(scope.ServiceProvider, job.PayloadJson);
                }

                // Clear emergency DLQ flag since blockchain is clearly working
                var syncSvc = scope.ServiceProvider.GetService<IBlockchainSyncService>();
                syncSvc?.ClearEmergencyDlqMove();
            }
            else
            {
                await HandleFailureAsync(dequeued, result.ErrorMessage, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception processing blockchain sync job: Type={Type}, EntityId={EntityId}",
                job.JobType, job.EntityId);

            await HandleFailureAsync(dequeued, ex.Message, ct);
        }
    }

    /// <summary>
    /// Gửi hash EHR lên blockchain để đồng bộ trạng thái hồ sơ bệnh án.
    /// </summary>
    private async Task<BlockchainTransactionResult?> HandleEhrHashAsync(
        IServiceProvider serviceProvider,
        string entityId,
        string payloadJson,
        BlockchainSyncDequeuedItem dequeued,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Skipping EHR sync job because HyperledgerFabric is disabled: {EntityId}", entityId);
            await _syncQueue.AckAsync(dequeued, ct);
            return null;
        }

        var ehrService = serviceProvider.GetRequiredService<IEhrBlockchainService>();
        var ehrRecord = JsonSerializer.Deserialize<EhrHashRecord>(payloadJson);

        return ehrRecord != null
            ? await ehrService.CommitEhrHashAsync(ehrRecord)
            : new BlockchainTransactionResult { Success = false, ErrorMessage = "Invalid payload" };
    }

    /// <summary>
    /// After a successful EHR hash commit, automatically enqueue an audit entry
    /// so callers don't need to handle audit-after-blockchain separately.
    /// If the audit enqueue itself fails, it is moved to the DLQ by the normal retry path.
    /// </summary>
    private Task EnqueueAuditAfterEhrSuccessAsync(IServiceProvider serviceProvider, string ehrPayloadJson)
    {
        try
        {
            var ehrRecord = JsonSerializer.Deserialize<EhrHashRecord>(ehrPayloadJson);
            if (ehrRecord == null)
            {
                _logger.LogWarning("Cannot enqueue audit after EHR success: failed to deserialize EHR payload.");
                return Task.CompletedTask;
            }

            var auditEntry = new AuditEntry
            {
                AuditId = Guid.NewGuid().ToString(),
                ActorDid = ehrRecord.CreatedByDid ?? "SYSTEM",
                ActorType = "USER",
                Action = "CREATE",
                TargetType = "EHR",
                TargetId = ehrRecord.EhrId,
                PatientDid = ehrRecord.PatientDid,
                OrganizationId = ehrRecord.OrganizationId,
                Result = "SUCCESS",
                Timestamp = BlockchainTime.NowIsoString
            };

            var syncService = serviceProvider.GetService<IBlockchainSyncService>();
            if (syncService != null)
            {
                syncService.EnqueueAuditEntry(
                    auditEntry,
                    onFailure: error =>
                    {
                        _logger.LogWarning(
                            "Audit blockchain failed after EHR hash success for EHR {EhrId}: {Error}",
                            ehrRecord.EhrId, error);
                        return Task.CompletedTask;
                    });

                _logger.LogInformation(
                    "Auto-enqueued audit entry after successful EHR hash commit: EhrId={EhrId}, AuditId={AuditId}",
                    ehrRecord.EhrId, auditEntry.AuditId);
            }
            else
            {
                _logger.LogWarning("IBlockchainSyncService not available, cannot enqueue audit after EHR success.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue audit entry after successful EHR hash commit.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Ghi quyền đồng ý (consent grant) lên blockchain để lưu lịch sử cấp quyền.
    /// </summary>
    private async Task<BlockchainTransactionResult?> HandleConsentGrantAsync(
        IServiceProvider serviceProvider,
        string entityId,
        string payloadJson,
        BlockchainSyncDequeuedItem dequeued,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Skipping consent grant job because HyperledgerFabric is disabled: {EntityId}", entityId);
            await _syncQueue.AckAsync(dequeued, ct);
            return null;
        }

        var consentService = serviceProvider.GetRequiredService<IConsentBlockchainService>();
        var consentRecord = JsonSerializer.Deserialize<ConsentRecord>(payloadJson);

        return consentRecord != null
            ? await consentService.GrantConsentAsync(consentRecord)
            : new BlockchainTransactionResult { Success = false, ErrorMessage = "Invalid payload" };
    }

    /// <summary>
    /// Ghi thao tác thu hồi consent lên blockchain để audit được lịch sử thay đổi.
    /// </summary>
    private async Task<BlockchainTransactionResult?> HandleConsentRevokeAsync(
        IServiceProvider serviceProvider,
        string entityId,
        string payloadJson,
        BlockchainSyncDequeuedItem dequeued,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Skipping consent revoke job because HyperledgerFabric is disabled: {EntityId}", entityId);
            await _syncQueue.AckAsync(dequeued, ct);
            return null;
        }

        var consentService = serviceProvider.GetRequiredService<IConsentBlockchainService>();
        var revokeData = JsonSerializer.Deserialize<ConsentRevokePayload>(payloadJson);

        return revokeData != null
            ? await consentService.RevokeConsentAsync(revokeData.ConsentId, revokeData.RevokedAt, revokeData.Reason)
            : new BlockchainTransactionResult { Success = false, ErrorMessage = "Invalid payload" };
    }

    /// <summary>
    /// Lưu audit log lên blockchain để đảm bảo dữ liệu kiểm toán không bị chỉnh sửa.
    /// </summary>
    private async Task<BlockchainTransactionResult?> HandleAuditLogAsync(
        IServiceProvider serviceProvider,
        string entityId,
        string payloadJson,
        BlockchainSyncDequeuedItem dequeued,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Skipping audit sync job because HyperledgerFabric is disabled: {EntityId}", entityId);
            await _syncQueue.AckAsync(dequeued, ct);
            return null;
        }

        var auditService = serviceProvider.GetRequiredService<IAuditBlockchainService>();
        var auditEntry = JsonSerializer.Deserialize<AuditEntry>(payloadJson);

        return auditEntry != null
            ? await auditService.CommitAuditEntryAsync(auditEntry)
            : new BlockchainTransactionResult { Success = false, ErrorMessage = "Invalid payload" };
    }

    /// <summary>
    /// Đăng ký user với Fabric CA để phục vụ việc cấp danh tính cho blockchain network.
    /// </summary>
    private async Task<BlockchainTransactionResult> HandleFabricCaEnrollmentAsync(
        IServiceProvider serviceProvider,
        string payloadJson,
        BlockchainSyncDequeuedItem dequeued,
        CancellationToken ct)
    {
        var fabricCa = serviceProvider.GetService<IFabricCaService>();
        var enrollmentPayload = JsonSerializer.Deserialize<FabricCaEnrollPayload>(payloadJson);

        if (fabricCa == null)
        {
            return new BlockchainTransactionResult
            {
                Success = false,
                ErrorMessage = "IFabricCaService is not registered in this service"
            };
        }

        if (enrollmentPayload == null)
        {
            return new BlockchainTransactionResult
            {
                Success = false,
                ErrorMessage = "Invalid payload"
            };
        }

        var enrollResult = await fabricCa.EnrollUserAsync(
            enrollmentPayload.EnrollmentId,
            enrollmentPayload.Username,
            enrollmentPayload.Role);

        return enrollResult.Success
            ? new BlockchainTransactionResult
            {
                Success = true,
                TxHash = string.Empty,
                BlockNumber = 0,
                Timestamp = VietnamTimeHelper.Now
            }
            : new BlockchainTransactionResult
            {
                Success = false,
                ErrorMessage = enrollResult.ErrorMessage ?? "Fabric CA enrollment failed"
            };
    }

    private async Task HandleFailureAsync(BlockchainSyncDequeuedItem dequeued, string? errorMessage, CancellationToken ct)
    {
        var job = dequeued.Job;
        job.Attempts = dequeued.RetryCount + 1;
        job.LastError = errorMessage;

        if (job.Attempts < _options.MaxRetries)
        {
            if (job.Attempts <= 6)
            {
                var delay = _options.RetryDelayMs * (int)Math.Pow(2, job.Attempts - 1);
                _logger.LogWarning(
                    "Blockchain sync failed, retrying in {Delay}ms: Type={Type}, EntityId={EntityId}, Error={Error}",
                    delay, job.JobType, job.EntityId, errorMessage);

                await Task.Delay(delay, ct);
                await _syncQueue.RequeueAsync(dequeued, job, ct);
            }
            else
            {
                var delay = _options.MaxRetryDelayMs * (int)Math.Pow(3, job.Attempts - 6);
                _logger.LogWarning(
                    "Blockchain sync failed, retrying in {Delay}ms: Type={Type}, EntityId={EntityId}, Error={Error}",
                    delay, job.JobType, job.EntityId, errorMessage);

                await Task.Delay(delay, ct);
                await _syncQueue.RequeueAsync(dequeued, job, ct);
            }
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

            await _syncQueue.MoveToDeadLetterAsync(dequeued, job, errorMessage, ct);
        }
    }
}
