using DBH.Consent.Service.Data;
using DBH.Consent.Service.DTOs;
using DBH.Consent.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DBH.Consent.Service.Services;

public class ConsentService : IConsentService
{
    private readonly ConsentDbContext _context;
    private readonly ILogger<ConsentService> _logger;

    public ConsentService(ConsentDbContext context, ILogger<ConsentService> logger)
    {
        _context = context;
        _logger = logger;
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

        // TODO: Call blockchain service to create consent on Hyperledger Fabric
        var blockchainConsentId = $"consent:{Guid.NewGuid():N}"; // Placeholder
        var txHash = $"0x{Guid.NewGuid():N}"; // Placeholder

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

        // TODO: Call blockchain service to revoke consent on Hyperledger Fabric
        var txHash = $"0x{Guid.NewGuid():N}"; // Placeholder

        consent.Status = ConsentStatus.REVOKED;
        consent.RevokedAt = DateTime.UtcNow;
        consent.RevokeReason = request.RevokeReason;
        consent.RevokeTxHash = txHash;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked consent {ConsentId}", consentId);

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
        // TODO: Implement blockchain sync
        // 1. Query Hyperledger Fabric for consent data
        // 2. Update local cache

        var consent = await _context.Consents
            .FirstOrDefaultAsync(c => c.BlockchainConsentId == blockchainConsentId);

        if (consent == null)
        {
            return new ApiResponse<ConsentResponse>
            {
                Success = false,
                Message = "Consent not found in local cache. Full sync may be required."
            };
        }

        // Placeholder: In real implementation, update from blockchain data
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

        // TODO: Send notification to patient

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
