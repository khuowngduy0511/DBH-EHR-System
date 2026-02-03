using System.Security.Cryptography;
using System.Text;
using DBH.EHR.Service.Models.Documents;
using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Entities;
using DBH.EHR.Service.Models.Enums;
using DBH.EHR.Service.Repositories.Mongo;
using DBH.EHR.Service.Repositories.Postgres;
using MongoDB.Bson;

namespace DBH.EHR.Service.Services;

/// <summary>
///  Ghi Primary + Mongo Primary, đọc từ Replica
/// </summary>
public class EhrService : IEhrService
{
    private readonly IEhrRecordRepository _ehrRecordRepo;
    private readonly IEhrDocumentRepository _ehrDocumentRepo;
    private readonly ILogger<EhrService> _logger;

    public EhrService(
        IEhrRecordRepository ehrRecordRepo,
        IEhrDocumentRepository ehrDocumentRepo,
        ILogger<EhrService> logger)
    {
        _ehrRecordRepo = ehrRecordRepo;
        _ehrDocumentRepo = ehrDocumentRepo;
        _logger = logger;
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
            Metadata = metadataJson != null 
                ? BsonDocument.Parse(metadataJson) 
                : null
        };
        
        var savedDocument = await _ehrDocumentRepo.CreateAsync(ehrDocument);
        
        // Cập nhật file_url 
        savedFile.FileUrl = $"mongodb://ehr_documents/{savedDocument.Id}";
        
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
