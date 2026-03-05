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
    private readonly IHttpClientFactory _httpClientFactory;

    public EhrService(
        IEhrRecordRepository ehrRecordRepo,
        IEhrDocumentRepository ehrDocumentRepo,
        ILogger<EhrService> logger,
        IHttpClientFactory httpClientFactory,
        IEhrBlockchainService? blockchainService = null)
    {
        _ehrRecordRepo = ehrRecordRepo;
        _ehrDocumentRepo = ehrDocumentRepo;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _blockchainService = blockchainService;
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
        
        var ehrDocument = new EhrDocument
        {
            EhrId = savedRecord.EhrId,
            VersionId = savedVersion.VersionId,
            FileId = savedFile.FileId,
            PatientId = request.PatientId,
            ReportType = request.ReportType.ToString(),
            Data = BsonDocument.Parse(documentJson),
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
                    Timestamp = DateTime.UtcNow.ToString("o")
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
        var hasConsent = await VerifyConsentAsync(record.PatientId, requesterId, ehrId);
        if (!hasConsent)
        {
            _logger.LogWarning("Consent denied: requester {RequesterId} has no consent for EHR {EhrId} of patient {PatientId}",
                requesterId, ehrId, record.PatientId);
            return (null, true, $"Requester {requesterId} không có consent để truy cập EHR {ehrId} của bệnh nhân {record.PatientId}");
        }

        _logger.LogInformation("Consent verified: requester {RequesterId} granted access to EHR {EhrId}", requesterId, ehrId);
        return (MapToEhrRecordResponse(record, useReplica), false, null);
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
    private async Task<bool> VerifyConsentAsync(Guid patientId, Guid requesterId, Guid ehrId)
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
                return false;
            }
            
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ConsentVerifyResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            return result?.HasAccess ?? false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Cannot reach Consent Service for EHR {EhrId} consent verification", ehrId);
            // Fail-closed: không kết nối được thì từ chối
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error verifying consent for EHR {EhrId}", ehrId);
            return false;
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
