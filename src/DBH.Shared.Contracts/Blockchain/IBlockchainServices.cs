namespace DBH.Shared.Contracts.Blockchain;

// ============================================================================
// Fabric CA Enrollment
// ============================================================================

/// <summary>
/// Result of a Fabric CA register+enroll operation
/// </summary>
public class FabricEnrollResult
{
    public bool Success { get; init; }
    /// <summary>Unique enrollment id used on the CA.</summary>
    public string EnrollmentId { get; init; } = string.Empty;
    /// <summary>Secret returned/used during registration. Persist it securely if the identity must be enrolled again.</summary>
    public string? EnrollmentSecret { get; init; }
    /// <summary>Expected local MSP directory for the enrolled identity.</summary>
    public string? AccountStoragePath { get; init; }
    /// <summary>PEM-encoded signed certificate returned by the CA</summary>
    public string? Certificate { get; init; }
    /// <summary>PEM-encoded private key generated locally before enrollment</summary>
    public string? PrivateKeyPem { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Service that registers and enrolls a user identity on the Fabric Certificate Authority.
/// </summary>
public interface IFabricCaService
{
    /// <summary>
    /// Register the identity on the CA and enroll (obtain a signed certificate).
    /// </summary>
    /// <param name="enrollmentId">Unique ID for the identity — typically the user's UID.</param>
    /// <param name="username">Human-readable name stored as a certificate attribute.</param>
    /// <param name="role">Role name used as the OU (Organizational Unit) attribute in the certificate.</param>
    /// <param name="secret">Optional CA secret to reuse for login/re-enrollment.</param>
    Task<FabricEnrollResult> EnrollUserAsync(string enrollmentId, string username, string role, string? secret = null);
}

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

/// <summary>
/// Service cho Emergency Access contract nằm cùng ehr chaincode package.
/// </summary>
public interface IEmergencyBlockchainService
{
    /// <summary>
    /// Ghi log emergency access lên blockchain.
    /// </summary>
    Task<BlockchainTransactionResult> EmergencyAccessAsync(EmergencyAccessRecord record);

    /// <summary>
    /// Lấy emergency access logs theo target record DID.
    /// </summary>
    Task<List<EmergencyAccessRecord>> GetEmergencyAccessByRecordAsync(string targetRecordDid);

    /// <summary>
    /// Lấy emergency access logs theo accessor DID.
    /// </summary>
    Task<List<EmergencyAccessRecord>> GetEmergencyAccessByAccessorAsync(string accessorDid);

    /// <summary>
    /// Lấy toàn bộ emergency access logs.
    /// </summary>
    Task<List<EmergencyAccessRecord>> GetAllEmergencyAccessAsync();
}
