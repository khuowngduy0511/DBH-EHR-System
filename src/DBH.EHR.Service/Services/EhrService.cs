using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBH.EHR.Service.Models.Documents;
using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Entities;
using DBH.EHR.Service.Repositories.Mongo;
using DBH.EHR.Service.Repositories.Postgres;
using DBH.Shared.Contracts.Blockchain;
using DBH.Shared.Infrastructure.cryptography;
using DBH.Shared.Infrastructure.Ipfs;
using DBH.Shared.Infrastructure.Blockchain.Sync;
using DBH.Shared.Infrastructure.Notification;
using MongoDB.Bson;

namespace DBH.EHR.Service.Services;

/// <summary>
///  Ghi Primary + Mongo Primary, đọc từ Replica
///  Tích hợp: Blockchain hash commit + Consent verification
///  ERD-aligned: ehr_records(ehr_id, patient_id, encounter_id, org_id, data, created_at)
/// </summary>
public class EhrService : IEhrService
{
    private readonly IEhrRecordRepository _ehrRecordRepo;
    private readonly IEhrDocumentRepository _ehrDocumentRepo;
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
        IEhrDocumentRepository ehrDocumentRepo,
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
        _ehrDocumentRepo = ehrDocumentRepo;
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

        // Tạo EHR record trong PG Primary (ERD: ehr_records)
        var ehrRecord = new EhrRecord
        {
            PatientId = request.PatientId,
            EncounterId = request.EncounterId,
            OrgId = request.OrgId,
            Data = documentJson
        };
        
        var savedRecord = await _ehrRecordRepo.CreateAsync(ehrRecord);
        
        // Tạo version đầu tiên (ERD: ehr_versions)
        var version = new EhrVersion
        {
            EhrId = savedRecord.EhrId,
            VersionNumber = 1,
            Data = documentJson
        };
        
        var savedVersion = await _ehrRecordRepo.CreateVersionAsync(version);
        
        // Tạo EHR file (ERD: ehr_files)
        var file = new EhrFile
        {
            EhrId = savedRecord.EhrId,
            FileHash = dataHash
        };
        
        var savedFile = await _ehrRecordRepo.CreateFileAsync(file);
        
        // Lưu document vào Mongo Primary 
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
            ipfsCid = "";
        }

        BsonDocument encryptedDataBson;
        if (string.IsNullOrEmpty(ipfsCid) || ipfsCid == "IPFS_UPLOAD_FAILED")
        {
            encryptedDataBson = new BsonDocument("encryptedText", encryptedDataStr);
        }
        else
        {
            encryptedDataBson = new BsonDocument("ipfsCid", ipfsCid);
        }
        
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
            Data = encryptedDataBson,
            DataHash = dataHash,
            VersionNumber = 1
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
                    CreatedByDid = $"did:fabric:org:{request.OrgId}",
                    OrganizationId = request.OrgId.ToString(),
                    Version = 1,
                    ContentHash = $"sha256:{dataHash}",
                    FileHash = $"sha256:{dataHash}",
                    Timestamp = DateTime.UtcNow.ToString("o"),
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
        
        _logger.LogInformation(
            "Tạo EHR {EhrId} version {VersionId} file {FileId} với MongoDB doc {DocId}",
            savedRecord.EhrId, savedVersion.VersionId, savedFile.FileId, savedDocument.Id);

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
            OffchainDocId = savedDocument.Id,
            DataHash = dataHash,
            CreatedAt = savedRecord.CreatedAt
        };
    }

    public async Task<EhrRecordResponseDto?> GetEhrRecordAsync(Guid ehrId, bool useReplica = false)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId, useReplica);
        if (record == null) return null;

        var response = MapToEhrRecordResponse(record);
        var (_, patientProfile) = await GetPatientUserProfileAsync(response.PatientId);
        response.PatientProfile = patientProfile;
        return response;
    }

    /// <inheritdoc />
    public async Task<(EhrRecordResponseDto? Record, bool ConsentDenied, string? DenyMessage)> GetEhrRecordWithConsentCheckAsync(
        Guid ehrId, Guid requesterId, bool useReplica = false)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId, useReplica);
        if (record == null)
            return (null, false, null);

        // Bypass consent check nếu requester là chính bệnh nhân
        if (requesterId == record.PatientId)
        {
            _logger.LogInformation("Consent bypass: requester {RequesterId} is owner of EHR {EhrId}", requesterId, ehrId);
            var response = MapToEhrRecordResponse(record);
            var (_, patientProfile) = await GetPatientUserProfileAsync(response.PatientId);
            response.PatientProfile = patientProfile;
            return (response, false, null);
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
        var consentedResponse = MapToEhrRecordResponse(record);
        var (_, consentedPatientProfile) = await GetPatientUserProfileAsync(consentedResponse.PatientId);
        consentedResponse.PatientProfile = consentedPatientProfile;
        return (consentedResponse, false, null);
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
        var responses = records.Select(r => MapToEhrRecordResponse(r)).ToList();
        await AttachPatientProfilesAsync(responses);
        return responses;
    }

    public async Task<IEnumerable<EhrRecordResponseDto>> GetOrgEhrRecordsAsync(Guid orgId, bool useReplica = false)
    {
        var records = await _ehrRecordRepo.GetByOrgIdAsync(orgId, useReplica);
        var responses = records.Select(r => MapToEhrRecordResponse(r)).ToList();
        await AttachPatientProfilesAsync(responses);
        return responses;
    }

    public async Task<IEnumerable<EhrVersionDto>> GetEhrVersionsAsync(Guid ehrId, bool useReplica = false)
    {
        var versions = await _ehrRecordRepo.GetVersionsAsync(ehrId, useReplica);
        return versions.Select(v => new EhrVersionDto
        {
            VersionId = v.VersionId,
            VersionNumber = v.VersionNumber,
            CreatedAt = v.CreatedAt
        });
    }

    public async Task<IEnumerable<EhrFileDto>> GetEhrFilesAsync(Guid ehrId, bool useReplica = false)
    {
        var files = await _ehrRecordRepo.GetFilesAsync(ehrId, useReplica);
        return files.Select(f => new EhrFileDto
        {
            FileId = f.FileId,
            FileUrl = f.FileUrl,
            FileHash = f.FileHash,
            CreatedAt = f.CreatedAt
        });
    }

    public async Task<EhrRecordResponseDto?> UpdateEhrRecordAsync(Guid ehrId, UpdateEhrRecordDto request)
    {
        var record = await _ehrRecordRepo.GetByIdWithVersionsAsync(ehrId);
        if (record == null) return null;

        var documentJson = request.Data.GetRawText();
        var dataHash = ComputeHash(documentJson);

        // Update the record's data
        record.Data = documentJson;
        await _ehrRecordRepo.UpdateAsync(record);

        // Create new version
        var latestVersion = record.Versions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        var newVersionNumber = (latestVersion?.VersionNumber ?? 0) + 1;

        var version = new EhrVersion
        {
            EhrId = ehrId,
            VersionNumber = newVersionNumber,
            Data = documentJson
        };
        await _ehrRecordRepo.CreateVersionAsync(version);

        _logger.LogInformation("Updated EHR {EhrId} to version {Version}", ehrId, newVersionNumber);

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

    public async Task<EhrVersionDetailDto?> GetVersionByIdAsync(Guid ehrId, Guid versionId, bool useReplica = false)
    {
        var version = await _ehrRecordRepo.GetVersionByIdAsync(ehrId, versionId, useReplica);
        if (version == null) return null;

        JsonElement? dataElement = null;
        if (!string.IsNullOrEmpty(version.Data))
        {
            dataElement = JsonSerializer.Deserialize<JsonElement>(version.Data);
        }

        return new EhrVersionDetailDto
        {
            VersionId = version.VersionId,
            EhrId = version.EhrId,
            VersionNumber = version.VersionNumber,
            Data = dataElement,
            CreatedAt = version.CreatedAt
        };
    }

    public async Task<EhrFileDto?> AddFileAsync(Guid ehrId, AddEhrFileDto request)
    {
        var record = await _ehrRecordRepo.GetByIdAsync(ehrId);
        if (record == null) return null;

        var file = new EhrFile
        {
            EhrId = ehrId,
            FileUrl = request.FileUrl,
            FileHash = request.FileHash
        };
        var savedFile = await _ehrRecordRepo.CreateFileAsync(file);

        _logger.LogInformation("Added file {FileId} to EHR {EhrId}", savedFile.FileId, ehrId);

        // Notify patient about new file
        if (_notificationClient != null)
        {
            await _notificationClient.SendAsync(
                record.PatientId,
                "File mới trong hồ sơ bệnh án",
                "Một file mới đã được thêm vào hồ sơ bệnh án của bạn.",
                "EhrFileAdded", "Low",
                ehrId.ToString(), "EHR");
        }

        return new EhrFileDto
        {
            FileId = savedFile.FileId,
            FileUrl = savedFile.FileUrl,
            FileHash = savedFile.FileHash,
            CreatedAt = savedFile.CreatedAt
        };
    }

    public async Task<bool> DeleteFileAsync(Guid ehrId, Guid fileId)
    {
        var file = await _ehrRecordRepo.GetFileByIdAsync(ehrId, fileId);
        if (file == null) return false;

        await _ehrRecordRepo.DeleteFileAsync(file);
        _logger.LogInformation("Deleted file {FileId} from EHR {EhrId}", fileId, ehrId);
        return true;
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
            
            var response = await client.PostAsync("api/v1/consents/verify", content);
            
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

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

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
