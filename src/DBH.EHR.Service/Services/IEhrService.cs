using DBH.EHR.Service.Models.DTOs;

namespace DBH.EHR.Service.Services;


public interface IEhrService
{
    Task<EhrResponse<CreateEhrRecordResponseDto>> CreateEhrRecordAsync(CreateEhrRecordDto request);

    Task<EhrResponse<EhrRecordResponseDto>> GetEhrRecordAsync(Guid ehrId);
    
    /// <summary>
    /// Lấy EHR với kiểm tra consent - trả null nếu không có quyền
    /// </summary>
    Task<(EhrRecordResponseDto? Record, bool ConsentDenied, string? DenyMessage)> GetEhrRecordWithConsentCheckAsync(
        Guid ehrId, Guid requesterId);
        
    /// <summary>
    /// Lấy EHR Document đã giải mã - trả null nếu không có quyền
    /// </summary>
    Task<(string? DecryptedData, bool ConsentDenied, string? DenyMessage)> GetEhrDocumentAsync(
        Guid ehrId, Guid requesterId);

    Task<(string? DecryptedData, bool Forbidden, string? Message)> GetEhrDocumentForCurrentUserAsync(Guid ehrId);

    Task<string?> DownloadIpfsRawAsync(string ipfsCid);

    Task<IpfsRawDownloadResponseDto?> DownloadLatestIpfsRawByEhrIdAsync(Guid ehrId);

    Task<EncryptIpfsPayloadResponseDto?> EncryptToIpfsForCurrentUserAsync(EncryptIpfsPayloadRequestDto request);

    Task<string?> DecryptIpfsForCurrentUserAsync(DecryptIpfsPayloadRequestDto request);
    
    Task<IEnumerable<EhrRecordResponseDto>> GetPatientEhrRecordsAsync(Guid patientId, Guid? requesterId = null);

    /// <summary>
    /// Lấy metadata tối thiểu (ehrId, orgId, createdAt...) — không kiểm tra consent.
    /// Dùng để xác định patient có hồ sơ không trước khi gửi access request.
    /// </summary>
    Task<IEnumerable<EhrMetadataDto>> GetPatientEhrMetadataAsync(Guid patientId);

    Task<IEnumerable<EhrRecordResponseDto>> GetOrgEhrRecordsAsync(Guid orgId);
    
    Task<PaginatedResult<EhrRecordResponseDto>> GetMyVisibleRecordsAsync(int page, int pageSize, string? search);
    
    Task<IEnumerable<EhrVersionDto>> GetEhrVersionsAsync(Guid ehrId);
    
    Task<IEnumerable<EhrFileDto>> GetEhrFilesAsync(Guid ehrId);
    
    Task<EhrRecordResponseDto?> UpdateEhrRecordAsync(Guid ehrId, UpdateEhrRecordDto request);

    /// <summary>
    /// Cập nhật EHR với kiểm tra consent WRITE — trả ConsentDenied nếu không có quyền
    /// </summary>
    Task<(EhrRecordResponseDto? Record, bool ConsentDenied, string? DenyMessage)> UpdateEhrRecordWithConsentCheckAsync(
        Guid ehrId, UpdateEhrRecordDto request, Guid requesterId);

    /// <summary>
    /// Tải xuống tài liệu EHR với kiểm tra consent DOWNLOAD — bệnh nhân chủ sở hữu không cần consent
    /// </summary>
    Task<(string? DecryptedData, bool ConsentDenied, string? DenyMessage)> DownloadEhrDocumentAsync(
        Guid ehrId, Guid requesterId);

    Task<EhrVersionDetailDto?> GetVersionByIdAsync(Guid ehrId, Guid versionId);

    /// <summary>
    /// Lấy nội dung tài liệu đã giải mã của một version EHR cụ thể
    /// </summary>
    Task<(EhrVersionDocumentResponseDto? Result, bool ConsentDenied, string? DenyMessage)> GetVersionDocumentAsync(
        Guid ehrId, Guid versionId, Guid requesterId);
    
    Task<EhrFileDto?> AddFileAsync(Guid ehrId, Stream fileStream, string fileName);
    
    Task<bool> DeleteFileAsync(Guid ehrId, Guid fileId);

    /// <summary>
    /// Giai ma va tra ve noi dung file dinh kem
    /// </summary>
    Task<(byte[]? Content, string? FileName, bool ConsentDenied, string? DenyMessage)>
        DownloadFileAsync(Guid ehrId, Guid fileId, Guid requesterId);
}

