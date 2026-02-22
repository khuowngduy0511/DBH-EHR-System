using DBH.Consent.Service.DTOs;
using DBH.Consent.Service.Models.Enums;

namespace DBH.Consent.Service.Services;

public interface IConsentService
{
    // =========================================================================
    // CONSENT OPERATIONS
    // =========================================================================

    /// <summary>
    /// Grant consent for EHR access (creates blockchain record)
    /// </summary>
    Task<ApiResponse<ConsentResponse>> GrantConsentAsync(GrantConsentRequest request);

    /// <summary>
    /// Get consent by ID
    /// </summary>
    Task<ApiResponse<ConsentResponse>> GetConsentByIdAsync(Guid consentId);

    /// <summary>
    /// Get consents granted by a patient
    /// </summary>
    Task<PagedResponse<ConsentResponse>> GetConsentsByPatientAsync(Guid patientId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Get consents granted to a grantee (doctor/org)
    /// </summary>
    Task<PagedResponse<ConsentResponse>> GetConsentsByGranteeAsync(Guid granteeId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Search consents with filters
    /// </summary>
    Task<PagedResponse<ConsentResponse>> SearchConsentsAsync(ConsentQueryParams query);

    /// <summary>
    /// Revoke consent (updates blockchain)
    /// </summary>
    Task<ApiResponse<ConsentResponse>> RevokeConsentAsync(Guid consentId, RevokeConsentRequest request);

    /// <summary>
    /// Verify if grantee has consent to access patient's EHR
    /// </summary>
    Task<VerifyConsentResponse> VerifyConsentAsync(VerifyConsentRequest request);

    /// <summary>
    /// Sync consent from blockchain (cache refresh)
    /// </summary>
    Task<ApiResponse<ConsentResponse>> SyncFromBlockchainAsync(string blockchainConsentId);

    // =========================================================================
    // ACCESS REQUEST OPERATIONS
    // =========================================================================

    /// <summary>
    /// Create access request (doctor requests patient consent)
    /// </summary>
    Task<ApiResponse<AccessRequestResponse>> CreateAccessRequestAsync(CreateAccessRequestDto request);

    /// <summary>
    /// Get access request by ID
    /// </summary>
    Task<ApiResponse<AccessRequestResponse>> GetAccessRequestByIdAsync(Guid requestId);

    /// <summary>
    /// Get pending access requests for a patient
    /// </summary>
    Task<PagedResponse<AccessRequestResponse>> GetAccessRequestsByPatientAsync(
        Guid patientId, 
        AccessRequestStatus? status = null,
        int page = 1, 
        int pageSize = 10);

    /// <summary>
    /// Get access requests made by a requester
    /// </summary>
    Task<PagedResponse<AccessRequestResponse>> GetAccessRequestsByRequesterAsync(
        Guid requesterId,
        AccessRequestStatus? status = null,
        int page = 1, 
        int pageSize = 10);

    /// <summary>
    /// Respond to access request (approve creates consent, deny closes request)
    /// </summary>
    Task<ApiResponse<AccessRequestResponse>> RespondToAccessRequestAsync(
        Guid requestId, 
        RespondAccessRequestDto response);

    /// <summary>
    /// Cancel access request (by requester)
    /// </summary>
    Task<ApiResponse<bool>> CancelAccessRequestAsync(Guid requestId);
}
