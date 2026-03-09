using DBH.Audit.Service.DbContext;
using DBH.Audit.Service.DTOs;
using DBH.Audit.Service.Models.Entities;
using DBH.Audit.Service.Models.Enums;
using DBH.Shared.Contracts.Blockchain;
using Microsoft.EntityFrameworkCore;

namespace DBH.Audit.Service.Services;

public class AuditService : IAuditService
{
    private readonly AuditDbContext _db;
    private readonly ILogger<AuditService> _logger;
    private readonly IAuditBlockchainService? _blockchainService;

    public AuditService(
        AuditDbContext db, 
        ILogger<AuditService> logger,
        IAuditBlockchainService? blockchainService = null)
    {
        _db = db;
        _logger = logger;
        _blockchainService = blockchainService;
    }

    // ========================================================================
    // Create
    // ========================================================================

    public async Task<ApiResponse<AuditLogResponse>> CreateAuditLogAsync(CreateAuditLogRequest request)
    {
        try
        {
            // Generate blockchain audit ID (will be replaced by real blockchain call)
            var blockchainAuditId = $"audit:{Guid.NewGuid():N}";

            var auditLog = new AuditLog
            {
                AuditId = Guid.NewGuid(),
                BlockchainAuditId = blockchainAuditId,
                ActorDid = request.ActorDid,
                ActorUserId = request.ActorUserId,
                ActorType = request.ActorType,
                Action = request.Action,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                PatientDid = request.PatientDid,
                PatientId = request.PatientId,
                ConsentId = request.ConsentId,
                OrganizationId = request.OrganizationId,
                Result = request.Result,
                Metadata = request.Metadata,
                ErrorMessage = request.ErrorMessage,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                BlockchainTimestamp = DateTime.UtcNow,
                SyncedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            // Commit to blockchain via IAuditBlockchainService
            if (_blockchainService != null)
            {
                try
                {
                    var auditEntry = new AuditEntry
                    {
                        AuditId = auditLog.AuditId.ToString(),
                        ActorDid = auditLog.ActorDid,
                        ActorType = auditLog.ActorType.ToString(),
                        Action = auditLog.Action.ToString(),
                        TargetType = auditLog.TargetType.ToString(),
                        TargetId = auditLog.TargetId?.ToString() ?? "",
                        PatientDid = auditLog.PatientDid,
                        ConsentId = auditLog.ConsentId?.ToString(),
                        OrganizationId = auditLog.OrganizationId?.ToString(),
                        Result = auditLog.Result.ToString(),
                        Timestamp = DateTime.UtcNow.ToString("o"),
                        IpAddress = auditLog.IpAddress,
                        Metadata = auditLog.Metadata
                    };

                    var txResult = await _blockchainService.CommitAuditEntryAsync(auditEntry);
                    if (txResult.Success)
                    {
                        auditLog.BlockchainTxHash = txResult.TxHash;
                        auditLog.BlockchainBlockNum = txResult.BlockNumber;
                        auditLog.BlockchainAuditId = $"audit:{txResult.TxHash?[..16]}";
                        _logger.LogInformation("Audit entry committed to blockchain: {TxHash}", txResult.TxHash);
                    }
                    else
                    {
                        _logger.LogWarning("Blockchain audit commit failed: {Error}", txResult.ErrorMessage);
                    }
                }
                catch (Exception bcEx)
                {
                    _logger.LogWarning(bcEx, "Blockchain audit commit exception for {AuditId}", auditLog.AuditId);
                }
            }

            _db.AuditLogs.Add(auditLog);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Audit log created: {AuditId} action={Action} actor={ActorDid}",
                auditLog.AuditId, auditLog.Action, auditLog.ActorDid);

            return ApiResponse<AuditLogResponse>.Ok(MapToResponse(auditLog));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for actor {ActorDid}", request.ActorDid);
            return ApiResponse<AuditLogResponse>.Fail($"Failed to create audit log: {ex.Message}");
        }
    }

    // ========================================================================
    // Read
    // ========================================================================

    public async Task<ApiResponse<AuditLogResponse>> GetAuditLogByIdAsync(Guid auditId)
    {
        var log = await _db.AuditLogs.FindAsync(auditId);
        if (log == null)
            return ApiResponse<AuditLogResponse>.Fail("Audit log not found");

        return ApiResponse<AuditLogResponse>.Ok(MapToResponse(log));
    }

    // ========================================================================
    // Search / Query
    // ========================================================================

    public async Task<PagedResponse<AuditLogResponse>> SearchAuditLogsAsync(AuditLogQueryParams query)
    {
        var q = _db.AuditLogs.AsQueryable();

        if (query.ActorUserId.HasValue)
            q = q.Where(a => a.ActorUserId == query.ActorUserId);
        if (query.PatientId.HasValue)
            q = q.Where(a => a.PatientId == query.PatientId);
        if (query.OrganizationId.HasValue)
            q = q.Where(a => a.OrganizationId == query.OrganizationId);
        if (query.TargetId.HasValue)
            q = q.Where(a => a.TargetId == query.TargetId);
        if (query.TargetType.HasValue)
            q = q.Where(a => a.TargetType == query.TargetType);
        if (query.Action.HasValue)
            q = q.Where(a => a.Action == query.Action);
        if (query.Result.HasValue)
            q = q.Where(a => a.Result == query.Result);
        if (query.FromDate.HasValue)
            q = q.Where(a => a.BlockchainTimestamp >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(a => a.BlockchainTimestamp <= query.ToDate.Value);

        var totalCount = await q.CountAsync();

        var items = await q
            .OrderByDescending(a => a.BlockchainTimestamp)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResponse<AuditLogResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResponse<AuditLogResponse>> GetAuditLogsByPatientAsync(Guid patientId, int page, int pageSize)
    {
        return await SearchAuditLogsAsync(new AuditLogQueryParams
        {
            PatientId = patientId,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<PagedResponse<AuditLogResponse>> GetAuditLogsByActorAsync(Guid actorUserId, int page, int pageSize)
    {
        return await SearchAuditLogsAsync(new AuditLogQueryParams
        {
            ActorUserId = actorUserId,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<PagedResponse<AuditLogResponse>> GetAuditLogsByTargetAsync(Guid targetId, TargetType targetType, int page, int pageSize)
    {
        return await SearchAuditLogsAsync(new AuditLogQueryParams
        {
            TargetId = targetId,
            TargetType = targetType,
            Page = page,
            PageSize = pageSize
        });
    }

    // ========================================================================
    // Statistics
    // ========================================================================

    public async Task<AuditStatsResponse> GetAuditStatsAsync(Guid? organizationId, DateTime? fromDate, DateTime? toDate)
    {
        var q = _db.AuditLogs.AsQueryable();

        if (organizationId.HasValue)
            q = q.Where(a => a.OrganizationId == organizationId);
        if (fromDate.HasValue)
            q = q.Where(a => a.BlockchainTimestamp >= fromDate.Value);
        if (toDate.HasValue)
            q = q.Where(a => a.BlockchainTimestamp <= toDate.Value);

        var logs = await q.ToListAsync();

        return new AuditStatsResponse
        {
            TotalLogs = logs.Count,
            SuccessCount = logs.Count(l => l.Result == AuditResult.SUCCESS),
            DeniedCount = logs.Count(l => l.Result == AuditResult.DENIED),
            ErrorCount = logs.Count(l => l.Result == AuditResult.ERROR),
            ActionBreakdown = logs
                .GroupBy(l => l.Action.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    // ========================================================================
    // Blockchain Sync
    // ========================================================================

    public async Task<ApiResponse<AuditLogResponse>> SyncFromBlockchainAsync(string blockchainAuditId)
    {
        // Check if already exists
        var existing = await _db.AuditLogs
            .FirstOrDefaultAsync(a => a.BlockchainAuditId == blockchainAuditId);

        if (existing != null)
            return ApiResponse<AuditLogResponse>.Ok(MapToResponse(existing), "Already synced");

        // Query blockchain via IAuditBlockchainService
        if (_blockchainService == null)
        {
            _logger.LogWarning("Blockchain service not available for sync {BlockchainAuditId}", blockchainAuditId);
            return ApiResponse<AuditLogResponse>.Fail("Blockchain service not configured");
        }

        try
        {
            var entry = await _blockchainService.GetAuditEntryAsync(blockchainAuditId);
            if (entry == null)
                return ApiResponse<AuditLogResponse>.Fail($"Audit entry {blockchainAuditId} not found on blockchain");

            var auditLog = new AuditLog
            {
                AuditId = Guid.TryParse(entry.AuditId, out var id) ? id : Guid.NewGuid(),
                BlockchainAuditId = blockchainAuditId,
                ActorDid = entry.ActorDid,
                ActorType = Enum.TryParse<ActorType>(entry.ActorType, true, out var at) ? at : ActorType.SYSTEM,
                Action = Enum.TryParse<AuditAction>(entry.Action, true, out var aa) ? aa : AuditAction.VIEW,
                TargetType = Enum.TryParse<TargetType>(entry.TargetType, true, out var tt) ? tt : TargetType.SYSTEM,
                TargetId = Guid.TryParse(entry.TargetId, out var tid) ? tid : null,
                PatientDid = entry.PatientDid,
                ConsentId = Guid.TryParse(entry.ConsentId, out var cid) ? cid : null,
                OrganizationId = Guid.TryParse(entry.OrganizationId, out var oid) ? oid : null,
                Result = Enum.TryParse<AuditResult>(entry.Result, true, out var ar) ? ar : AuditResult.SUCCESS,
                IpAddress = entry.IpAddress,
                Metadata = entry.Metadata,
                BlockchainTimestamp = DateTime.TryParse(entry.Timestamp, out var ts) ? ts : DateTime.UtcNow,
                SyncedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _db.AuditLogs.Add(auditLog);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Synced audit entry from blockchain: {BlockchainAuditId}", blockchainAuditId);
            return ApiResponse<AuditLogResponse>.Ok(MapToResponse(auditLog), "Synced from blockchain");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync audit from blockchain: {BlockchainAuditId}", blockchainAuditId);
            return ApiResponse<AuditLogResponse>.Fail($"Blockchain sync failed: {ex.Message}");
        }
    }

    // ========================================================================
    // Mapping
    // ========================================================================

    private static AuditLogResponse MapToResponse(AuditLog log) => new()
    {
        AuditId = log.AuditId,
        BlockchainAuditId = log.BlockchainAuditId,
        ActorDid = log.ActorDid,
        ActorUserId = log.ActorUserId,
        ActorType = log.ActorType,
        Action = log.Action,
        TargetType = log.TargetType,
        TargetId = log.TargetId,
        PatientDid = log.PatientDid,
        PatientId = log.PatientId,
        ConsentId = log.ConsentId,
        Result = log.Result,
        Metadata = log.Metadata,
        IpAddress = log.IpAddress,
        Timestamp = log.BlockchainTimestamp,
        BlockchainTxHash = log.BlockchainTxHash
    };
}
