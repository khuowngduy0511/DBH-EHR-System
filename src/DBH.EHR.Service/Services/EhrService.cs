using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBH.EHR.Service.Models.Documents;
using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Entities;
using DBH.EHR.Service.Models.Enums;
using DBH.EHR.Service.Repositories.Mongo;
using DBH.EHR.Service.Repositories.Postgres;
using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.cryptography;
using DBH.Shared.Infrastructure.Ipfs;
using MongoDB.Bson;

namespace DBH.EHR.Service.Services;

/// <summary>
///  Ghi Primary + Mongo Primary, đọc từ Replica
///  Tích hợp: Blockchain hash commit + Consent verification
/// </summary>
public class EhrService : IEhrService
{
    private readonly IEhrRecordRepository _ehrRecordRepo;
    private readonly IEhrDocumentRepository _ehrDocumentRepo;
    private readonly ILogger<EhrService> _logger;
    private readonly IEhrBlockchainService? _blockchainService;
    private readonly IConsentBlockchainService? _consentBlockchainService;
    private readonly IHttpClientFactory _httpClientFactory;

    public EhrService(
        IEhrRecordRepository ehrRecordRepo,
        IEhrDocumentRepository ehrDocumentRepo,
        ILogger<EhrService> logger,
        IHttpClientFactory httpClientFactory,
        IEhrBlockchainService? blockchainService = null,
        IConsentBlockchainService? consentBlockchainService = null)
    {
        _ehrRecordRepo = ehrRecordRepo;
        _ehrDocumentRepo = ehrDocumentRepo;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _blockchainService = blockchainService;
        _consentBlockchainService = consentBlockchainService;
    }

    // EHR Records 

    public async Task<CreateEhrRecordResponseDto> CreateEhrRecordAsync(CreateEhrRecordDto request)
    {
        _logger.LogInformation(
            "Tạo EHR cho bệnh nhân {PatientId} bởi bác sĩ {DoctorId}, loại: {ReportType}",
            request.PatientId, request.CreatedByDoctorId, request.ReportType);

        // Tạo EHR record trong PG Primary
        var ehrRecord = new EhrRecord
        {
            PatientId = request.PatientId,
            CreatedByDoctorId = request.CreatedByDoctorId,
            EncounterId = request.EncounterId,
            HospitalId = request.HospitalId,
            CurrentVersion = 1
        };
        
        var savedRecord = await _ehrRecordRepo.CreateAsync(ehrRecord);
        
        // Tạo version đầu tiên
        var documentJson = request.Data.GetRawText();
        var dataHash = ComputeHash(documentJson);
        
        var version = new EhrVersion
        {
            EhrId = savedRecord.EhrId,
            Version = 1,
            FileHash = dataHash,
            ChangedBy = request.CreatedByDoctorId,
            ChangeReason = "Tạo mới",
            TxStatus = TxStatus.PENDING
        };
        
        var savedVersion = await _ehrRecordRepo.CreateVersionAsync(version);
        
        // Tạo EHR file
        var metadataJson = request.Metadata?.GetRawText();
        var file = new EhrFile
        {
            EhrId = savedRecord.EhrId,
            Version = 1,
            ReportType = request.ReportType,
            FileHash = dataHash,
            MimeType = "application/json",
            SizeBytes = Encoding.UTF8.GetByteCount(documentJson),
            CreatedBy = request.CreatedByDoctorId,
            Metadata = metadataJson
        };
        
        var savedFile = await _ehrRecordRepo.CreateFileAsync(file);
        
        //Lưu document vào Mongo Primary 
        // Parse metadata chỉ khi nó là JSON object
        BsonDocument? metadataBson = null;
        if (metadataJson != null)
        {
            try
            {
                metadataBson = BsonDocument.Parse(metadataJson);
            }
            catch
            {
                // Nếu metadata không phải JSON object, wrap nó
                metadataBson = new BsonDocument("value", metadataJson);
            }
        }
        
        // Generate AES-256 Blue Key and encrypt data
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        var blueKeyBytes = aes.Key;

        var encryptedDataStr = SymmetricEncryptionService.EncryptString(documentJson, blueKeyBytes);
        
        // Upload to IPFS
        string ipfsCid = string.Empty;
        try 
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, encryptedDataStr);
            var uploadRes = await IpfsClientService.UploadAsync(tempFile);
            if (uploadRes != null)
            {
                ipfsCid = uploadRes.Hash;
            }
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload encrypted EHR to IPFS");
            ipfsCid = "IPFS_UPLOAD_FAILED";
        }

        var encryptedDataBson = new BsonDocument("ipfsCid", ipfsCid);
        
        // Wrap AES blue key with Patient's Public Key
        string encryptedAesKey = string.Empty;
        try 
        {
            var authClient = _httpClientFactory.CreateClient("AuthService");
            var keyRes = await authClient.GetAsync($"/api/v1/auth/{request.PatientId}/keys");
            if (keyRes.IsSuccessStatusCode)
            {
                var keysJson = await keyRes.Content.ReadAsStringAsync();
                var keys = JsonSerializer.Deserialize<AuthUserKeysDto>(keysJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (keys != null && !string.IsNullOrEmpty(keys.PublicKey))
                {
                    encryptedAesKey = AsymmetricEncryptionService.WrapKey(blueKeyBytes, keys.PublicKey);
                }
            }
            else 
            {
                _logger.LogWarning("Failed to retrieve Patient Keys from Auth Service for encryption.");
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error calling Auth Service for patient keys");
        }

        var ehrDocument = new EhrDocument
        {
            EhrId = savedRecord.EhrId,
            VersionId = savedVersion.VersionId,
            FileId = savedFile.FileId,
            PatientId = request.PatientId,
            ReportType = request.ReportType.ToString(),
            Data = encryptedDataBson,
            DataHash = dataHash,
            Version = 1,
            CreatedBy = request.CreatedByDoctorId,
            Metadata = metadataBson
        };
        
        var savedDocument = await _ehrDocumentRepo.CreateAsync(ehrDocument);
        
        // Cập nhật file_url 
        savedFile.FileUrl = $"mongodb://ehr_documents/{savedDocument.Id}";

        // === Blockchain: Commit EHR hash lên Hyperledger Fabric ===
        if (_blockchainService != null)
        {
            try
            {
                var ehrHashRecord = new EhrHashRecord
                {
                    EhrId = savedRecord.EhrId.ToString(),
                    PatientDid = $"did:fabric:patient:{request.PatientId}",
                    CreatedByDid = $"did:fabric:doctor:{request.CreatedByDoctorId}",
                    OrganizationId = request.HospitalId.ToString(),
                    Version = 1,
                    ContentHash = $"sha256:{dataHash}",
                    FileHash = $"sha256:{dataHash}",
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    EncryptedAesKey = encryptedAesKey
                };

                var txResult = await _blockchainService.CommitEhrHashAsync(ehrHashRecord);
                if (txResult.Success)
                {
                    savedVersion.TxStatus = TxStatus.COMMITTED;
                    savedVersion.BlockchainTxHash = txResult.TxHash;
                    _logger.LogInformation("EHR hash committed to blockchain: {TxHash}", txResult.TxHash);
                }
                else
                {
                    _logger.LogWarning("Blockchain hash commit failed: {Error}", txResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Blockchain hash commit exception for EHR {EhrId}", savedRecord.EhrId);
            }
        }
        
        _logger.LogInformation(
            "Tạo EHR {EhrId} version {VersionId} file {FileId} với MongoDB doc {DocId}",
            savedRecord.EhrId, savedVersion.VersionId, savedFile.FileId, savedDocument.Id);

        return new CreateEhrRecordResponseDto
        {
            EhrId = savedRecord.EhrId,
            VersionId = savedVersion.VersionId,
            FileId = savedFile.FileId,
            Version = 1,
            OffchainDocId = savedDocument.Id,
            DataHash = dataHash,
            CreatedAt = savedRecord.CreatedAt
        };
    }

    public async Task<EhrRecordResponseDto?> GetEhrRecordAsync(Guid ehrId, bool useReplica = false)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId, useReplica);
        if (record == null) return null;
        
        return MapToEhrRecordResponse(record, useReplica);
    }

    /// <inheritdoc />
    public async Task<(EhrRecordResponseDto? Record, bool ConsentDenied, string? DenyMessage)> GetEhrRecordWithConsentCheckAsync(
        Guid ehrId, Guid requesterId, bool useReplica = false)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId, useReplica);
        if (record == null)
            return (null, false, null);

        // Bypass consent check nếu requester là chính bệnh nhân hoặc bác sĩ tạo
        if (requesterId == record.PatientId || requesterId == record.CreatedByDoctorId)
        {
            _logger.LogInformation("Consent bypass: requester {RequesterId} is owner/creator of EHR {EhrId}", requesterId, ehrId);
            return (MapToEhrRecordResponse(record, useReplica), false, null);
        }

        // Gọi Consent Service để kiểm tra quyền truy cập
        var consentResult = await VerifyConsentAsync(record.PatientId, requesterId, ehrId);
        if (!consentResult.HasAccess)
        {
            _logger.LogWarning("Consent denied: requester {RequesterId} has no consent for EHR {EhrId} of patient {PatientId}",
                requesterId, ehrId, record.PatientId);
            return (null, true, $"Requester {requesterId} không có consent để truy cập EHR {ehrId} của bệnh nhân {record.PatientId}");
        }

        _logger.LogInformation("Consent verified: requester {RequesterId} granted access to EHR {EhrId}", requesterId, ehrId);
        return (MapToEhrRecordResponse(record, useReplica), false, null);
    }

    public async Task<(string? DecryptedData, bool ConsentDenied, string? DenyMessage)> GetEhrDocumentAsync(Guid ehrId, Guid requesterId, bool useReplica = false)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId, useReplica);
        if (record == null)
            return (null, false, "EHR Record not found");

        var ehrDoc = await _ehrDocumentRepo.GetByEhrIdAsync(ehrId, useReplica);
        if (ehrDoc == null)
             return (null, false, "MongoDB Document not found");

        if (!ehrDoc.Data.Contains("encryptedText") && !ehrDoc.Data.Contains("ipfsCid"))
             return (ehrDoc.Data.ToJson(), false, null); // Legacy plain-text support

        string encryptedText = string.Empty;
        if (ehrDoc.Data.Contains("ipfsCid"))
        {
            var cid = ehrDoc.Data["ipfsCid"].AsString;
            try
            {
                var downloadedPath = await IpfsClientService.RetrieveAsync(cid);
                encryptedText = await File.ReadAllTextAsync(downloadedPath);
                if (File.Exists(downloadedPath)) File.Delete(downloadedPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve encrypted EHR from IPFS: {Cid}", cid);
                return (null, false, "Failed to retrieve document from IPFS");
            }
        }
        else
        {
            encryptedText = ehrDoc.Data["encryptedText"].AsString;
        }

        var authClient = _httpClientFactory.CreateClient("AuthService");
        var requesterKeyRes = await authClient.GetAsync($"/api/v1/auth/{requesterId}/keys");
        if (!requesterKeyRes.IsSuccessStatusCode)
           return (null, false, "Cannot fetch requester keys from Auth Service");
           
        var keysJson = await requesterKeyRes.Content.ReadAsStringAsync();
        var keys = JsonSerializer.Deserialize<AuthUserKeysDto>(keysJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (keys == null || string.IsNullOrEmpty(keys.EncryptedPrivateKey))
             return (null, false, "Requester missing encrypted private key.");

        var privateKey = MasterKeyEncryptionService.Decrypt(keys.EncryptedPrivateKey);

        byte[]? blueKeyBytes = null;

        if (requesterId == record.PatientId)
        {
             if (_blockchainService != null) {
                  var ehrHashRecordList = await _blockchainService.GetEhrHistoryAsync(ehrId.ToString());
                  var latestEhrHash = ehrHashRecordList?.OrderByDescending(x => x.Version).FirstOrDefault();
                  if (latestEhrHash != null && !string.IsNullOrEmpty(latestEhrHash.EncryptedAesKey))
                  {
                       blueKeyBytes = AsymmetricEncryptionService.UnwrapKey(latestEhrHash.EncryptedAesKey, privateKey);
                  }
             }
        }
        else 
        {
             var consentResult = await VerifyConsentAsync(record.PatientId, requesterId, ehrId);
             if (!consentResult.HasAccess || string.IsNullOrEmpty(consentResult.ConsentId))
             {
                  return (null, true, "Requester does not have consent to read this EHR.");
             }

             if (_consentBlockchainService != null) {
                  var consentRecord = await _consentBlockchainService.GetConsentAsync(consentResult.ConsentId);
                  if (consentRecord != null && !string.IsNullOrEmpty(consentRecord.EncryptedAesKey))
                  {
                       blueKeyBytes = AsymmetricEncryptionService.UnwrapKey(consentRecord.EncryptedAesKey, privateKey);
                  }
             }
        }

        if (blueKeyBytes == null)
            return (null, false, "Unable to extract AES wrapper key from ledgers");

        try {
            var decryptedPayload = SymmetricEncryptionService.DecryptString(encryptedText, blueKeyBytes);
            return (decryptedPayload, false, null);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to decrypt document payload for EHR {EhrId}", ehrId);
            return (null, false, "Decryption failed due to invalid key or corrupted payload.");
        }
    }

    public async Task<IEnumerable<EhrRecordResponseDto>> GetPatientEhrRecordsAsync(Guid patientId, bool useReplica = false)
    {
        var records = await _ehrRecordRepo.GetByPatientIdAsync(patientId, useReplica);
        return records.Select(r => MapToEhrRecordResponse(r, useReplica));
    }

    public async Task<IEnumerable<EhrRecordResponseDto>> GetHospitalEhrRecordsAsync(Guid hospitalId, bool useReplica = false)
    {
        var records = await _ehrRecordRepo.GetByHospitalIdAsync(hospitalId, useReplica);
        return records.Select(r => MapToEhrRecordResponse(r, useReplica));
    }

    public async Task<IEnumerable<EhrVersionDto>> GetEhrVersionsAsync(Guid ehrId, bool useReplica = false)
    {
        var versions = await _ehrRecordRepo.GetVersionsAsync(ehrId, useReplica);
        return versions.Select(v => new EhrVersionDto
        {
            VersionId = v.VersionId,
            Version = v.Version,
            FileHash = v.FileHash,
            ChangedBy = v.ChangedBy,
            ChangeReason = v.ChangeReason,
            TxStatus = v.TxStatus.ToString(),
            BlockchainTxHash = v.BlockchainTxHash,
            CreatedAt = v.CreatedAt
        });
    }

    public async Task<IEnumerable<EhrFileDto>> GetEhrFilesAsync(Guid ehrId, int? version = null, bool useReplica = false)
    {
        var files = await _ehrRecordRepo.GetFilesAsync(ehrId, version, useReplica);
        return files.Select(f => new EhrFileDto
        {
            FileId = f.FileId,
            Version = f.Version,
            ReportType = f.ReportType.ToString(),
            FileUrl = f.FileUrl,
            FileHash = f.FileHash,
            MimeType = f.MimeType,
            SizeBytes = f.SizeBytes,
            CreatedBy = f.CreatedBy,
            CreatedAt = f.CreatedAt
        });
    }

    // Private Helpers

    /// <summary>
    /// Gọi Consent Service API để kiểm tra requester có consent truy cập EHR không
    /// </summary>
    private async Task<(bool HasAccess, string? ConsentId)> VerifyConsentAsync(Guid patientId, Guid requesterId, Guid ehrId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ConsentService");
            
            // POST /api/consents/verify với body VerifyConsentRequest
            var verifyRequest = new
            {
                PatientId = patientId,
                GranteeId = requesterId,
                EhrId = ehrId
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(verifyRequest),
                Encoding.UTF8,
                "application/json");
            
            var response = await client.PostAsync("api/consents/verify", content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Consent Service returned {StatusCode} for verify request", response.StatusCode);
                // Fail-closed: nếu Consent Service không available, từ chối truy cập
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
            // Fail-closed: không kết nối được thì từ chối
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error verifying consent for EHR {EhrId}", ehrId);
            return (false, null);
        }
    }

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static EhrRecordResponseDto MapToEhrRecordResponse(EhrRecord record, bool usedReplica)
    {
        var latestVersion = record.Versions?.OrderByDescending(v => v.Version).FirstOrDefault();
        
        return new EhrRecordResponseDto
        {
            EhrId = record.EhrId,
            PatientId = record.PatientId,
            EncounterId = record.EncounterId,
            HospitalId = record.HospitalId,
            CurrentVersion = record.CurrentVersion,
            LatestVersionInfo = latestVersion != null ? new EhrVersionDto
            {
                VersionId = latestVersion.VersionId,
                Version = latestVersion.Version,
                FileHash = latestVersion.FileHash,
                ChangedBy = latestVersion.ChangedBy,
                ChangeReason = latestVersion.ChangeReason,
                TxStatus = latestVersion.TxStatus.ToString(),
                BlockchainTxHash = latestVersion.BlockchainTxHash,
                CreatedAt = latestVersion.CreatedAt
            } : null,
            Files = record.Files?.Select(f => new EhrFileDto
            {
                FileId = f.FileId,
                Version = f.Version,
                ReportType = f.ReportType.ToString(),
                FileUrl = f.FileUrl,
                FileHash = f.FileHash,
                MimeType = f.MimeType,
                SizeBytes = f.SizeBytes,
                CreatedBy = f.CreatedBy,
                CreatedAt = f.CreatedAt
            }).ToList(),
            CreatedByDoctorId = record.CreatedByDoctorId,
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
