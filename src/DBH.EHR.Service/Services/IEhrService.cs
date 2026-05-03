using DBH.EHR.Service.Models.DTOs;

namespace DBH.EHR.Service.Services;


public interface IEhrService
{
    Task<CreateEhrRecordResponseDto> CreateEhrRecordAsync(CreateEhrRecordDto request);

    Task<EhrRecordResponseDto?> GetEhrRecordAsync(Guid ehrId);
    
    /// <summary>
    /// Láº¥y EHR vá»›i kiá»ƒm tra consent - tráº£ null náº¿u khÃ´ng cÃ³ quyá»n
    /// </summary>
    Task<(EhrRecordResponseDto? Record, bool ConsentDenied, string? DenyMessage)> GetEhrRecordWithConsentCheckAsync(
        Guid ehrId, Guid requesterId);
        
    /// <summary>
    /// Láº¥y EHR Document Ä‘Ã£ Ä‘Æ°á»£c giáº£i mÃ£ - tráº£ null náº¿u khÃ´ng cÃ³ quyá»n
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
    
    Task<IEnumerable<EhrVersionDto>> GetEhrVersionsAsync(Guid ehrId);
    
    Task<IEnumerable<EhrFileDto>> GetEhrFilesAsync(Guid ehrId);
    
    Task<EhrRecordResponseDto?> UpdateEhrRecordAsync(Guid ehrId, UpdateEhrRecordDto request);

    /// <summary>
    /// Cáº­p nháº­t EHR vá»›i kiá»ƒm tra consent WRITE â€” tráº£ ConsentDenied náº¿u khÃ´ng cÃ³ quyá»n
    /// </summary>
    Task<(EhrRecordResponseDto? Record, bool ConsentDenied, string? DenyMessage)> UpdateEhrRecordWithConsentCheckAsync(
        Guid ehrId, UpdateEhrRecordDto request, Guid requesterId);

    /// <summary>
    /// Táº£i xuá»‘ng tÃ i liá»‡u EHR vá»›i kiá»ƒm tra consent DOWNLOAD â€” bá»‡nh nhÃ¢n chá»§ sá»Ÿ há»¯u khÃ´ng cáº§n consent
    /// </summary>
    Task<(string? DecryptedData, bool ConsentDenied, string? DenyMessage)> DownloadEhrDocumentAsync(
        Guid ehrId, Guid requesterId);

    Task<EhrVersionDetailDto?> GetVersionByIdAsync(Guid ehrId, Guid versionId);

    /// <summary>
    /// L?y n?i dung tài li?u dã gi?i mã c?a m?t version EHR c? th?
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

