using DBH.Consent.Service.DbContext;
using DBH.Consent.Service.DTOs;
using DBH.Consent.Service.Models.Enums;
using DBH.Shared.Contracts;
using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using DBH.Shared.Infrastructure.Caching;
using DBH.Shared.Infrastructure.cryptography;
using DBH.Shared.Infrastructure.Notification;
using DBH.Shared.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DBH.Consent.Service.Services;

public class ConsentService : IConsentService
{
    private readonly ConsentDbContext _context;
    private readonly ILogger<ConsentService> _logger;
    private readonly IConsentBlockchainService? _blockchainService;
    private readonly IEhrBlockchainService? _ehrBlockchainService;
    private readonly IBlockchainSyncService _blockchainSyncService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INotificationServiceClient? _notificationClient;
    private readonly ICacheService _cache;

    private static readonly TimeSpan ConsentCacheTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ConsentListCacheTtl = TimeSpan.FromMinutes(5);

    public ConsentService(
        ConsentDbContext context,
        ILogger<ConsentService> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IBlockchainSyncService blockchainSyncService,
        ICacheService cache,
        IConsentBlockchainService? blockchainService = null,
        IEhrBlockchainService? ehrBlockchainService = null,
        INotificationServiceClient? notificationClient = null)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _blockchainSyncService = blockchainSyncService;
        _cache = cache;
        _blockchainService = blockchainService;
        _ehrBlockchainService = ehrBlockchainService;
        _notificationClient = notificationClient;
    }

    // =========================================================================
    // CONSENT OPERATIONS
    // =========================================================================

    public async Task<ApiResponse<ConsentResponse>> GrantConsentAsync(GrantConsentRequest request)
    {
        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedPatientId = await ResolveUserIdAsync(authClient, request.PatientId, isPatientProfile: true, bearerToken) ?? request.PatientId;
        var normalizedGranteeId = await ResolveUserIdAsync(authClient, request.GranteeId, isPatientProfile: false, bearerToken) ?? request.GranteeId;

        // Check if active consent already exists
        var existingConsent = await _context.Consents.FirstOrDefaultAsync(c =>
            c.PatientId == normalizedPatientId &&
            c.GranteeId == normalizedGranteeId &&
            c.EhrId == request.EhrId &&
            c.Status == ConsentStatus.ACTIVE);

        if (existingConsent != null)
        {
            return new ApiResponse<ConsentResponse>
            {
                Success = false,
                Message = "An active consent already exists for this patient-grantee-EHR combination"
            };
        }

        // === Encryption: Lấy AES key của EHR và wrap lại cho Grantee ===
        string wrappedKeyForGrantee = string.Empty;
        if (request.EhrId.HasValue)
        {
            try
            {
                var ehrClient = _httpClientFactory.CreateClient("EhrService"); // Cần lấy record để lấy EncryptedAesKey của patient

                // Theo kiến trúc: Auth Service cung cấp EncryptedPrivateKey -> ConsentService có cấu hình MasterKey để decrypt
                var patientKeyRes = await SendAuthGetAsync(authClient, $"/api/v1/auth/{normalizedPatientId}/keys", bearerToken);
                var granteeKeyRes = await SendAuthGetAsync(authClient, $"/api/v1/auth/{normalizedGranteeId}/keys", bearerToken);

                if (patientKeyRes.IsSuccessStatusCode && granteeKeyRes.IsSuccessStatusCode)
                {
                    var pKeyJson = await patientKeyRes.Content.ReadAsStringAsync();
                    var gKeyJson = await granteeKeyRes.Content.ReadAsStringAsync();
                    var patientKeys = JsonSerializer.Deserialize<AuthUserKeysDto>(pKeyJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var granteeKeys = JsonSerializer.Deserialize<AuthUserKeysDto>(gKeyJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (patientKeys != null && granteeKeys != null)
                    {
                        var patientPrivateKey = MasterKeyEncryptionService.Decrypt(patientKeys.EncryptedPrivateKey);

                        // 2. Fetch EHR Blockchain record to get Patient's EncryptedAesKey (with retry for async commit)
                        if (_ehrBlockchainService != null && request.EhrId.HasValue)
                        {
                            List<EhrHashRecord>? ehrHashRecordList = null;
                            for (int attempt = 1; attempt <= 8; attempt++)
                            {
                                ehrHashRecordList = await _ehrBlockchainService.GetEhrHistoryAsync(request.EhrId.Value.ToString());
                                var latestAttempt = ehrHashRecordList?.OrderByDescending(x => x.Version).FirstOrDefault();
                                if (latestAttempt != null && !string.IsNullOrEmpty(latestAttempt.EncryptedAesKey))
                                    break;
                                if (attempt < 8)
                                    await Task.Delay(400);
                            }
                            var latestEhrHash = ehrHashRecordList?.OrderByDescending(x => x.Version).FirstOrDefault();
                            
                            if (latestEhrHash != null && !string.IsNullOrEmpty(latestEhrHash.EncryptedAesKey))
                            {
                                // Unwrap with Patient's Private Key
                                var blueKeyBytes = AsymmetricEncryptionService.UnwrapKey(latestEhrHash.EncryptedAesKey, patientPrivateKey);
                                
                                // Wrap with Grantee's Public Key
                                wrappedKeyForGrantee = AsymmetricEncryptionService.WrapKey(blueKeyBytes, granteeKeys.PublicKey);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error wrapping key for grantee");
            }
        }

        // === Blockchain: Ghi consent lên Hyperledger Fabric ===
        string blockchainConsentId;
        string txHash;

        if (_blockchainService != null)
        {
            var consentRecord = new ConsentRecord
            {
                ConsentId = Guid.NewGuid().ToString(),
                PatientDid = request.PatientDid,
                GranteeDid = request.GranteeDid,
                GranteeType = request.GranteeType.ToString(),
                Permission = request.Permission.ToString(),
                Purpose = request.Purpose.ToString(),
                EhrId = request.EhrId?.ToString(),
                GrantedAt = BlockchainTime.NowIsoString,
                ExpiresAt = request.DurationDays.HasValue
                    ? BlockchainTime.Now.AddDays(request.DurationDays.Value).ToString("o")
                    : null,
                Status = "ACTIVE",
                EncryptedAesKey = wrappedKeyForGrantee
            };

            _blockchainSyncService.EnqueueConsentGrant(
                consentRecord,
                onFailure: error =>
                {
                    _logger.LogWarning("Queued blockchain consent grant failed for {ConsentId}: {Error}", consentRecord.ConsentId, error);
                    return Task.CompletedTask;
                });

            blockchainConsentId = consentRecord.ConsentId;
            txHash = string.Empty;
        }
        else
        {
            blockchainConsentId = $"consent:{Guid.NewGuid():N}";
            txHash = string.Empty;
        }

        var consent = new Models.Entities.Consent
        {
            BlockchainConsentId = blockchainConsentId,
            PatientId = normalizedPatientId,
            PatientDid = request.PatientDid,
            GranteeId = normalizedGranteeId,
            GranteeDid = request.GranteeDid,
            GranteeType = request.GranteeType,
            EhrId = request.EhrId,
            Permission = request.Permission,
            Purpose = request.Purpose,
            GrantedAt = VietnamTime.DatabaseNow,
            ExpiresAt = request.DurationDays.HasValue 
                ? VietnamTime.DatabaseNow.AddDays(request.DurationDays.Value)
                : null,
            Status = ConsentStatus.ACTIVE,
            GrantTxHash = txHash,
            EncryptedAesKey = wrappedKeyForGrantee
        };

        _context.Consents.Add(consent);
        await _context.SaveChangesAsync();

        // Tự động tạo Audit Log khi Grant Consent
        var auditEntry = new AuditEntry
        {
            AuditId = Guid.NewGuid().ToString(),
            ActorDid = request.PatientDid, // Hoặc lấy từ token user đăng nhập
            ActorType = "PATIENT",
            Action = "GRANT",
            TargetType = "CONSENT",
            TargetId = consent.ConsentId.ToString(),
            PatientDid = request.PatientDid,
            ConsentId = consent.ConsentId.ToString(),
            Result = "SUCCESS",
            Timestamp = BlockchainTime.NowIsoString
        };

        _blockchainSyncService.EnqueueAuditEntry(
            auditEntry,
            onFailure: error =>
            {
                _logger.LogWarning("Queued blockchain audit log failed for {ConsentId}: {Error}", consent.ConsentId, error);
                return Task.CompletedTask;
            });

        // Also POST to Audit Service for local DB storage (use original patientId for by-patient query)
        _ = PostAuditLogToServiceAsync("GRANT_CONSENT", consent.ConsentId, request.PatientId, request.PatientId, "PATIENT");

        _logger.LogInformation(
            "Granted consent {ConsentId} from patient {PatientId} to grantee {GranteeId}",
            consent.ConsentId, consent.PatientId, consent.GranteeId);

        await _cache.RemoveByPatternAsync($"consents:grantee:{consent.GranteeId}:*");

        // Notify grantee about new consent
        if (_notificationClient != null)
        {
            await _notificationClient.SendAsync(
                consent.GranteeId,
                "Quyền truy cập được cấp",
                "Bạn đã được cấp quyền truy cập hồ sơ bệnh án.",
                "ConsentGranted", "High",
                consent.ConsentId.ToString(), "Consent");
        }

        return new ApiResponse<ConsentResponse>
        {
            Success = true,
            Message = "Consent granted successfully",
            Data = MapToResponse(consent)
        };
    }

    public async Task<ApiResponse<ConsentResponse>> GetConsentByIdAsync(Guid consentId)
    {
        var cKey = $"consent:{consentId}";
        var cCached = await _cache.GetAsync<ApiResponse<ConsentResponse>>(cKey);
        if (cCached != null) return cCached;

        var consent = await _context.Consents.FindAsync(consentId);
        if (consent == null)
        {
            return new ApiResponse<ConsentResponse>
            {
                Success = false,
                Message = "Consent not found"
            };
        }

        var cResult = new ApiResponse<ConsentResponse>
        {
            Success = true,
            Data = MapToResponse(consent)
        };
        await _cache.SetAsync(cKey, cResult, ConsentCacheTtl);
        return cResult;
    }

    public async Task<PagedResponse<ConsentResponse>> GetConsentsByPatientAsync(Guid patientId, int page = 1, int pageSize = 10)
    {
        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedPatientId = await ResolveUserIdAsync(authClient, patientId, isPatientProfile: true, bearerToken);
        
        var patientCandidates = new[] { patientId, normalizedPatientId ?? Guid.Empty }
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var query = _context.Consents.Where(c => patientCandidates.Contains(c.PatientId));
        return await ExecutePagedQueryAsync(query, page, pageSize);
    }

    public async Task<PagedResponse<ConsentResponse>> GetConsentsByGranteeAsync(Guid granteeId, int page = 1, int pageSize = 10)
    {
        var gKey = $"consents:grantee:{granteeId}:{page}:{pageSize}";
        var gCached = await _cache.GetAsync<PagedResponse<ConsentResponse>>(gKey);
        if (gCached != null) return gCached;

        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedGranteeId = await ResolveUserIdAsync(authClient, granteeId, isPatientProfile: false, bearerToken);

        var granteeCandidates = new[] { granteeId, normalizedGranteeId ?? Guid.Empty }
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var query = _context.Consents.Where(c => granteeCandidates.Contains(c.GranteeId));
        var gResult = await ExecutePagedQueryAsync(query, page, pageSize);
        await _cache.SetAsync(gKey, gResult, ConsentListCacheTtl);
        return gResult;
    }

    public async Task<PagedResponse<ConsentResponse>> SearchConsentsAsync(ConsentQueryParams queryParams)
    {
        var query = _context.Consents.AsQueryable();

        if (queryParams.PatientId.HasValue)
            query = query.Where(c => c.PatientId == queryParams.PatientId.Value);

        if (queryParams.GranteeId.HasValue)
            query = query.Where(c => c.GranteeId == queryParams.GranteeId.Value);

        if (queryParams.Status.HasValue)
            query = query.Where(c => c.Status == queryParams.Status.Value);

        if (queryParams.Purpose.HasValue)
            query = query.Where(c => c.Purpose == queryParams.Purpose.Value);

        return await ExecutePagedQueryAsync(query, queryParams.Page, queryParams.PageSize);
    }

    public async Task<ApiResponse<ConsentResponse>> RevokeConsentAsync(Guid consentId, RevokeConsentRequest request)
    {
        var consent = await _context.Consents.FindAsync(consentId);
        if (consent == null)
        {
            return new ApiResponse<ConsentResponse>
            {
                Success = false,
                Message = "Consent not found"
            };
        }

        if (consent.Status != ConsentStatus.ACTIVE)
        {
            return new ApiResponse<ConsentResponse>
            {
                Success = false,
                Message = "Only active consents can be revoked"
            };
        }

        // === Blockchain: Revoke consent on Hyperledger Fabric ===
        string txHash;

        if (_blockchainService != null)
        {
            _blockchainSyncService.EnqueueConsentRevoke(
                consent.BlockchainConsentId,
                BlockchainTime.NowIsoString,
                request.RevokeReason,
                onFailure: error =>
                {
                    _logger.LogWarning("Queued blockchain consent revoke failed for {ConsentId}: {Error}", consent.BlockchainConsentId, error);
                    return Task.CompletedTask;
                });

            txHash = string.Empty;
        }
        else
        {
            txHash = string.Empty;
        }

        consent.Status = ConsentStatus.REVOKED;
        consent.RevokedAt = VietnamTime.DatabaseNow;
        consent.RevokeReason = request.RevokeReason;
        consent.RevokeTxHash = txHash;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync($"consent:{consentId}");
        await _cache.RemoveByPatternAsync($"consents:grantee:{consent.GranteeId}:*");

        // Tự động tạo Audit Log khi Revoke Consent
        var auditEntry = new AuditEntry
        {
            AuditId = Guid.NewGuid().ToString(),
            ActorDid = consent.PatientDid, // Chủ thẻ là người thu hồi
            ActorType = "PATIENT",
            Action = "REVOKE",
            TargetType = "CONSENT",
            TargetId = consent.ConsentId.ToString(),
            PatientDid = consent.PatientDid,
            ConsentId = consent.ConsentId.ToString(),
            Result = "SUCCESS",
            Timestamp = VietnamTimeHelper.Now.ToString("o")
        };

        _blockchainSyncService.EnqueueAuditEntry(
            auditEntry,
            onFailure: error =>
            {
                _logger.LogWarning("Queued blockchain audit log failed for {ConsentId}: {Error}", consent.ConsentId, error);
                return Task.CompletedTask;
            });

        // Also POST to Audit Service for local DB storage
        // consent.PatientId is the normalized userId — resolve back to original profile patientId for by-patient query
        var revokeAuthClient = _httpClientFactory.CreateClient("AuthService");
        var revokeBearer = GetBearerTokenFromContext();
        var originalPatientId = await ResolveProfilePatientIdAsync(revokeAuthClient, consent.PatientId, revokeBearer) ?? consent.PatientId;
        _ = PostAuditLogToServiceAsync("REVOKE_CONSENT", consent.ConsentId, originalPatientId, consent.PatientId, "PATIENT");

        _logger.LogInformation("Revoked consent {ConsentId}", consentId);

        // Notify grantee about revocation
        if (_notificationClient != null)
        {
            await _notificationClient.SendAsync(
                consent.GranteeId,
                "Quyền truy cập đã bị thu hồi",
                $"Quyền truy cập hồ sơ bệnh án đã bị thu hồi. Lý do: {request.RevokeReason}",
                "ConsentRevoked", "High",
                consent.ConsentId.ToString(), "Consent");
        }

        return new ApiResponse<ConsentResponse>
        {
            Success = true,
            Message = "Consent revoked successfully",
            Data = MapToResponse(consent)
        };
    }

    public async Task<VerifyConsentResponse> VerifyConsentAsync(VerifyConsentRequest request)
    {
        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedPatientId = await ResolveUserIdAsync(authClient, request.PatientId, isPatientProfile: true, bearerToken);
        var normalizedGranteeId = await ResolveUserIdAsync(authClient, request.GranteeId, isPatientProfile: false, bearerToken);

        var patientCandidates = new[] { request.PatientId, normalizedPatientId ?? Guid.Empty }
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var granteeCandidates = new[] { request.GranteeId, normalizedGranteeId ?? Guid.Empty }
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var query = _context.Consents.Where(c =>
            patientCandidates.Contains(c.PatientId) &&
            granteeCandidates.Contains(c.GranteeId) &&
            c.Status == ConsentStatus.ACTIVE &&
            (c.ExpiresAt == null || c.ExpiresAt > VietnamTimeHelper.Now));

        // If specific EHR ID requested, check for it or null (all records)
        if (request.EhrId.HasValue)
        {
            query = query.Where(c => c.EhrId == null || c.EhrId == request.EhrId.Value);
        }

        var consent = await query.FirstOrDefaultAsync();

        if (consent == null)
        {
            return new VerifyConsentResponse
            {
                HasAccess = false,
                Message = "No active consent found"
            };
        }

        // Check permission level if required
        if (request.RequiredPermission.HasValue)
        {
            var hasPermission = consent.Permission == ConsentPermission.FULL_ACCESS ||
                consent.Permission == request.RequiredPermission.Value;

            if (!hasPermission)
            {
                return new VerifyConsentResponse
                {
                    HasAccess = false,
                    Message = $"Consent exists but insufficient permission. Has: {consent.Permission}, Required: {request.RequiredPermission}"
                };
            }
        }

        return new VerifyConsentResponse
        {
            HasAccess = true,
            ConsentId = consent.ConsentId,
            Permission = consent.Permission,
            ExpiresAt = consent.ExpiresAt,
            Message = "Access granted"
        };
    }

    public async Task<ApiResponse<ConsentResponse>> SyncFromBlockchainAsync(string blockchainConsentId)
    {
        var consent = await _context.Consents
            .FirstOrDefaultAsync(c => c.BlockchainConsentId == blockchainConsentId);

        if (_blockchainService != null)
        {
            var bcConsent = await _blockchainService.GetConsentAsync(blockchainConsentId);
            if (bcConsent != null && consent != null)
            {
                // Update local cache from blockchain data
                consent.Status = Enum.TryParse<ConsentStatus>(bcConsent.Status, true, out var status) 
                    ? status : consent.Status;
                consent.LastSyncedAt = VietnamTime.DatabaseNow;
                await _context.SaveChangesAsync();

                return new ApiResponse<ConsentResponse>
                {
                    Success = true,
                    Message = "Consent synced from blockchain",
                    Data = MapToResponse(consent)
                };
            }
        }

        if (consent == null)
        {
            return new ApiResponse<ConsentResponse>
            {
                Success = false,
                Message = "Consent not found in local cache. Full sync may be required."
            };
        }

        consent.LastSyncedAt = VietnamTime.DatabaseNow;
        await _context.SaveChangesAsync();

        return new ApiResponse<ConsentResponse>
        {
            Success = true,
            Message = "Consent synced from blockchain",
            Data = MapToResponse(consent)
        };
    }

    // =========================================================================
    // ACCESS REQUEST OPERATIONS
    // =========================================================================

    public async Task<ApiResponse<AccessRequestResponse>> CreateAccessRequestAsync(CreateAccessRequestDto request)
    {
        // Normalize PatientId and RequesterId to UserId
        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedPatientId = await ResolveUserIdAsync(authClient, request.PatientId, isPatientProfile: true, bearerToken) ?? request.PatientId;
        var normalizedRequesterId = await ResolveUserIdAsync(authClient, request.RequesterId, isPatientProfile: false, bearerToken) ?? request.RequesterId;

        // Check for existing pending request (dùng cả ID gốc và ID đã normalize)
        var patientCandidates = new[] { request.PatientId, normalizedPatientId }
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
        var requesterCandidates = new[] { request.RequesterId, normalizedRequesterId }
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var existingRequest = await _context.AccessRequests.FirstOrDefaultAsync(r =>
            patientCandidates.Contains(r.PatientId) &&
            requesterCandidates.Contains(r.RequesterId) &&
            r.Status == AccessRequestStatus.PENDING);

        if (existingRequest != null)
        {
            return new ApiResponse<AccessRequestResponse>
            {
                Success = false,
                Message = "A pending access request already exists"
            };
        }

        var accessRequest = new Models.Entities.AccessRequest
        {
            PatientId = normalizedPatientId,
            PatientDid = request.PatientDid,
            RequesterId = normalizedRequesterId,
            RequesterDid = request.RequesterDid,
            RequesterType = request.RequesterType,
            OrganizationId = request.OrganizationId,
            EhrId = request.EhrId,
            Permission = request.Permission,
            Purpose = request.Purpose,
            Reason = request.Reason,
            RequestedDurationDays = request.RequestedDurationDays,
            Status = AccessRequestStatus.PENDING,
            ExpiresAt = VietnamTime.DatabaseNow.AddDays(7) // Request expires in 7 days
        };

        _context.AccessRequests.Add(accessRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created access request {RequestId} from {RequesterId} to patient {PatientId}",
            accessRequest.RequestId, accessRequest.RequesterId, accessRequest.PatientId);

        // Audit: blockchain + Audit Service
        var auditEntry = new AuditEntry
        {
            ActorDid = accessRequest.RequesterId.ToString(),
            ActorType = accessRequest.RequesterType.ToString(),
            Action = "CREATE_ACCESS_REQUEST",
            TargetType = "ACCESS_REQUEST",
            TargetId = accessRequest.RequestId.ToString(),
            PatientDid = accessRequest.PatientId.ToString(),
            Result = "SUCCESS",
            Timestamp = VietnamTimeHelper.Now.ToString("o")
        };
        _blockchainSyncService.EnqueueAuditEntry(
            auditEntry,
            onFailure: error =>
            {
                _logger.LogWarning("Queued blockchain audit log failed for AccessRequest {RequestId}: {Error}", accessRequest.RequestId, error);
                return Task.CompletedTask;
            });
        _ = PostAuditLogToServiceAsync("CREATE_ACCESS_REQUEST", accessRequest.RequestId, request.PatientId, accessRequest.RequesterId, accessRequest.RequesterType.ToString());

        // Notify patient about access request
        if (_notificationClient != null)
        {
            await _notificationClient.SendAsync(
                accessRequest.PatientId,
                "Yêu cầu truy cập hồ sơ",
                "Có yêu cầu truy cập hồ sơ bệnh án của bạn. Vui lòng xem xét và phản hồi.",
                "AccessRequestCreated", "High",
                accessRequest.RequestId.ToString(), "AccessRequest");
        }

        return new ApiResponse<AccessRequestResponse>
        {
            Success = true,
            Message = "Access request created successfully",
            Data = MapToResponse(accessRequest)
        };
    }

    public async Task<ApiResponse<AccessRequestResponse>> GetAccessRequestByIdAsync(Guid requestId)
    {
        var request = await _context.AccessRequests.FindAsync(requestId);
        if (request == null)
        {
            return new ApiResponse<AccessRequestResponse>
            {
                Success = false,
                Message = "Access request not found"
            };
        }

        return new ApiResponse<AccessRequestResponse>
        {
            Success = true,
            Data = MapToResponse(request)
        };
    }

    public async Task<PagedResponse<AccessRequestResponse>> GetAccessRequestsByPatientAsync(
        Guid patientId, AccessRequestStatus? status, int page = 1, int pageSize = 10)
    {
        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedPatientId = await ResolveUserIdAsync(authClient, patientId, isPatientProfile: true, bearerToken);

        var patientCandidates = new[] { patientId, normalizedPatientId ?? Guid.Empty }
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var query = _context.AccessRequests.Where(r => patientCandidates.Contains(r.PatientId));

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await ExecuteAccessRequestPagedQueryAsync(query, page, pageSize);
    }

    public async Task<PagedResponse<AccessRequestResponse>> GetAccessRequestsByRequesterAsync(
        Guid requesterId, AccessRequestStatus? status, int page = 1, int pageSize = 10)
    {
        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedRequesterId = await ResolveUserIdAsync(authClient, requesterId, isPatientProfile: false, bearerToken);

        var requesterCandidates = new[] { requesterId, normalizedRequesterId ?? Guid.Empty }
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var query = _context.AccessRequests.Where(r => requesterCandidates.Contains(r.RequesterId));

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await ExecuteAccessRequestPagedQueryAsync(query, page, pageSize);
    }

    public async Task<ApiResponse<AccessRequestResponse>> RespondToAccessRequestAsync(
        Guid requestId, RespondAccessRequestDto response)
    {
        var request = await _context.AccessRequests.FindAsync(requestId);
        if (request == null)
        {
            return new ApiResponse<AccessRequestResponse>
            {
                Success = false,
                Message = "Access request not found"
            };
        }

        if (request.Status != AccessRequestStatus.PENDING)
        {
            return new ApiResponse<AccessRequestResponse>
            {
                Success = false,
                Message = "This request has already been processed"
            };
        }

        request.RespondedAt = VietnamTime.DatabaseNow;
        request.ResponseReason = response.ResponseReason;

        if (response.Approve)
        {
            // Create consent
            var consentResult = await GrantConsentAsync(new GrantConsentRequest
            {
                PatientId = request.PatientId,
                PatientDid = request.PatientDid,
                GranteeId = request.RequesterId,
                GranteeDid = request.RequesterDid,
                GranteeType = request.RequesterType,
                EhrId = request.EhrId,
                Permission = request.Permission,
                Purpose = request.Purpose,
                DurationDays = request.RequestedDurationDays
            });

            if (consentResult.Success && consentResult.Data != null)
            {
                request.Status = AccessRequestStatus.APPROVED;
                request.ConsentId = consentResult.Data.ConsentId;
            }
            else
            {
                return new ApiResponse<AccessRequestResponse>
                {
                    Success = false,
                    Message = $"Failed to create consent: {consentResult.Message}"
                };
            }
        }
        else
        {
            request.Status = AccessRequestStatus.DENIED;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Access request {RequestId} {Status} by patient",
            requestId, request.Status);

        // Audit: blockchain + Audit Service
        var respondAuditEntry = new AuditEntry
        {
            ActorDid = request.PatientId.ToString(),
            ActorType = "PATIENT",
            Action = request.Status == AccessRequestStatus.APPROVED ? "APPROVE_ACCESS_REQUEST" : "DENY_ACCESS_REQUEST",
            TargetType = "ACCESS_REQUEST",
            TargetId = request.RequestId.ToString(),
            PatientDid = request.PatientId.ToString(),
            Result = "SUCCESS",
            Timestamp = VietnamTimeHelper.Now.ToString("o")
        };
        _blockchainSyncService.EnqueueAuditEntry(
            respondAuditEntry,
            onFailure: error =>
            {
                _logger.LogWarning("Queued blockchain audit log failed for AccessRequest response {RequestId}: {Error}", request.RequestId, error);
                return Task.CompletedTask;
            });
        {
            var respondAuthClient = _httpClientFactory.CreateClient("AuthService");
            var respondBearer = GetBearerTokenFromContext();
            var respondOriginalPatientId = await ResolveProfilePatientIdAsync(respondAuthClient, request.PatientId, respondBearer) ?? request.PatientId;
            _ = PostAuditLogToServiceAsync(
                request.Status == AccessRequestStatus.APPROVED ? "APPROVE_ACCESS_REQUEST" : "DENY_ACCESS_REQUEST",
                request.RequestId, respondOriginalPatientId, request.PatientId, "PATIENT");
        }

        // Notify requester about access request response
        if (_notificationClient != null)
        {
            var notifTitle = request.Status == AccessRequestStatus.APPROVED
                ? "Yêu cầu truy cập được chấp nhận"
                : "Yêu cầu truy cập bị từ chối";
            var notifBody = request.Status == AccessRequestStatus.APPROVED
                ? "Yêu cầu truy cập hồ sơ bệnh án của bạn đã được bệnh nhân chấp nhận."
                : $"Yêu cầu truy cập của bạn đã bị từ chối.{(response.ResponseReason != null ? $" Lý do: {response.ResponseReason}" : "")}";
            await _notificationClient.SendAsync(
                request.RequesterId,
                notifTitle,
                notifBody,
                "AccessRequestResponded", "High",
                request.RequestId.ToString(), "AccessRequest");
        }

        return new ApiResponse<AccessRequestResponse>
        {
            Success = true,
            Message = $"Access request {request.Status.ToString().ToLower()}",
            Data = MapToResponse(request)
        };
    }

    public async Task<ApiResponse<bool>> CancelAccessRequestAsync(Guid requestId)
    {
        var request = await _context.AccessRequests.FindAsync(requestId);
        if (request == null)
        {
            return new ApiResponse<bool> { Success = false, Message = "Access request not found" };
        }

        if (request.Status != AccessRequestStatus.PENDING)
        {
            return new ApiResponse<bool> { Success = false, Message = "Only pending requests can be cancelled" };
        }

        request.Status = AccessRequestStatus.CANCELLED;
        await _context.SaveChangesAsync();

        // Audit: blockchain + Audit Service
        var cancelAuditEntry = new AuditEntry
        {
            ActorDid = request.RequesterId.ToString(),
            ActorType = request.RequesterType.ToString(),
            Action = "CANCEL_ACCESS_REQUEST",
            TargetType = "ACCESS_REQUEST",
            TargetId = request.RequestId.ToString(),
            PatientDid = request.PatientId.ToString(),
            Result = "SUCCESS",
            Timestamp = VietnamTimeHelper.Now.ToString("o")
        };
        _blockchainSyncService.EnqueueAuditEntry(
            cancelAuditEntry,
            onFailure: error =>
            {
                _logger.LogWarning("Queued blockchain audit log failed for AccessRequest cancel {RequestId}: {Error}", request.RequestId, error);
                return Task.CompletedTask;
            });
        {
            var cancelAuthClient = _httpClientFactory.CreateClient("AuthService");
            var cancelBearer = GetBearerTokenFromContext();
            var cancelOriginalPatientId = await ResolveProfilePatientIdAsync(cancelAuthClient, request.PatientId, cancelBearer) ?? request.PatientId;
            _ = PostAuditLogToServiceAsync("CANCEL_ACCESS_REQUEST", request.RequestId, cancelOriginalPatientId, request.RequesterId, request.RequesterType.ToString());
        }

        return new ApiResponse<bool> { Success = true, Message = "Access request cancelled", Data = true };
    }

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    private async Task<PagedResponse<ConsentResponse>> ExecutePagedQueryAsync(
        IQueryable<Models.Entities.Consent> query, int page, int pageSize)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.GrantedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<ConsentResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private async Task<PagedResponse<AccessRequestResponse>> ExecuteAccessRequestPagedQueryAsync(
        IQueryable<Models.Entities.AccessRequest> query, int page, int pageSize)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<AccessRequestResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static ConsentResponse MapToResponse(Models.Entities.Consent consent)
    {
        return new ConsentResponse
        {
            ConsentId = consent.ConsentId,
            BlockchainConsentId = consent.BlockchainConsentId,
            PatientId = consent.PatientId,
            PatientDid = consent.PatientDid,
            GranteeId = consent.GranteeId,
            GranteeDid = consent.GranteeDid,
            GranteeType = consent.GranteeType,
            EhrId = consent.EhrId,
            Permission = consent.Permission,
            Purpose = consent.Purpose,
            GrantedAt = consent.GrantedAt,
            ExpiresAt = consent.ExpiresAt,
            Status = consent.Status,
            RevokedAt = consent.RevokedAt,
            RevokeReason = consent.RevokeReason,
            GrantTxHash = consent.GrantTxHash,
            RevokeTxHash = consent.RevokeTxHash,
            BlockchainBlockNum = consent.BlockchainBlockNum
        };
    }

    private static AccessRequestResponse MapToResponse(Models.Entities.AccessRequest request)
    {
        return new AccessRequestResponse
        {
            RequestId = request.RequestId,
            PatientId = request.PatientId,
            PatientDid = request.PatientDid,
            RequesterId = request.RequesterId,
            RequesterDid = request.RequesterDid,
            RequesterType = request.RequesterType,
            OrganizationId = request.OrganizationId,
            EhrId = request.EhrId,
            Permission = request.Permission,
            Purpose = request.Purpose,
            Reason = request.Reason,
            RequestedDurationDays = request.RequestedDurationDays,
            Status = request.Status,
            ConsentId = request.ConsentId,
            RespondedAt = request.RespondedAt,
            ResponseReason = request.ResponseReason,
            CreatedAt = request.CreatedAt,
            ExpiresAt = request.ExpiresAt
        };
    }

    private string? GetBearerTokenFromContext()
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";
        if (authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return authorization[bearerPrefix.Length..].Trim();
        }

        return authorization.Trim();
    }

    private async Task<HttpResponseMessage> SendAuthGetAsync(HttpClient authClient, string requestUri, string? bearerToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        if (!string.IsNullOrWhiteSpace(bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        return await authClient.SendAsync(request);
    }

    private async Task<Guid?> ResolveUserIdAsync(HttpClient authClient, Guid profileOrUserId, bool isPatientProfile, string? bearerToken)
    {
        var queryKey = isPatientProfile ? "patientId" : "doctorId";
        var response = await SendAuthGetAsync(authClient, $"/api/v1/auth/user-id?{queryKey}={profileOrUserId}", bearerToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        if (doc.RootElement.TryGetProperty("userId", out var camelUserId)
            && camelUserId.ValueKind == JsonValueKind.String
            && Guid.TryParse(camelUserId.GetString(), out var parsedCamel))
        {
            return parsedCamel;
        }

        if (doc.RootElement.TryGetProperty("UserId", out var pascalUserId)
            && pascalUserId.ValueKind == JsonValueKind.String
            && Guid.TryParse(pascalUserId.GetString(), out var parsedPascal))
        {
            return parsedPascal;
        }

        return null;
    }

    /// <summary>
    /// Reverse of ResolveUserIdAsync: given a userId, resolve back to the original
    /// profilePatientId so that audit "by-patient" queries work correctly.
    /// Calls GET /api/v1/auth/users/{userId} and extracts profiles.Patient.patientId.
    /// </summary>
    private async Task<Guid?> ResolveProfilePatientIdAsync(HttpClient authClient, Guid userId, string? bearerToken)
    {
        var response = await SendAuthGetAsync(authClient, $"/api/v1/auth/users/{userId}", bearerToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // Try profiles.Patient.patientId (camelCase)
        if (doc.RootElement.TryGetProperty("profiles", out var profiles))
        {
            if (profiles.TryGetProperty("Patient", out var patientProfile)
                && patientProfile.TryGetProperty("patientId", out var pid)
                && pid.ValueKind == JsonValueKind.String
                && Guid.TryParse(pid.GetString(), out var parsed))
            {
                return parsed;
            }
            // Fallback PascalCase
            if (profiles.TryGetProperty("Patient", out var patientProfile2)
                && patientProfile2.TryGetProperty("PatientId", out var pid2)
                && pid2.ValueKind == JsonValueKind.String
                && Guid.TryParse(pid2.GetString(), out var parsed2))
            {
                return parsed2;
            }
        }

        // Try Profiles (PascalCase root)
        if (doc.RootElement.TryGetProperty("Profiles", out var profilesPascal))
        {
            if (profilesPascal.TryGetProperty("Patient", out var patientProfile)
                && patientProfile.TryGetProperty("PatientId", out var pid)
                && pid.ValueKind == JsonValueKind.String
                && Guid.TryParse(pid.GetString(), out var parsed))
            {
                return parsed;
            }
            if (profilesPascal.TryGetProperty("Patient", out var patientProfile2)
                && patientProfile2.TryGetProperty("patientId", out var pid2)
                && pid2.ValueKind == JsonValueKind.String
                && Guid.TryParse(pid2.GetString(), out var parsed2))
            {
                return parsed2;
            }
        }

        return null;
    }

    /// <summary>
    /// POST audit log to the Audit Service HTTP API so it gets stored in PostgreSQL
    /// for querying by both patient and actor.
    /// </summary>
    private async Task PostAuditLogToServiceAsync(string action, Guid targetId, Guid patientId, Guid? actorUserId, string actorType, Guid? orgId = null)
    {
        try
        {
            var auditClient = _httpClientFactory.CreateClient("AuditService");
            var bearerToken = GetBearerTokenFromContext();

            var auditPayload = new
            {
                actorDid = actorUserId?.ToString() ?? "SYSTEM",
                actorUserId = actorUserId,
                actorType = actorType,
                action = action,
                targetType = "CONSENT",
                targetId = targetId,
                patientDid = patientId.ToString(),
                patientId = patientId,
                organizationId = orgId,
                result = "SUCCESS",
                ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString()
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/v1/audit")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(auditPayload),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            var response = await auditClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Audit Service returned {StatusCode} for consent audit log POST (ConsentId={ConsentId}, Action={Action})",
                    response.StatusCode, targetId, action);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to POST consent audit log to Audit Service for ConsentId={ConsentId}, Action={Action}", targetId, action);
        }
    }
}

/// <summary>
/// Response DTO từ Auth Service keys endpoint
/// </summary>
internal class AuthUserKeysDto
{
    public Guid UserId { get; set; }
    public string PublicKey { get; set; } = string.Empty;
    public string EncryptedPrivateKey { get; set; } = string.Empty;
}
