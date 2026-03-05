namespace DBH.Shared.Contracts.Blockchain;

// ============================================================================
// Blockchain Service Interfaces
// Được implement trong DBH.Shared.Infrastructure
// ============================================================================

/// <summary>
/// Interface cho Hyperledger Fabric Gateway client
/// </summary>
public interface IFabricGateway : IAsyncDisposable
{
    /// <summary>
    /// Submit transaction (write) lên blockchain - chờ commit
    /// </summary>
    Task<BlockchainTransactionResult> SubmitTransactionAsync(
        string channelName,
        string chaincodeName,
        string functionName,
        params string[] args);

    /// <summary>
    /// Evaluate transaction (read) từ blockchain - không commit
    /// </summary>
    Task<string> EvaluateTransactionAsync(
        string channelName,
        string chaincodeName,
        string functionName,
        params string[] args);

    /// <summary>
    /// Kiểm tra kết nối Fabric network
    /// </summary>
    Task<bool> IsConnectedAsync();
}

/// <summary>
/// Service quản lý EHR hash trên blockchain
/// </summary>
public interface IEhrBlockchainService
{
    /// <summary>
    /// Lưu hash của EHR version lên blockchain
    /// </summary>
    Task<BlockchainTransactionResult> CommitEhrHashAsync(EhrHashRecord record);

    /// <summary>
    /// Lấy hash EHR từ blockchain để verify integrity
    /// </summary>
    Task<EhrHashRecord?> GetEhrHashAsync(string ehrId, int version);

    /// <summary>
    /// Lấy toàn bộ lịch sử thay đổi của EHR
    /// </summary>
    Task<List<EhrHashRecord>> GetEhrHistoryAsync(string ehrId);

    /// <summary>
    /// Verify integrity: so sánh hash hiện tại với hash trên blockchain
    /// </summary>
    Task<bool> VerifyEhrIntegrityAsync(string ehrId, int version, string currentHash);
}

/// <summary>
/// Service quản lý Consent trên blockchain
/// </summary>
public interface IConsentBlockchainService
{
    /// <summary>
    /// Ghi consent lên blockchain
    /// </summary>
    Task<BlockchainTransactionResult> GrantConsentAsync(ConsentRecord record);

    /// <summary>
    /// Thu hồi consent trên blockchain
    /// </summary>
    Task<BlockchainTransactionResult> RevokeConsentAsync(
        string consentId, string revokedAt, string? reason);

    /// <summary>
    /// Lấy consent từ blockchain
    /// </summary>
    Task<ConsentRecord?> GetConsentAsync(string consentId);

    /// <summary>
    /// Verify consent còn hiệu lực không
    /// </summary>
    Task<bool> VerifyConsentAsync(string consentId, string granteeDid);

    /// <summary>
    /// Lấy tất cả consent của patient
    /// </summary>
    Task<List<ConsentRecord>> GetPatientConsentsAsync(string patientDid);

    /// <summary>
    /// Lấy lịch sử thay đổi consent
    /// </summary>
    Task<List<ConsentRecord>> GetConsentHistoryAsync(string consentId);
}

/// <summary>
/// Service ghi audit log lên blockchain
/// </summary>
public interface IAuditBlockchainService
{
    /// <summary>
    /// Ghi audit entry lên blockchain (immutable)
    /// </summary>
    Task<BlockchainTransactionResult> CommitAuditEntryAsync(AuditEntry entry);

    /// <summary>
    /// Lấy audit entry từ blockchain
    /// </summary>
    Task<AuditEntry?> GetAuditEntryAsync(string auditId);

    /// <summary>
    /// Lấy audit logs theo patient DID
    /// </summary>
    Task<List<AuditEntry>> GetAuditsByPatientAsync(string patientDid);

    /// <summary>
    /// Lấy audit logs theo actor DID
    /// </summary>
    Task<List<AuditEntry>> GetAuditsByActorAsync(string actorDid);
}
