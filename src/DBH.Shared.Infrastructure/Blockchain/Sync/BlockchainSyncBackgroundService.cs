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
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Blockchain sync background service started.");

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
    /// Thực thi một blockchain job đã lấy từ queue.
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
            var delay = _options.RetryDelayMs * (int)Math.Pow(2, job.Attempts - 1);
            _logger.LogWarning(
                "Blockchain sync failed, retrying in {Delay}ms: Type={Type}, EntityId={EntityId}, Error={Error}",
                delay, job.JobType, job.EntityId, errorMessage);

            await Task.Delay(delay, ct);
            await _syncQueue.RequeueAsync(dequeued, job, ct);
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
