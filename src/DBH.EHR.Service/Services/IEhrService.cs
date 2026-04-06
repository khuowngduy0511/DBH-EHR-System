using DBH.EHR.Service.Models.DTOs;

namespace DBH.EHR.Service.Services;


public interface IEhrService
{
    Task<CreateEhrRecordResponseDto> CreateEhrRecordAsync(CreateEhrRecordDto request);

    Task<EhrRecordResponseDto?> GetEhrRecordAsync(Guid ehrId, bool useReplica = false);
    
    /// <summary>
    /// Lấy EHR với kiểm tra consent - trả null nếu không có quyền
    /// </summary>
    Task<(EhrRecordResponseDto? Record, bool ConsentDenied, string? DenyMessage)> GetEhrRecordWithConsentCheckAsync(
        Guid ehrId, Guid requesterId, bool useReplica = false);
        
    /// <summary>
    /// Lấy EHR Document đã được giải mã - trả null nếu không có quyền
    /// </summary>
    Task<(string? DecryptedData, bool ConsentDenied, string? DenyMessage)> GetEhrDocumentAsync(
        Guid ehrId, Guid requesterId, bool useReplica = false);
    
    Task<IEnumerable<EhrRecordResponseDto>> GetPatientEhrRecordsAsync(Guid patientId, bool useReplica = false);

    Task<IEnumerable<EhrRecordResponseDto>> GetOrgEhrRecordsAsync(Guid orgId, bool useReplica = false);
    
    Task<IEnumerable<EhrVersionDto>> GetEhrVersionsAsync(Guid ehrId, bool useReplica = false);
    
    Task<IEnumerable<EhrFileDto>> GetEhrFilesAsync(Guid ehrId, bool useReplica = false);
    
    Task<EhrRecordResponseDto?> UpdateEhrRecordAsync(Guid ehrId, UpdateEhrRecordDto request);
    
    Task<EhrVersionDetailDto?> GetVersionByIdAsync(Guid ehrId, Guid versionId, bool useReplica = false);
    
    Task<EhrFileDto?> AddFileAsync(Guid ehrId, Stream fileStream, string fileName);
    
    Task<bool> DeleteFileAsync(Guid ehrId, Guid fileId);
}
