using DBH.Consent.Service.DbContext;
using DBH.Consent.Service.DTOs;
using DBH.Consent.Service.Models.Enums;
using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using DBH.Shared.Infrastructure.cryptography;
using DBH.Shared.Infrastructure.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly INotificationServiceClient? _notificationClient;

    public ConsentService(
        ConsentDbContext context,
        ILogger<ConsentService> logger,
        IHttpClientFactory httpClientFactory,
        IBlockchainSyncService blockchainSyncService,
        IConsentBlockchainService? blockchainService = null,
        IEhrBlockchainService? ehrBlockchainService = null,
        INotificationServiceClient? notificationClient = null)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _blockchainSyncService = blockchainSyncService;
        _blockchainService = blockchainService;
        _ehrBlockchainService = ehrBlockchainService;
        _notificationClient = notificationClient;
    }

    // =========================================================================
    // CONSENT OPERATIONS
    // =========================================================================

    public async Task<ApiResponse<ConsentResponse>> GrantConsentAsync(GrantConsentRequest request)
    {
        // Check if active consent already exists
        var existingConsent = await _context.Consents.FirstOrDefaultAsync(c =>
            c.PatientId == request.PatientId &&
            c.GranteeId == request.GranteeId &&
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
                var authClient = _httpClientFactory.CreateClient("AuthService");
                var ehrClient = _httpClientFactory.CreateClient("EhrService"); // Cần lấy record để lấy EncryptedAesKey của patient

                // 1. Resolve PatientId and GranteeId to UserId
                // PatientId/GranteeId are profile IDs, but Auth /keys requires UserId
                var patientUserIdRes = await authClient.GetAsync($"api/v1/auth/user-id?patientId={request.PatientId}");
                var patientUserId = request.PatientId;
                if (patientUserIdRes.IsSuccessStatusCode)
                {
                    var pidJson = await patientUserIdRes.Content.ReadAsStringAsync();
                    using var pidDoc = JsonDocument.Parse(pidJson);
                    if (pidDoc.RootElement.TryGetProperty("userId", out var pidEl))
                        patientUserId = pidEl.GetGuid();
                }

                var granteeUserId = request.GranteeId;
                var granteeUserIdRes = await authClient.GetAsync($"api/v1/auth/user-id?doctorId={request.GranteeId}");
                if (granteeUserIdRes.IsSuccessStatusCode)
                {
                    var gidJson = await granteeUserIdRes.Content.ReadAsStringAsync();
                    using var gidDoc = JsonDocument.Parse(gidJson);
                    if (gidDoc.RootElement.TryGetProperty("userId", out var gidEl))
                        granteeUserId = gidEl.GetGuid();
                }

                // Theo kiến trúc: Auth Service cung cấp EncryptedPrivateKey -> ConsentService có cấu hình MasterKey để decrypt
                var patientKeyRes = await authClient.GetAsync($"/api/v1/auth/{patientUserId}/keys");
                var granteeKeyRes = await authClient.GetAsync($"/api/v1/auth/{granteeUserId}/keys");

                if (patientKeyRes.IsSuccessStatusCode && granteeKeyRes.IsSuccessStatusCode)
                {
                    var pKeyJson = await patientKeyRes.Content.ReadAsStringAsync();
                    var gKeyJson = await granteeKeyRes.Content.ReadAsStringAsync();
                    var patientKeys = JsonSerializer.Deserialize<AuthUserKeysDto>(pKeyJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var granteeKeys = JsonSerializer.Deserialize<AuthUserKeysDto>(gKeyJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (patientKeys != null && granteeKeys != null)
                    {
                        var patientPrivateKey = MasterKeyEncryptionService.Decrypt(patientKeys.EncryptedPrivateKey);

                        // 2. Fetch EHR Blockchain record to get Patient's EncryptedAesKey
                        if (_ehrBlockchainService != null && request.EhrId.HasValue)
                        {
                            var ehrHashRecordList = await _ehrBlockchainService.GetEhrHistoryAsync(request.EhrId.Value.ToString());
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
                GrantedAt = DateTime.UtcNow.ToString("o"),
                ExpiresAt = request.DurationDays.HasValue
                    ? DateTime.UtcNow.AddDays(request.DurationDays.Value).ToString("o")
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
            PatientId = request.PatientId,
            PatientDid = request.PatientDid,
            GranteeId = request.GranteeId,
            GranteeDid = request.GranteeDid,
            GranteeType = request.GranteeType,
            EhrId = request.EhrId,
            Permission = request.Permission,
            Purpose = request.Purpose,
            Conditions = request.Conditions,
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = request.DurationDays.HasValue 
                ? DateTime.UtcNow.AddDays(request.DurationDays.Value) 
                : null,
            Status = ConsentStatus.ACTIVE,
            GrantTxHash = txHash
        };

        _context.Consents.Add(consent);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Granted consent {ConsentId} from patient {PatientId} to grantee {GranteeId}",
            consent.ConsentId, consent.PatientId, consent.GranteeId);

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
        var consent = await _context.Consents.FindAsync(consentId);
        if (consent == null)
        {
            return new ApiResponse<ConsentResponse>
            {
                Success = false,
                Message = "Consent not found"
            };
        }

        return new ApiResponse<ConsentResponse>
        {
            Success = true,
            Data = MapToResponse(consent)
        };
    }

    public async Task<PagedResponse<ConsentResponse>> GetConsentsByPatientAsync(Guid patientId, int page = 1, int pageSize = 10)
    {
        var query = _context.Consents.Where(c => c.PatientId == patientId);
        return await ExecutePagedQueryAsync(query, page, pageSize);
    }

    public async Task<PagedResponse<ConsentResponse>> GetConsentsByGranteeAsync(Guid granteeId, int page = 1, int pageSize = 10)
    {
        var query = _context.Consents.Where(c => c.GranteeId == granteeId);
        return await ExecutePagedQueryAsync(query, page, pageSize);
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
                DateTime.UtcNow.ToString("o"),
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
        consent.RevokedAt = DateTime.UtcNow;
        consent.RevokeReason = request.RevokeReason;
        consent.RevokeTxHash = txHash;

        await _context.SaveChangesAsync();

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
        var query = _context.Consents.Where(c =>
            c.PatientId == request.PatientId &&
            c.GranteeId == request.GranteeId &&
            c.Status == ConsentStatus.ACTIVE &&
            (c.ExpiresAt == null || c.ExpiresAt > DateTime.UtcNow));

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
                consent.LastSyncedAt = DateTime.UtcNow;
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

        consent.LastSyncedAt = DateTime.UtcNow;
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
        // Check for existing pending request
        var existingRequest = await _context.AccessRequests.FirstOrDefaultAsync(r =>
            r.PatientId == request.PatientId &&
            r.RequesterId == request.RequesterId &&
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
            Status = AccessRequestStatus.PENDING,
            ExpiresAt = DateTime.UtcNow.AddDays(7) // Request expires in 7 days
        };

        _context.AccessRequests.Add(accessRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created access request {RequestId} from {RequesterId} to patient {PatientId}",
            accessRequest.RequestId, accessRequest.RequesterId, accessRequest.PatientId);

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
        var query = _context.AccessRequests.Where(r => r.PatientId == patientId);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await ExecuteAccessRequestPagedQueryAsync(query, page, pageSize);
    }

    public async Task<PagedResponse<AccessRequestResponse>> GetAccessRequestsByRequesterAsync(
        Guid requesterId, AccessRequestStatus? status, int page = 1, int pageSize = 10)
    {
        var query = _context.AccessRequests.Where(r => r.RequesterId == requesterId);

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

        request.RespondedAt = DateTime.UtcNow;
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
            Conditions = consent.Conditions,
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
