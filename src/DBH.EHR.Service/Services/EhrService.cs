using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Entities;
using DBH.EHR.Service.Repositories.Postgres;
using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.cryptography;
using DBH.Shared.Contracts;
using DBH.Shared.Infrastructure.Ipfs;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using DBH.Shared.Infrastructure.Notification;

namespace DBH.EHR.Service.Services;

/// <summary>
///  Ghi Primary + IPFS, đọc từ Replica
///  Tích hợp: Blockchain hash commit + Consent verification
///  ERD-aligned: ehr_records(ehr_id, patient_id, encounter_id, org_id, data, created_at)
/// </summary>
public class EhrService : IEhrService
{
    private readonly IEhrRecordRepository _ehrRecordRepo;
    private readonly ILogger<EhrService> _logger;
    private readonly IEhrBlockchainService? _blockchainService;
    private readonly IConsentBlockchainService? _consentBlockchainService;
    private readonly IBlockchainSyncService _blockchainSyncService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthServiceClient _authServiceClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly INotificationServiceClient? _notificationClient;

    public EhrService(
        IEhrRecordRepository ehrRecordRepo,
        ILogger<EhrService> logger,
        IHttpClientFactory httpClientFactory,
        IAuthServiceClient authServiceClient,
        IHttpContextAccessor httpContextAccessor,
        IBlockchainSyncService blockchainSyncService,
        IEhrBlockchainService? blockchainService = null,
        IConsentBlockchainService? consentBlockchainService = null,
        INotificationServiceClient? notificationClient = null)
    {
        _ehrRecordRepo = ehrRecordRepo;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _authServiceClient = authServiceClient;
        _httpContextAccessor = httpContextAccessor;
        _blockchainSyncService = blockchainSyncService;
        _blockchainService = blockchainService;
        _consentBlockchainService = consentBlockchainService;
        _notificationClient = notificationClient;
    }

    // EHR Records 

    public async Task<CreateEhrRecordResponseDto> CreateEhrRecordAsync(CreateEhrRecordDto request)
    {
        _logger.LogInformation(
            "Tạo EHR cho bệnh nhân {PatientId}, org: {OrgId}",
            request.PatientId, request.OrgId);

        var documentJson = request.Data.GetRawText();
        var dataHash = ComputeHash(documentJson);

        // Tạo EHR record trong PG Primary (ERD: ehr_records) — metadata only
        var ehrRecord = new EhrRecord
        {
            PatientId = request.PatientId,
            EncounterId = request.EncounterId,
            OrgId = request.OrgId
        };
        
        var savedRecord = await _ehrRecordRepo.CreateAsync(ehrRecord);
        
        // Generate AES-256 Blue Key and encrypt data
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        var blueKeyBytes = aes.Key;

        var encryptedDataStr = SymmetricEncryptionService.EncryptString(documentJson, blueKeyBytes);
        
        // Upload encrypted data to IPFS
        string? ipfsCid = null;
        string? encryptedFallbackData = null;
        try 
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, encryptedDataStr);
            var uploadRes = await IpfsClientService.UploadAsync(tempFile);
            if (uploadRes != null && !string.IsNullOrEmpty(uploadRes.Hash))
            {
                ipfsCid = uploadRes.Hash;
                _logger.LogInformation("Successfully uploaded encrypted EHR to IPFS. EhrId={EhrId}, CID={Cid}, Size={Size}", savedRecord.EhrId, ipfsCid, uploadRes.Size);
            }
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload encrypted EHR to IPFS for EhrId={EhrId}. Will use encrypted fallback storage.", savedRecord.EhrId);
        }

        // Fallback: store encrypted data in Postgres if IPFS failed
        if (string.IsNullOrEmpty(ipfsCid))
        {
            encryptedFallbackData = encryptedDataStr;
            _logger.LogWarning("IPFS upload failed for EHR {EhrId}, using encrypted fallback storage in PostgreSQL", savedRecord.EhrId);
        }

        // Tạo version đầu tiên (ERD: ehr_versions) with IPFS CID or fallback
        var version = new EhrVersion
        {
            EhrId = savedRecord.EhrId,
            VersionNumber = 1,
            IpfsCid = ipfsCid,
            EncryptedFallbackData = encryptedFallbackData,
            DataHash = dataHash
        };
        
        var savedVersion = await _ehrRecordRepo.CreateVersionAsync(version);
        
        // Tạo EHR file (ERD: ehr_files)
        var file = new EhrFile
        {
            EhrId = savedRecord.EhrId,
            FileHash = dataHash,
            IpfsCid = ipfsCid
        };
        
        var savedFile = await _ehrRecordRepo.CreateFileAsync(file);
        
        // Wrap AES blue key with Patient's Public Key
        string encryptedAesKey = string.Empty;
        try 
        {
            var authClient = _httpClientFactory.CreateClient("AuthService");
            var bearerToken = GetBearerTokenFromContext();
            var patientUserId = await ResolvePatientUserIdAsync(authClient, request.PatientId, bearerToken);
            if (patientUserId.HasValue)
            {
                var keyRes = await SendAuthGetAsync(authClient, $"/api/v1/auth/{patientUserId.Value}/keys", bearerToken);
                if (keyRes.IsSuccessStatusCode)
                {
                    var keysJson = await keyRes.Content.ReadAsStringAsync();
                    var keys = JsonSerializer.Deserialize<AuthUserKeysDto>(keysJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (keys != null && !string.IsNullOrEmpty(keys.PublicKey))
                    {
                        encryptedAesKey = AsymmetricEncryptionService.WrapKey(blueKeyBytes, keys.PublicKey);
                        _logger.LogInformation("Successfully wrapped AES key for patient {PatientId}", request.PatientId);
                    }
                    else
                    {
                        _logger.LogWarning("Patient {PatientId} has no public key available for encryption", request.PatientId);
                    }
                }
                else
                {
                    _logger.LogWarning("Auth Service returned status {StatusCode} when fetching keys for userId {UserId}", keyRes.StatusCode, patientUserId.Value);
                }
            }
            else 
            {
                _logger.LogWarning("Failed to resolve Patient UserId from PatientId {PatientId} for key encryption", request.PatientId);
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error wrapping AES key with patient public key for EHR {EhrId}", savedRecord.EhrId);
        }

        // === Blockchain: Commit EHR hash lên Hyperledger Fabric ===
        if (_blockchainService != null)
        {
            try
            {
                var ehrHashRecord = new EhrHashRecord
                {
                    EhrId = savedRecord.EhrId.ToString(),
                    PatientDid = $"did:fabric:patient:{request.PatientId}",
                    CreatedByDid = $"did:fabric:org:{request.OrgId}",
                    OrganizationId = request.OrgId?.ToString() ?? string.Empty,
                    Version = 1,
                    ContentHash = $"sha256:{dataHash}",
                    FileHash = $"sha256:{dataHash}",
                    Timestamp = BlockchainTime.NowIsoString,
                    EncryptedAesKey = encryptedAesKey
                };

                _blockchainSyncService.EnqueueEhrHash(
                    ehrHashRecord,
                    onFailure: error =>
                    {
                        _logger.LogWarning("Queued blockchain hash commit failed for EHR {EhrId}: {Error}", savedRecord.EhrId, error);
                        return Task.CompletedTask;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Blockchain hash commit exception for EHR {EhrId}", savedRecord.EhrId);
            }
        }
        
        // Tự động Audit Log
        EnqueueEhrAuditLog("CREATE", savedRecord.EhrId, request.PatientId, request.OrgId);

        _logger.LogInformation(
            "Tạo EHR {EhrId} version {VersionId} file {FileId}, IPFS CID: {IpfsCid}",
            savedRecord.EhrId, savedVersion.VersionId, savedFile.FileId, ipfsCid ?? "fallback");

        // Notify patient about new EHR
        if (_notificationClient != null)
        {
            await _notificationClient.SendAsync(
                request.PatientId,
                "Hồ sơ bệnh án mới",
                "Hồ sơ bệnh án mới đã được tạo cho bạn.",
                "EhrCreated", "Normal",
                savedRecord.EhrId.ToString(), "EHR");
        }

        var (_, patientProfile) = await GetPatientUserProfileAsync(request.PatientId);

        return new CreateEhrRecordResponseDto
        {
            EhrId = savedRecord.EhrId,
            PatientId = request.PatientId,
            PatientProfile = patientProfile,
            VersionId = savedVersion.VersionId,
            FileId = savedFile.FileId,
            VersionNumber = 1,
            IpfsCid = ipfsCid,
            DataHash = dataHash,
            CreatedAt = savedRecord.CreatedAt
        };
    }

    public async Task<EhrRecordResponseDto?> GetEhrRecordAsync(Guid ehrId)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId);
        if (record == null) return null;

        var response = MapToEhrRecordResponse(record);
        var (_, patientProfile) = await GetPatientUserProfileAsync(response.PatientId);
        response.PatientProfile = patientProfile;
        return response;
    }

    /// <inheritdoc />
    public async Task<(EhrRecordResponseDto? Record, bool ConsentDenied, string? DenyMessage)> GetEhrRecordWithConsentCheckAsync(
        Guid ehrId, Guid requesterId)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId);
        if (record == null)
            return (null, false, null);

        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedPatientId = await ResolvePatientUserIdAsync(authClient, record.PatientId, bearerToken) ?? record.PatientId;
        var normalizedRequesterId = await ResolveRequesterUserIdAsync(authClient, requesterId, bearerToken) ?? requesterId;

        // Bypass consent check nếu requester là chính bệnh nhân
        if (requesterId == record.PatientId || normalizedRequesterId == normalizedPatientId)
        {
            _logger.LogInformation("Consent bypass: requester {RequesterId} is owner of EHR {EhrId}", requesterId, ehrId);
            var response = MapToEhrRecordResponse(record);
            var (_, patientProfile) = await GetPatientUserProfileAsync(response.PatientId);
            response.PatientProfile = patientProfile;
            return (response, false, null);
        }

        // Gọi Consent Service để kiểm tra quyền truy cập
        var consentResult = await VerifyConsentAsync(normalizedPatientId, normalizedRequesterId, ehrId, "READ");
        if (!consentResult.HasAccess)
        {
            _logger.LogWarning("Consent denied: requester {RequesterId} has no consent for EHR {EhrId} of patient {PatientId}",
                requesterId, ehrId, record.PatientId);
            return (null, true, $"Người yêu cầu {requesterId} không có consent để truy cập EHR {ehrId} của bệnh nhân {record.PatientId}");
        }

        _logger.LogInformation("Consent verified: requester {RequesterId} granted access to EHR {EhrId}", requesterId, ehrId);
        var consentedResponse = MapToEhrRecordResponse(record);
        var (_, consentedPatientProfile) = await GetPatientUserProfileAsync(consentedResponse.PatientId);
        consentedResponse.PatientProfile = consentedPatientProfile;
        return (consentedResponse, false, null);
    }

    public async Task<(string? DecryptedData, bool ConsentDenied, string? DenyMessage)> GetEhrDocumentAsync(Guid ehrId, Guid requesterId)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId);
        if (record == null)
            return (null, false, "Không tìm thấy hồ sơ EHR");

        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedPatientId = await ResolvePatientUserIdAsync(authClient, record.PatientId, bearerToken) ?? record.PatientId;
        var normalizedRequesterId = await ResolveRequesterUserIdAsync(authClient, requesterId, bearerToken) ?? requesterId;

        // Get latest version from Postgres
        var latestVersion = record.Versions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (latestVersion == null)
            return (null, false, "Không tìm thấy phiên bản nào của EHR");

        // Retrieve encrypted text from IPFS or fallback
        string encryptedText;
        if (!string.IsNullOrEmpty(latestVersion.IpfsCid))
        {
            try
            {
                var downloadedPath = await IpfsClientService.RetrieveAsync(latestVersion.IpfsCid);
                encryptedText = await File.ReadAllTextAsync(downloadedPath);
                if (File.Exists(downloadedPath)) File.Delete(downloadedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve encrypted EHR from IPFS: {Cid}", latestVersion.IpfsCid);
                return (null, false, "Không thể lấy tài liệu từ IPFS");
            }
        }
        else if (!string.IsNullOrEmpty(latestVersion.EncryptedFallbackData))
        {
            encryptedText = latestVersion.EncryptedFallbackData;
        }
        else
        {
            return (null, false, "Không có dữ liệu mã hóa cho phiên bản EHR này");
        }

        bool isPatientOwner = requesterId == record.PatientId || normalizedRequesterId == normalizedPatientId;

        // Check consent BEFORE fetching keys (avoid unnecessary Auth calls if denied)
        (bool HasAccess, string? ConsentId) consentCheckResult = (false, null);
        if (!isPatientOwner)
        {
            consentCheckResult = await VerifyConsentAsync(normalizedPatientId, normalizedRequesterId, ehrId, "READ");
            if (!consentCheckResult.HasAccess || string.IsNullOrEmpty(consentCheckResult.ConsentId))
            {
                return (null, true, "Người yêu cầu không có consent để đọc EHR này.");
            }
        }

        var requesterKeyRes = await SendAuthGetAsync(authClient, $"/api/v1/auth/{normalizedRequesterId}/keys", bearerToken);
        if (!requesterKeyRes.IsSuccessStatusCode)
              return (null, false, "Không thể lấy khóa của người yêu cầu từ Auth Service");
           
        var keysJson = await requesterKeyRes.Content.ReadAsStringAsync();
        var keys = JsonSerializer.Deserialize<AuthUserKeysDto>(keysJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (keys == null || string.IsNullOrEmpty(keys.EncryptedPrivateKey))
               return (null, false, "Người yêu cầu không có khóa riêng đã mã hóa.");

        var privateKey = MasterKeyEncryptionService.Decrypt(keys.EncryptedPrivateKey);

        byte[]? consentDerivedKey = null;
        byte[]? patientDerivedKey = null;

        if (isPatientOwner)
        {
            // Always try DB key first — it is always the correct key for the latest version
            if (!string.IsNullOrEmpty(latestVersion.EncryptedAesKeyForPatient))
            {
                try { patientDerivedKey = AsymmetricEncryptionService.UnwrapKey(latestVersion.EncryptedAesKeyForPatient, privateKey); }
                catch (Exception ex) { _logger.LogWarning(ex, "Cannot unwrap DB-stored AES key for EhrId={EhrId}", ehrId); }
            }

            // Also try blockchain key (may differ — e.g. older version committed; add as additional candidate)
            if (patientDerivedKey == null)
            {
                var latestEhrEncryptedKey = await GetLatestEhrEncryptedAesKeyWithRetryAsync(ehrId);
                if (!string.IsNullOrEmpty(latestEhrEncryptedKey))
                {
                    try { patientDerivedKey = AsymmetricEncryptionService.UnwrapKey(latestEhrEncryptedKey, privateKey); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Cannot unwrap blockchain AES key for EhrId={EhrId}", ehrId); }
                }
            }
        }
        else 
        {

            if (_consentBlockchainService != null)
            {
                var blockchainConsentId = await ResolveBlockchainConsentIdAsync(consentCheckResult.ConsentId!) ?? consentCheckResult.ConsentId!;
                var consentRecord = await _consentBlockchainService.GetConsentAsync(blockchainConsentId);
                if (consentRecord != null && !string.IsNullOrEmpty(consentRecord.EncryptedAesKey))
                {
                    try
                    {
                        consentDerivedKey = AsymmetricEncryptionService.UnwrapKey(consentRecord.EncryptedAesKey, privateKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Unable to unwrap consent key for ConsentId={ConsentId}, BlockchainConsentId={BlockchainConsentId}", consentCheckResult.ConsentId, blockchainConsentId);
                    }
                }
                else
                {
                    _logger.LogWarning("Unable to load encrypted consent key from blockchain for ConsentId={ConsentId}, BlockchainConsentId={BlockchainConsentId}", consentCheckResult.ConsentId, blockchainConsentId);
                }
            }

            // Always prepare fallback from latest EHR key because consent key can become stale after EHR update,
            // and consent blockchain service may be unavailable in some environments.
            var patientKeyRes = await SendAuthGetAsync(authClient, $"/api/v1/auth/{normalizedPatientId}/keys", bearerToken);
            if (patientKeyRes.IsSuccessStatusCode)
            {
                var patientKeyJson = await patientKeyRes.Content.ReadAsStringAsync();
                var patientKeys = JsonSerializer.Deserialize<AuthUserKeysDto>(patientKeyJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (patientKeys != null && !string.IsNullOrEmpty(patientKeys.EncryptedPrivateKey))
                {
                    var patientPrivateKey = MasterKeyEncryptionService.Decrypt(patientKeys.EncryptedPrivateKey);

                    // DB key first — always correct for the latest version
                    if (!string.IsNullOrEmpty(latestVersion.EncryptedAesKeyForPatient))
                    {
                        try
                        {
                            patientDerivedKey = AsymmetricEncryptionService.UnwrapKey(latestVersion.EncryptedAesKeyForPatient, patientPrivateKey);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Unable to unwrap DB-stored AES key for non-owner fallback EhrId={EhrId}", ehrId);
                        }
                    }

                    // Also try blockchain key if DB key failed
                    if (patientDerivedKey == null && _blockchainService != null)
                    {
                        var latestEhrEncryptedKey = await GetLatestEhrEncryptedAesKeyWithRetryAsync(ehrId);
                        if (!string.IsNullOrEmpty(latestEhrEncryptedKey))
                        {
                            try
                            {
                                patientDerivedKey = AsymmetricEncryptionService.UnwrapKey(latestEhrEncryptedKey, patientPrivateKey);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Unable to unwrap latest EHR key for fallback on EhrId={EhrId}", ehrId);
                            }
                        }
                    }
                }
            }
        }

        var candidateKeys = new List<byte[]>();
        if (consentDerivedKey != null)
        {
            candidateKeys.Add(consentDerivedKey);
        }
        if (patientDerivedKey != null)
        {
            candidateKeys.Add(patientDerivedKey);
        }

        if (candidateKeys.Count == 0)
            return (null, false, "Không thể trích xuất khóa bao AES từ sổ cái");

        Exception? lastDecryptError = null;
        foreach (var candidateKey in candidateKeys)
        {
            try
            {
                var decryptedPayload = SymmetricEncryptionService.DecryptString(encryptedText, candidateKey);

                // Tự động Audit Log
                EnqueueEhrAuditLog("VIEW", ehrId, record.PatientId, record.OrgId);

                return (decryptedPayload, false, null);
            }
            catch (Exception ex)
            {
                lastDecryptError = ex;
            }
        }

        _logger.LogError(lastDecryptError, "Failed to decrypt document payload for EHR {EhrId}", ehrId);
        return (null, false, "Giải mã thất bại do khóa không hợp lệ hoặc dữ liệu bị hỏng.");
    }

    public async Task<(string? DecryptedData, bool Forbidden, string? Message)> GetEhrDocumentForCurrentUserAsync(Guid ehrId)
    {
        var userId = GetCurrentUserIdFromContext();
        if (!userId.HasValue)
        {
            return (null, true, "Không thể xác định ID người dùng hiện tại từ token");
        }

        var (decryptedData, consentDenied, denyMessage) = await GetEhrDocumentAsync(ehrId, userId.Value);
        return (decryptedData, consentDenied, denyMessage);
    }

    public async Task<string?> DownloadIpfsRawAsync(string ipfsCid)
    {
        if (string.IsNullOrWhiteSpace(ipfsCid))
        {
            return null;
        }

        try
        {
            var downloadedPath = await IpfsClientService.RetrieveAsync(ipfsCid);
            var encryptedText = await File.ReadAllTextAsync(downloadedPath);
            if (File.Exists(downloadedPath))
            {
                File.Delete(downloadedPath);
            }

            return encryptedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download raw encrypted payload from IPFS. Cid={Cid}", ipfsCid);
            return null;
        }
    }

    public async Task<IpfsRawDownloadResponseDto?> DownloadLatestIpfsRawByEhrIdAsync(Guid ehrId)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId);
        if (record == null)
        {
            return null;
        }

        var latestVersion = record.Versions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (latestVersion == null || string.IsNullOrWhiteSpace(latestVersion.IpfsCid))
        {
            return null;
        }

        var encryptedData = await DownloadIpfsRawAsync(latestVersion.IpfsCid);
        if (string.IsNullOrWhiteSpace(encryptedData))
        {
            return null;
        }

        return new IpfsRawDownloadResponseDto
        {
            IpfsCid = latestVersion.IpfsCid,
            EncryptedData = encryptedData
        };
    }

    public async Task<EncryptIpfsPayloadResponseDto?> EncryptToIpfsForCurrentUserAsync(EncryptIpfsPayloadRequestDto request)
    {
        var currentUserId = GetCurrentUserIdFromContext();
        if (!currentUserId.HasValue || string.IsNullOrWhiteSpace(request.Data))
        {
            return null;
        }

        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var keyRes = await SendAuthGetAsync(authClient, $"/api/v1/auth/{currentUserId.Value}/keys", bearerToken);
        if (!keyRes.IsSuccessStatusCode)
        {
            return null;
        }

        var keysJson = await keyRes.Content.ReadAsStringAsync();
        var keys = JsonSerializer.Deserialize<AuthUserKeysDto>(keysJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (keys == null || string.IsNullOrWhiteSpace(keys.PublicKey))
        {
            return null;
        }

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        var blueKeyBytes = aes.Key;

        var encryptedDataStr = SymmetricEncryptionService.EncryptString(request.Data, blueKeyBytes);
        var wrappedAesKey = AsymmetricEncryptionService.WrapKey(blueKeyBytes, keys.PublicKey);

        string? ipfsCid = null;
        try
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, encryptedDataStr);
            var uploadRes = await IpfsClientService.UploadAsync(tempFile);
            if (uploadRes != null && !string.IsNullOrEmpty(uploadRes.Hash))
            {
                ipfsCid = uploadRes.Hash;
            }

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload encrypted payload to IPFS for current user {UserId}", currentUserId.Value);
            return null;
        }

        if (string.IsNullOrWhiteSpace(ipfsCid))
        {
            return null;
        }

        return new EncryptIpfsPayloadResponseDto
        {
            IpfsCid = ipfsCid,
            WrappedAesKey = wrappedAesKey,
            DataHash = ComputeHash(request.Data)
        };
    }

    public async Task<string?> DecryptIpfsForCurrentUserAsync(DecryptIpfsPayloadRequestDto request)
    {
        var currentUserId = GetCurrentUserIdFromContext();
        if (!currentUserId.HasValue
            || string.IsNullOrWhiteSpace(request.IpfsCid)
            || string.IsNullOrWhiteSpace(request.WrappedAesKey))
        {
            return null;
        }

        var encryptedText = await DownloadIpfsRawAsync(request.IpfsCid);
        if (string.IsNullOrWhiteSpace(encryptedText))
        {
            return null;
        }

        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var keyRes = await SendAuthGetAsync(authClient, $"/api/v1/auth/{currentUserId.Value}/keys", bearerToken);
        if (!keyRes.IsSuccessStatusCode)
        {
            return null;
        }

        var keysJson = await keyRes.Content.ReadAsStringAsync();
        var keys = JsonSerializer.Deserialize<AuthUserKeysDto>(keysJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (keys == null || string.IsNullOrWhiteSpace(keys.EncryptedPrivateKey))
        {
            return null;
        }

        try
        {
            var privateKey = MasterKeyEncryptionService.Decrypt(keys.EncryptedPrivateKey);
            var blueKeyBytes = AsymmetricEncryptionService.UnwrapKey(request.WrappedAesKey, privateKey);
            return SymmetricEncryptionService.DecryptString(encryptedText, blueKeyBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt IPFS payload for current user {UserId}", currentUserId.Value);
            return null;
        }
    }

    public async Task<IEnumerable<EhrRecordResponseDto>> GetPatientEhrRecordsAsync(Guid patientId, Guid? requesterId = null)
    {
        var allRecords = (await _ehrRecordRepo.GetByPatientIdAsync(patientId)).ToList();

        IEnumerable<EhrRecord> accessibleRecords = allRecords;

        if (requesterId.HasValue)
        {
            var authClient = _httpClientFactory.CreateClient("AuthService");
            var bearerToken = GetBearerTokenFromContext();
            var normalizedPatientId = await ResolvePatientUserIdAsync(authClient, patientId, bearerToken) ?? patientId;
            var normalizedRequesterId = await ResolveRequesterUserIdAsync(authClient, requesterId.Value, bearerToken) ?? requesterId.Value;

            // Bệnh nhân xem hồ sơ chính mình → trả hết
            bool isSelf = requesterId.Value == patientId ||
                          requesterId.Value == normalizedPatientId ||
                          normalizedRequesterId == normalizedPatientId;

            if (!isSelf)
            {
                // Lọc từng record theo consent
                var filtered = new List<EhrRecord>();
                foreach (var record in allRecords)
                {
                    var (hasAccess, _) = await VerifyConsentAsync(normalizedPatientId, normalizedRequesterId, record.EhrId, "READ");
                    if (hasAccess)
                        filtered.Add(record);
                }
                accessibleRecords = filtered;
            }
        }

        var responses = accessibleRecords.Select(r => MapToEhrRecordResponse(r)).ToList();
        await AttachPatientProfilesAsync(responses);
        return responses;
    }

    public async Task<IEnumerable<EhrMetadataDto>> GetPatientEhrMetadataAsync(Guid patientId)
    {
        var records = await _ehrRecordRepo.GetByPatientIdAsync(patientId);
        return records.Select(r => new EhrMetadataDto
        {
            EhrId        = r.EhrId,
            PatientId    = r.PatientId,
            OrgId        = r.OrgId,
            EncounterId  = r.EncounterId,
            CreatedAt    = r.CreatedAt,
            VersionCount = r.Versions?.Count ?? 0
        });
    }

    public async Task<IEnumerable<EhrRecordResponseDto>> GetOrgEhrRecordsAsync(Guid orgId)    {
        var records = await _ehrRecordRepo.GetByOrgIdAsync(orgId);
        var responses = records.Select(r => MapToEhrRecordResponse(r)).ToList();
        await AttachPatientProfilesAsync(responses);
        return responses;
    }

    public async Task<IEnumerable<EhrVersionDto>> GetEhrVersionsAsync(Guid ehrId)
    {
        var versions = await _ehrRecordRepo.GetVersionsAsync(ehrId);
        return versions.Select(v => new EhrVersionDto
        {
            VersionId = v.VersionId,
            VersionNumber = v.VersionNumber,
            CreatedAt = v.CreatedAt
        });
    }

    public async Task<IEnumerable<EhrFileDto>> GetEhrFilesAsync(Guid ehrId)
    {
        var files = await _ehrRecordRepo.GetFilesAsync(ehrId);
        return files.Select(f => new EhrFileDto
        {
            FileId = f.FileId,
            FileUrl = f.FileUrl,
            FileName = f.FileUrl,
            FileHash = f.FileHash,
            CreatedAt = f.CreatedAt
        });
    }

    public async Task<EhrRecordResponseDto?> UpdateEhrRecordAsync(Guid ehrId, UpdateEhrRecordDto request)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId);
        if (record == null) return null;

        if (request.Data.ValueKind == System.Text.Json.JsonValueKind.Undefined || request.Data.ValueKind == System.Text.Json.JsonValueKind.Null)
            throw new ArgumentException("Data field is required and must be a valid JSON object.");

        var documentJson = request.Data.GetRawText();
        var dataHash = ComputeHash(documentJson);

        // Calculate new version number early
        var latestVersion = record.Versions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        var newVersionNumber = (latestVersion?.VersionNumber ?? 0) + 1;

        // Generate new AES-256 key and encrypt
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        var blueKeyBytes = aes.Key;

        var encryptedDataStr = SymmetricEncryptionService.EncryptString(documentJson, blueKeyBytes);

        // Upload encrypted data to IPFS
        string? ipfsCid = null;
        string? encryptedFallbackData = null;
        try
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, encryptedDataStr);
            var uploadRes = await IpfsClientService.UploadAsync(tempFile);
            if (uploadRes != null && !string.IsNullOrEmpty(uploadRes.Hash))
            {
                ipfsCid = uploadRes.Hash;
                _logger.LogInformation("Successfully uploaded encrypted EHR update to IPFS. EhrId={EhrId}, Version={Version}, CID={Cid}, Size={Size}", ehrId, newVersionNumber, ipfsCid, uploadRes.Size);
            }
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload encrypted EHR update to IPFS for EhrId={EhrId}, Version={Version}. Will use encrypted fallback storage.", ehrId, newVersionNumber);
        }

        // Fallback if IPFS failed
        if (string.IsNullOrEmpty(ipfsCid))
        {
            encryptedFallbackData = encryptedDataStr;
            _logger.LogWarning("IPFS upload failed on update for EHR {EhrId} version {Version}, using encrypted fallback storage in PostgreSQL", ehrId, newVersionNumber);
        }

        // Wrap AES key for Patient BEFORE creating the version so it can be stored in DB immediately.
        // This ensures GET /document can decrypt without waiting for async blockchain commit.
        string encryptedAesKeyForPatient = string.Empty;
        string encryptedAesKeyForRequester = string.Empty;
        Guid? requesterUserId = GetCurrentUserIdFromContext();
        var authClientEarly = _httpClientFactory.CreateClient("AuthService");
        var bearerTokenEarly = GetBearerTokenFromContext();
        Guid? patientUserIdEarly = null;

        try
        {
            patientUserIdEarly = await ResolvePatientUserIdAsync(authClientEarly, record.PatientId, bearerTokenEarly);
            if (patientUserIdEarly.HasValue)
            {
                var keyRes = await SendAuthGetAsync(authClientEarly, $"/api/v1/auth/{patientUserIdEarly.Value}/keys", bearerTokenEarly);
                if (keyRes.IsSuccessStatusCode)
                {
                    var keysJson = await keyRes.Content.ReadAsStringAsync();
                    var keys = JsonSerializer.Deserialize<AuthUserKeysDto>(keysJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (keys != null && !string.IsNullOrEmpty(keys.PublicKey))
                    {
                        encryptedAesKeyForPatient = AsymmetricEncryptionService.WrapKey(blueKeyBytes, keys.PublicKey);
                        _logger.LogInformation("Successfully wrapped AES key for patient {PatientId} in EHR update", record.PatientId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pre-wrapping patient AES key during EHR update for EHR {EhrId}", ehrId);
        }

        // Create new version — include patient AES key for immediate fallback decryption
        var version = new EhrVersion
        {
            EhrId = ehrId,
            VersionNumber = newVersionNumber,
            IpfsCid = ipfsCid,
            EncryptedFallbackData = encryptedFallbackData,
            DataHash = dataHash,
            EncryptedAesKeyForPatient = string.IsNullOrEmpty(encryptedAesKeyForPatient) ? null : encryptedAesKeyForPatient
        };
        await _ehrRecordRepo.CreateVersionAsync(version);

        try
        {
            var authClient = authClientEarly;
            var bearerToken = bearerTokenEarly;

            // 1. Patient key already wrapped above — reuse for blockchain commit
            var patientUserId = patientUserIdEarly;

            // 2. Wrap for Requester (Doctor/Operator) if different from patient
            if (requesterUserId.HasValue && requesterUserId.Value != patientUserId)
            {
                var keyRes = await SendAuthGetAsync(authClient, $"/api/v1/auth/{requesterUserId.Value}/keys", bearerToken);
                if (keyRes.IsSuccessStatusCode)
                {
                    var keysJson = await keyRes.Content.ReadAsStringAsync();
                    var keys = JsonSerializer.Deserialize<AuthUserKeysDto>(keysJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (keys != null && !string.IsNullOrEmpty(keys.PublicKey))
                    {
                        encryptedAesKeyForRequester = AsymmetricEncryptionService.WrapKey(blueKeyBytes, keys.PublicKey);
                        
                        // Update current requester's consent if they have one
                        if (_consentBlockchainService != null)
                        {
                            var consentRes = await _consentBlockchainService.GetConsentAsync($"{record.PatientId}_{requesterUserId.Value}");
                            if (consentRes != null)
                            {
                                consentRes.EncryptedAesKey = encryptedAesKeyForRequester;
                                _blockchainSyncService.EnqueueConsentGrant(consentRes);
                                _logger.LogInformation("Enqueued update for requester's consent with new AES key. RequesterUserId={UserId}", requesterUserId.Value);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error re-wrapping keys during EHR update for EHR {EhrId}", ehrId);
        }

        // Blockchain: Commit updated hash
        if (_blockchainService != null)
        {
            try
            {
                var ehrHashRecord = new EhrHashRecord
                {
                    EhrId = ehrId.ToString(),
                    PatientDid = $"did:fabric:patient:{record.PatientId}",
                    CreatedByDid = $"did:fabric:org:{record.OrgId}",
                    OrganizationId = record.OrgId?.ToString() ?? string.Empty,
                    Version = newVersionNumber,
                    ContentHash = $"sha256:{dataHash}",
                    FileHash = $"sha256:{dataHash}",
                    Timestamp = BlockchainTime.NowIsoString,
                    EncryptedAesKey = encryptedAesKeyForPatient
                };

                _blockchainSyncService.EnqueueEhrHash(
                    ehrHashRecord,
                    onFailure: error =>
                    {
                        _logger.LogWarning("Queued blockchain hash commit failed for EHR {EhrId} v{Version}: {Error}", ehrId, newVersionNumber, error);
                        return Task.CompletedTask;
                    });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Blockchain hash commit exception for EHR {EhrId} v{Version}", ehrId, newVersionNumber);
            }
        }

        // Tự động Audit Log
        EnqueueEhrAuditLog("UPDATE", ehrId, record.PatientId, record.OrgId);

        _logger.LogInformation("Updated EHR {EhrId} to version {Version}, IPFS CID: {IpfsCid}", ehrId, newVersionNumber, ipfsCid ?? "fallback");

        // Notify patient about EHR update
        if (_notificationClient != null)
        {
            await _notificationClient.SendAsync(
                record.PatientId,
                "Hồ sơ bệnh án được cập nhật",
                $"Hồ sơ bệnh án đã được cập nhật lên phiên bản {newVersionNumber}.",
                "EhrUpdated", "Normal",
                ehrId.ToString(), "EHR");
        }

        // Reload record with updated versions
        var updatedRecord = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId);
        if (updatedRecord == null)
        {
            return null;
        }

        var response = MapToEhrRecordResponse(updatedRecord);
        var (_, patientProfile) = await GetPatientUserProfileAsync(response.PatientId);
        response.PatientProfile = patientProfile;
        return response;
    }

    public async Task<(EhrRecordResponseDto? Record, bool ConsentDenied, string? DenyMessage)> UpdateEhrRecordWithConsentCheckAsync(
        Guid ehrId, UpdateEhrRecordDto request, Guid requesterId)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId);
        if (record == null) return (null, false, null);

        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedPatientId = await ResolvePatientUserIdAsync(authClient, record.PatientId, bearerToken) ?? record.PatientId;
        var normalizedRequesterId = await ResolveRequesterUserIdAsync(authClient, requesterId, bearerToken) ?? requesterId;

        // Patient cannot edit their own EHR
        if (normalizedRequesterId == normalizedPatientId)
            return (null, true, "Bệnh nhân không có quyền chỉnh sửa hồ sơ EHR.");

        var consentResult = await VerifyConsentAsync(normalizedPatientId, normalizedRequesterId, ehrId, "WRITE");
        if (!consentResult.HasAccess)
            return (null, true, $"Người yêu cầu {requesterId} không có consent WRITE để cập nhật EHR {ehrId}.");

        var result = await UpdateEhrRecordAsync(ehrId, request);
        return (result, false, null);
    }

    public async Task<(string? DecryptedData, bool ConsentDenied, string? DenyMessage)> DownloadEhrDocumentAsync(
        Guid ehrId, Guid requesterId)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId);
        if (record == null) return (null, false, "Không tìm thấy hồ sơ EHR");

        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedPatientId = await ResolvePatientUserIdAsync(authClient, record.PatientId, bearerToken) ?? record.PatientId;
        var normalizedRequesterId = await ResolveRequesterUserIdAsync(authClient, requesterId, bearerToken) ?? requesterId;

        bool isPatientOwner = requesterId == record.PatientId || normalizedRequesterId == normalizedPatientId;

        if (!isPatientOwner)
        {
            var consentResult = await VerifyConsentAsync(normalizedPatientId, normalizedRequesterId, ehrId, "READ");
            if (!consentResult.HasAccess)
                return (null, true, "Người yêu cầu không có consent READ để xuất EHR này.");
        }

        var (decryptedData, consentDenied, denyMessage) = await GetEhrDocumentAsync(ehrId, requesterId);
        if (decryptedData != null)
            EnqueueEhrAuditLog("DOWNLOAD", ehrId, record.PatientId, record.OrgId);

        return (decryptedData, consentDenied, denyMessage);
    }

    public async Task<EhrVersionDetailDto?> GetVersionByIdAsync(Guid ehrId, Guid versionId)
    {
        var version = await _ehrRecordRepo.GetVersionByIdAsync(ehrId, versionId);
        if (version == null) return null;

        return new EhrVersionDetailDto
        {
            VersionId = version.VersionId,
            EhrId = version.EhrId,
            VersionNumber = version.VersionNumber,
            IpfsCid = version.IpfsCid,
            DataHash = version.DataHash,
            CreatedAt = version.CreatedAt
        };
    }

    public async Task<(EhrVersionDocumentResponseDto? Result, bool ConsentDenied, string? DenyMessage)> GetVersionDocumentAsync(
        Guid ehrId, Guid versionId, Guid requesterId)
    {
        // Load EHR và version cụ thể
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId);
        if (record == null)
            return (null, false, "EHR Record not found");

        var version = await _ehrRecordRepo.GetVersionByIdAsync(ehrId, versionId);
        if (version == null)
            return (null, false, $"Không tìm thấy phiên bản {versionId} trong EHR {ehrId}");

        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedPatientId = await ResolvePatientUserIdAsync(authClient, record.PatientId, bearerToken) ?? record.PatientId;
        var normalizedRequesterId = await ResolveRequesterUserIdAsync(authClient, requesterId, bearerToken) ?? requesterId;

        bool isPatientOwner = requesterId == record.PatientId || normalizedRequesterId == normalizedPatientId;

        // Kiểm tra consent nếu không phải chính bệnh nhân
        (bool HasAccess, string? ConsentId) consentCheckResult = (true, null);
        if (!isPatientOwner)
        {
            consentCheckResult = await VerifyConsentAsync(normalizedPatientId, normalizedRequesterId, ehrId, "READ");
            if (!consentCheckResult.HasAccess)
                return (null, true, "Requester does not have consent to read this EHR.");
        }

        // Lấy dữ liệu mã hóa từ IPFS hoặc fallback
        string encryptedText;
        if (!string.IsNullOrEmpty(version.IpfsCid))
        {
            try
            {
                var downloadedPath = await IpfsClientService.RetrieveAsync(version.IpfsCid);
                encryptedText = await File.ReadAllTextAsync(downloadedPath);
                if (File.Exists(downloadedPath)) File.Delete(downloadedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve version {VersionId} from IPFS: {Cid}", versionId, version.IpfsCid);
                return (null, false, "Failed to retrieve document from IPFS");
            }
        }
        else if (!string.IsNullOrEmpty(version.EncryptedFallbackData))
        {
            encryptedText = version.EncryptedFallbackData;
        }
        else
        {
            return (null, false, "No encrypted data available for this version");
        }

        // Lấy private key của requester
        var requesterKeyRes = await SendAuthGetAsync(authClient, $"/api/v1/auth/{normalizedRequesterId}/keys", bearerToken);
        if (!requesterKeyRes.IsSuccessStatusCode)
            return (null, false, "Cannot fetch requester keys from Auth Service");

        var keysJson = await requesterKeyRes.Content.ReadAsStringAsync();
        var keys = JsonSerializer.Deserialize<AuthUserKeysDto>(keysJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (keys == null || string.IsNullOrEmpty(keys.EncryptedPrivateKey))
            return (null, false, "Requester missing encrypted private key.");

        var privateKey = MasterKeyEncryptionService.Decrypt(keys.EncryptedPrivateKey);

        // Lấy AES key của version từ blockchain với retry
        var versionEncryptedAesKey = await GetVersionEncryptedAesKeyWithRetryAsync(ehrId, version.VersionNumber);
        var candidateKeys = new List<byte[]>();

        if (!string.IsNullOrEmpty(versionEncryptedAesKey))
        {
            if (isPatientOwner)
            {
                // Bệnh nhân dùng private key của chính mình để unwrap
                try
                {
                    candidateKeys.Add(AsymmetricEncryptionService.UnwrapKey(versionEncryptedAesKey, privateKey));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to unwrap version AES key for patient owner EhrId={EhrId} Version={Version}", ehrId, version.VersionNumber);
                }
            }
            else
            {
                // Non-owner: AES key được wrap bằng public key bệnh nhân → cần private key của bệnh nhân
                var patientKeyRes = await SendAuthGetAsync(authClient, $"/api/v1/auth/{normalizedPatientId}/keys", bearerToken);
                if (patientKeyRes.IsSuccessStatusCode)
                {
                    var patientKeyJson = await patientKeyRes.Content.ReadAsStringAsync();
                    var patientKeys = JsonSerializer.Deserialize<AuthUserKeysDto>(patientKeyJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (patientKeys != null && !string.IsNullOrEmpty(patientKeys.EncryptedPrivateKey))
                    {
                        var patientPrivateKey = MasterKeyEncryptionService.Decrypt(patientKeys.EncryptedPrivateKey);
                        try
                        {
                            candidateKeys.Add(AsymmetricEncryptionService.UnwrapKey(versionEncryptedAesKey, patientPrivateKey));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Unable to unwrap version AES key via patient key EhrId={EhrId} Version={Version}", ehrId, version.VersionNumber);
                        }

                        // DB-stored key fallback for version (when blockchain key not yet committed)
                        if (candidateKeys.Count == 0 && !string.IsNullOrEmpty(version.EncryptedAesKeyForPatient))
                        {
                            try { candidateKeys.Add(AsymmetricEncryptionService.UnwrapKey(version.EncryptedAesKeyForPatient, patientPrivateKey)); }
                            catch (Exception ex) { _logger.LogWarning(ex, "Unable to unwrap DB AES key for version EhrId={EhrId} Version={Version}", ehrId, version.VersionNumber); }
                        }
                    }
                }

                // Thử thêm consent key (hoạt động nếu version này là version được wrap trong consent)
                if (_consentBlockchainService != null && consentCheckResult.ConsentId != null)
                {
                    var blockchainConsentId = await ResolveBlockchainConsentIdAsync(consentCheckResult.ConsentId) ?? consentCheckResult.ConsentId;
                    try
                    {
                        var consentRecord = await _consentBlockchainService.GetConsentAsync(blockchainConsentId);
                        if (consentRecord != null && !string.IsNullOrEmpty(consentRecord.EncryptedAesKey))
                            candidateKeys.Add(AsymmetricEncryptionService.UnwrapKey(consentRecord.EncryptedAesKey, privateKey));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Unable to unwrap consent key for version EhrId={EhrId} Version={Version}", ehrId, version.VersionNumber);
                    }
                }
            }
        }

        // DB-stored patient key as last resort (when blockchain key is null entirely)
        if (candidateKeys.Count == 0 && !string.IsNullOrEmpty(version.EncryptedAesKeyForPatient))
        {
            if (isPatientOwner)
            {
                try { candidateKeys.Add(AsymmetricEncryptionService.UnwrapKey(version.EncryptedAesKeyForPatient, privateKey)); }
                catch (Exception ex) { _logger.LogWarning(ex, "Cannot unwrap DB AES key for patient owner EhrId={EhrId}", ehrId); }
            }
        }

        if (candidateKeys.Count == 0)
            return (null, false, "Không thể trích xuất AES key cho phiên bản này từ blockchain");

        Exception? lastDecryptError = null;
        foreach (var candidateKey in candidateKeys)
        {
            try
            {
                var decrypted = SymmetricEncryptionService.DecryptString(encryptedText, candidateKey);
                EnqueueEhrAuditLog("READ_VERSION", ehrId, record.PatientId, record.OrgId);
                var (dto, _) = await BuildVersionDocumentResponseAsync(version, record, decrypted);
                return (dto, false, null);
            }
            catch (Exception ex)
            {
                lastDecryptError = ex;
            }
        }

        _logger.LogError(lastDecryptError, "Decryption failed for EHR {EhrId} version {VersionId}", ehrId, versionId);
        return (null, false, "Giải mã thất bại do khóa không hợp lệ hoặc dữ liệu bị hỏng.");
    }

    private async Task<(EhrVersionDocumentResponseDto dto, bool success)> BuildVersionDocumentResponseAsync(
        EhrVersion version, EhrRecord record, string document)
    {
        var (_, patientProfile) = await GetPatientUserProfileAsync(record.PatientId);

        var files = (await _ehrRecordRepo.GetFilesAsync(record.EhrId))
            .Select(f => new EhrFileDto
            {
                FileId = f.FileId,
                FileUrl = f.FileUrl,
                FileName = f.FileUrl,
                FileHash = f.FileHash,
                CreatedAt = f.CreatedAt
            }).ToList();

        return (new EhrVersionDocumentResponseDto
        {
            VersionId = version.VersionId,
            EhrId = version.EhrId,
            PatientId = record.PatientId,
            PatientProfile = patientProfile,
            EncounterId = record.EncounterId,
            OrgId = record.OrgId,
            VersionNumber = version.VersionNumber,
            DataHash = version.DataHash,
            Files = files,
            EhrCreatedAt = record.CreatedAt,
            CreatedAt = version.CreatedAt,
            Document = document
        }, true);
    }

    public async Task<EhrFileDto?> AddFileAsync(Guid ehrId, Stream fileStream, string fileName)
    {
        var record = await _ehrRecordRepo.GetByIdAsync(ehrId);
        if (record == null) return null;

        // Compute file hash from stream
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(fileStream);
        var fileHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        fileStream.Position = 0;

        // Encrypt file with AES
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        var blueKeyBytes = aes.Key;

        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();
        var encryptedDataStr = SymmetricEncryptionService.EncryptString(
            Convert.ToBase64String(fileBytes), blueKeyBytes);

        // Upload to IPFS
        string? ipfsCid = null;
        string? encryptedFallbackData = null;
        try
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, encryptedDataStr);
            var uploadRes = await IpfsClientService.UploadAsync(tempFile);
            if (uploadRes != null && !string.IsNullOrEmpty(uploadRes.Hash))
            {
                ipfsCid = uploadRes.Hash;
                _logger.LogInformation("Successfully uploaded encrypted file to IPFS. EhrId={EhrId}, CID={Cid}, Size={Size}, FileHash={FileHash}", ehrId, ipfsCid, uploadRes.Size, fileHash);
            }
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload encrypted file to IPFS for EhrId={EhrId}. Will use encrypted fallback storage.", ehrId);
        }

        // Fallback if IPFS failed
        if (string.IsNullOrEmpty(ipfsCid))
        {
            encryptedFallbackData = encryptedDataStr;
            _logger.LogWarning("IPFS upload failed for EHR {EhrId}, using encrypted fallback storage in PostgreSQL. FileHash={FileHash}", ehrId, fileHash);
        }

        // Wrap AES key with patient's public key for later decryption
        string? encryptedAesKey = null;
        try
        {
            var authClient = _httpClientFactory.CreateClient("AuthService");
            var bearerToken = GetBearerTokenFromContext();
            var patientUserId = await ResolvePatientUserIdAsync(authClient, record.PatientId, bearerToken);
            var keyRes = await SendAuthGetAsync(authClient, $"/api/v1/auth/{patientUserId ?? record.PatientId}/keys", bearerToken);
            if (keyRes.IsSuccessStatusCode)
            {
                var keysJson = await keyRes.Content.ReadAsStringAsync();
                var keys = JsonSerializer.Deserialize<AuthUserKeysDto>(keysJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (keys != null && !string.IsNullOrEmpty(keys.PublicKey))
                    encryptedAesKey = AsymmetricEncryptionService.WrapKey(blueKeyBytes, keys.PublicKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to wrap AES key for file in EHR {EhrId}. File can still be stored but decryption may not be possible without the key.", ehrId);
        }

        var file = new EhrFile
        {
            EhrId = ehrId,
            FileUrl = fileName,
            FileHash = fileHash,
            IpfsCid = ipfsCid,
            EncryptedFallbackData = encryptedFallbackData,
            EncryptedAesKey = encryptedAesKey
        };
        var savedFile = await _ehrRecordRepo.CreateFileAsync(file);

        _logger.LogInformation("Added file {FileId} to EHR {EhrId}, IPFS CID: {IpfsCid}", savedFile.FileId, ehrId, ipfsCid ?? "fallback");

        EnqueueEhrAuditLog("CREATE", ehrId, record.PatientId, record.OrgId);

        if (_notificationClient != null)
        {
            await _notificationClient.SendAsync(
                record.PatientId,
                "File moi trong ho so benh an",
                "Mot file moi da duoc them vao ho so benh an cua ban.",
                "EhrFileAdded", "Low",
                ehrId.ToString(), "EHR");
        }

        return new EhrFileDto
        {
            FileId = savedFile.FileId,
            FileName = savedFile.FileUrl,
            FileUrl = savedFile.FileUrl,
            FileHash = savedFile.FileHash,
            CreatedAt = savedFile.CreatedAt
        };
    }

    public async Task<(byte[]? Content, string? FileName, bool ConsentDenied, string? DenyMessage)>
        DownloadFileAsync(Guid ehrId, Guid fileId, Guid requesterId)
    {
        var record = await _ehrRecordRepo.GetByIdAsync(ehrId);
        if (record == null) return (null, null, false, "EHR not found");

        var file = await _ehrRecordRepo.GetFileByIdAsync(ehrId, fileId);
        if (file == null) return (null, null, false, "File not found");

        var authClient = _httpClientFactory.CreateClient("AuthService");
        var bearerToken = GetBearerTokenFromContext();
        var normalizedPatientId = await ResolvePatientUserIdAsync(authClient, record.PatientId, bearerToken) ?? record.PatientId;
        var normalizedRequesterId = await ResolveRequesterUserIdAsync(authClient, requesterId, bearerToken) ?? requesterId;

        bool isPatientOwner = requesterId == record.PatientId || normalizedRequesterId == normalizedPatientId;

        if (!isPatientOwner)
        {
            var consentResult = await VerifyConsentAsync(normalizedPatientId, normalizedRequesterId, ehrId, "DOWNLOAD");
            if (!consentResult.HasAccess)
                return (null, null, true, "Nguoi yeu cau khong co consent DOWNLOAD de tai file nay.");
        }

        // Get encrypted content from IPFS or fallback
        string encryptedText;
        if (!string.IsNullOrEmpty(file.IpfsCid))
        {
            try
            {
                var downloadedPath = await IpfsClientService.RetrieveAsync(file.IpfsCid);
                encryptedText = await System.IO.File.ReadAllTextAsync(downloadedPath);
                if (System.IO.File.Exists(downloadedPath)) System.IO.File.Delete(downloadedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve file {FileId} from IPFS: {Cid}", fileId, file.IpfsCid);
                return (null, null, false, "Khong the tai file tu IPFS");
            }
        }
        else if (!string.IsNullOrEmpty(file.EncryptedFallbackData))
        {
            encryptedText = file.EncryptedFallbackData;
        }
        else
        {
            return (null, null, false, "Khong co du lieu file");
        }

        // Unwrap AES key — try patient private key from DB-stored wrapped key
        if (string.IsNullOrEmpty(file.EncryptedAesKey))
            return (null, null, false, "File nay khong co AES key (upload cu, chua ho tro tai ve).");

        var requesterKeyRes = await SendAuthGetAsync(authClient,
            $"/api/v1/auth/{(isPatientOwner ? normalizedRequesterId : normalizedPatientId)}/keys", bearerToken);
        if (!requesterKeyRes.IsSuccessStatusCode)
            return (null, null, false, "Khong the lay private key de giai ma file");

        var keysJson = await requesterKeyRes.Content.ReadAsStringAsync();
        var keys = JsonSerializer.Deserialize<AuthUserKeysDto>(keysJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (keys == null || string.IsNullOrEmpty(keys.EncryptedPrivateKey))
            return (null, null, false, "Private key khong ton tai");

        byte[] aesKey;
        try
        {
            var privateKey = MasterKeyEncryptionService.Decrypt(keys.EncryptedPrivateKey);
            aesKey = AsymmetricEncryptionService.UnwrapKey(file.EncryptedAesKey, privateKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unwrap AES key for file {FileId}", fileId);
            return (null, null, false, "Giai ma AES key that bai");
        }

        try
        {
            var decryptedBase64 = SymmetricEncryptionService.DecryptString(encryptedText, aesKey);
            var fileBytes = Convert.FromBase64String(decryptedBase64);
            EnqueueEhrAuditLog("DOWNLOAD", ehrId, record.PatientId, record.OrgId);
            return (fileBytes, file.FileUrl ?? "ehr_file", false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed for file {FileId}", fileId);
            return (null, null, false, "Giai ma file that bai");
        }
    }

    public async Task<bool> DeleteFileAsync(Guid ehrId, Guid fileId)
    {
        var file = await _ehrRecordRepo.GetFileByIdAsync(ehrId, fileId);
        if (file == null) return false;

        await _ehrRecordRepo.DeleteFileAsync(file);
        _logger.LogInformation("Deleted file {FileId} from EHR {EhrId}", fileId, ehrId);

        // Audit log for file deletion
        var record = await _ehrRecordRepo.GetByIdAsync(ehrId);
        if (record != null)
        {
            EnqueueEhrAuditLog("DELETE", ehrId, record.PatientId, record.OrgId);
        }

        return true;
    }

    // Private Helpers

    /// <summary>
    /// Gọi Consent Service API để kiểm tra requester có consent truy cập EHR không
    /// </summary>
    private async Task<(bool HasAccess, string? ConsentId)> VerifyConsentAsync(Guid patientId, Guid requesterId, Guid ehrId, string requiredPermission = "READ")
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ConsentService");
            
            var verifyRequest = new
            {
                PatientId = patientId,
                GranteeId = requesterId,
                EhrId = ehrId,
                RequiredPermission = requiredPermission
            };
            
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/v1/consents/verify")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(verifyRequest),
                    Encoding.UTF8,
                    "application/json")
            };

            var bearerToken = GetBearerTokenFromContext();
            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }
            
            var response = await client.SendAsync(requestMessage);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Consent Service returned {StatusCode} for verify request", response.StatusCode);
                return (false, null);
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ConsentVerifyResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return (result?.HasAccess ?? false, result?.ConsentId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Cannot reach Consent Service for EHR {EhrId} consent verification", ehrId);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error verifying consent for EHR {EhrId}", ehrId);
            return (false, null);
        }
    }

    private async Task<string?> ResolveBlockchainConsentIdAsync(string consentId)
    {
        if (!Guid.TryParse(consentId, out var parsedConsentId))
        {
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("ConsentService");
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"api/v1/consents/{parsedConsentId}");

            var bearerToken = GetBearerTokenFromContext();
            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            var response = await client.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var dataElement))
            {
                return null;
            }

            if (dataElement.TryGetProperty("blockchainConsentId", out var camelId)
                && camelId.ValueKind == JsonValueKind.String)
            {
                return camelId.GetString();
            }

            if (dataElement.TryGetProperty("BlockchainConsentId", out var pascalId)
                && pascalId.ValueKind == JsonValueKind.String)
            {
                return pascalId.GetString();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve blockchain consent id for ConsentId={ConsentId}", consentId);
            return null;
        }
    }

    private async Task<string?> GetVersionEncryptedAesKeyWithRetryAsync(Guid ehrId, int versionNumber, int maxAttempts = 8, int delayMs = 400)
    {
        if (_blockchainService == null)
            return null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var record = await _blockchainService.GetEhrHashAsync(ehrId.ToString(), versionNumber);
                if (record != null && !string.IsNullOrEmpty(record.EncryptedAesKey))
                    return record.EncryptedAesKey;

                // Fallback: tìm trong lịch sử nếu GetEhrHashAsync không trả về
                var history = await _blockchainService.GetEhrHistoryAsync(ehrId.ToString());
                var versionRecord = history?.FirstOrDefault(x => x.Version == versionNumber);
                if (versionRecord != null && !string.IsNullOrEmpty(versionRecord.EncryptedAesKey))
                    return versionRecord.EncryptedAesKey;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Attempt {Attempt}/{MaxAttempts}: cannot read EHR hash for EhrId={EhrId} Version={Version}", attempt, maxAttempts, ehrId, versionNumber);
            }

            if (attempt < maxAttempts)
                await Task.Delay(delayMs);
        }

        return null;
    }

    private async Task<string?> GetLatestEhrEncryptedAesKeyWithRetryAsync(Guid ehrId, int maxAttempts = 8, int delayMs = 400)
    {
        if (_blockchainService == null)
        {
            return null;
        }

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var ehrHashRecordList = await _blockchainService.GetEhrHistoryAsync(ehrId.ToString());
                var latestEhrHash = ehrHashRecordList?.OrderByDescending(x => x.Version).FirstOrDefault();
                if (latestEhrHash != null && !string.IsNullOrEmpty(latestEhrHash.EncryptedAesKey))
                {
                    return latestEhrHash.EncryptedAesKey;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Attempt {Attempt}/{MaxAttempts}: cannot read EHR hash history for EhrId={EhrId}", attempt, maxAttempts, ehrId);
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(delayMs);
            }
        }

        return null;
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

    private Guid? GetCurrentUserIdFromContext()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return null;
        }

        var rawId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("nameid")?.Value;

        if (Guid.TryParse(rawId, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private string GetCurrentActorType()
    {
        var role = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value
            ?? _httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;
        return role?.ToUpperInvariant() switch
        {
            "PATIENT" => "PATIENT",
            "NURSE" => "NURSE",
            "ADMIN" => "ADMIN",
            _ => "DOCTOR"
        };
    }

    private void EnqueueEhrAuditLog(string action, Guid targetId, Guid patientId, Guid? orgId = null, string result = "SUCCESS")
    {
        var currentUserId = GetCurrentUserIdFromContext();
        var auditEntry = new AuditEntry
        {
            AuditId = Guid.NewGuid().ToString(),
            ActorDid = currentUserId?.ToString() ?? orgId?.ToString() ?? "SYSTEM",
            ActorType = "USER",
            Action = action,
            TargetType = "EHR",
            TargetId = targetId.ToString(),
            PatientDid = patientId.ToString(),
            OrganizationId = orgId?.ToString(),
            Result = result,
            Timestamp = BlockchainTime.NowIsoString
        };

        _blockchainSyncService.EnqueueAuditEntry(
            auditEntry,
            onFailure: error =>
            {
                _logger.LogWarning("Queued blockchain audit log failed for EHR {EhrId}: {Error}", targetId, error);
                return Task.CompletedTask;
            });

        // Also POST to Audit Service API so audit logs are queryable in the local DB
        _ = PostAuditLogToServiceAsync(action, targetId, patientId, orgId, currentUserId, result);
    }

    /// <summary>
    /// POST audit log to the Audit Service HTTP API so it gets stored in PostgreSQL
    /// for querying by both patient and actor (doctor).
    /// </summary>
    private async Task PostAuditLogToServiceAsync(string action, Guid targetId, Guid patientId, Guid? orgId, Guid? actorUserId, string result)
    {
        try
        {
            var auditClient = _httpClientFactory.CreateClient("AuditService");
            var bearerToken = GetBearerTokenFromContext();

            var auditPayload = new
            {
                actorDid = actorUserId?.ToString() ?? orgId?.ToString() ?? "SYSTEM",
                actorUserId = actorUserId,
                actorType = actorUserId.HasValue ? GetCurrentActorType() : "SYSTEM",
                action = action,
                targetType = "EHR",
                targetId = targetId,
                patientDid = patientId.ToString(),
                patientId = patientId,
                organizationId = orgId,
                result = result,
                ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
                userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString()
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/v1/audit")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(auditPayload),
                    Encoding.UTF8,
                    "application/json")
            };

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            var response = await auditClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Audit Service returned {StatusCode} for audit log POST (EHR {EhrId}, Action={Action})",
                    response.StatusCode, targetId, action);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to POST audit log to Audit Service for EHR {EhrId}, Action={Action}", targetId, action);
        }
    }

    private async Task<(Guid? UserId, AuthUserProfileDetailDto? Profile)> GetPatientUserProfileAsync(Guid patientId)
    {
        var bearerToken = GetBearerTokenFromContext();
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return (null, null);
        }

        var userId = await _authServiceClient.GetUserIdByPatientIdAsync(patientId, bearerToken);
        if (!userId.HasValue)
        {
            return (null, null);
        }

        var profile = await _authServiceClient.GetUserProfileDetailAsync(userId.Value, bearerToken);
        return (userId, profile);
    }

    private async Task AttachPatientProfilesAsync(List<EhrRecordResponseDto> responses)
    {
        var tasks = responses.Select(async response =>
        {
            var (_, patientProfile) = await GetPatientUserProfileAsync(response.PatientId);
            response.PatientProfile = patientProfile;
        });

        await Task.WhenAll(tasks);
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

    private async Task<Guid?> ResolvePatientUserIdAsync(HttpClient authClient, Guid patientId, string? bearerToken)
    {
        var response = await SendAuthGetAsync(authClient, $"/api/v1/auth/user-id?patientId={patientId}", bearerToken);
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

    private async Task<Guid?> ResolveRequesterUserIdAsync(HttpClient authClient, Guid requesterId, string? bearerToken)
    {
        // Try as doctorId (also works when requesterId IS a userId, because Auth checks Users table first)
        var asDoctor = await SendAuthGetAsync(authClient, $"/api/v1/auth/user-id?doctorId={requesterId}", bearerToken);
        if (asDoctor.IsSuccessStatusCode)
        {
            var body = await asDoctor.Content.ReadAsStringAsync();
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
        }

        // Try as staffId (for Nurse, LabTech, Pharmacist, Receptionist roles)
        var asStaff = await SendAuthGetAsync(authClient, $"/api/v1/auth/user-id?staffId={requesterId}", bearerToken);
        if (asStaff.IsSuccessStatusCode)
        {
            var body = await asStaff.Content.ReadAsStringAsync();
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
        }

        // Try as patientId (last resort)
        var asPatient = await SendAuthGetAsync(authClient, $"/api/v1/auth/user-id?patientId={requesterId}", bearerToken);
        if (!asPatient.IsSuccessStatusCode)
        {
            return null;
        }

        var patientBody = await asPatient.Content.ReadAsStringAsync();
        using var patientDoc = JsonDocument.Parse(patientBody);
        if (patientDoc.RootElement.TryGetProperty("userId", out var patientCamelUserId)
            && patientCamelUserId.ValueKind == JsonValueKind.String
            && Guid.TryParse(patientCamelUserId.GetString(), out var patientParsedCamel))
        {
            return patientParsedCamel;
        }

        if (patientDoc.RootElement.TryGetProperty("UserId", out var patientPascalUserId)
            && patientPascalUserId.ValueKind == JsonValueKind.String
            && Guid.TryParse(patientPascalUserId.GetString(), out var patientParsedPascal))
        {
            return patientParsedPascal;
        }

        return null;
    }

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private DateTime GetLocalTime() => VietnamTimeHelper.Now;

    private static EhrRecordResponseDto MapToEhrRecordResponse(EhrRecord record)
    {
        var latestVersion = record.Versions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        
        return new EhrRecordResponseDto
        {
            EhrId = record.EhrId,
            PatientId = record.PatientId,
            EncounterId = record.EncounterId,
            OrgId = record.OrgId,
            LatestVersionInfo = latestVersion != null ? new EhrVersionDto
            {
                VersionId = latestVersion.VersionId,
                VersionNumber = latestVersion.VersionNumber,
                CreatedAt = latestVersion.CreatedAt
            } : null,
            Files = record.Files?.Select(f => new EhrFileDto
            {
                FileId = f.FileId,
                FileUrl = f.FileUrl,
                FileName = f.FileUrl,
                FileHash = f.FileHash,
                CreatedAt = f.CreatedAt
            }).ToList(),
            CreatedAt = record.CreatedAt
        };
    }
}

/// <summary>
/// Response DTO từ Consent Service verify endpoint
/// </summary>
internal class ConsentVerifyResponse
{
    public bool HasAccess { get; set; }
    public string? ConsentId { get; set; }
    public string? Scope { get; set; }
    public DateTime? ExpiresAt { get; set; }
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

