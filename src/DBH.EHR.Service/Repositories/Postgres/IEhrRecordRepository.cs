using DBH.EHR.Service.Models.Entities;

namespace DBH.EHR.Service.Repositories.Postgres;

public interface IEhrRecordRepository
{
    // Ghi (chỉ Primary)
    Task<EhrRecord> CreateAsync(EhrRecord record);
    Task<EhrVersion> CreateVersionAsync(EhrVersion version);
    Task<EhrFile> CreateFileAsync(EhrFile file);
    Task<EhrRecord> UpdateAsync(EhrRecord record);
    Task<EhrVersion> UpdateVersionAsync(EhrVersion version);
    
    // Đọc
    Task<EhrRecord?> GetByIdAsync(Guid ehrId);
    Task<EhrRecord?> GetByIdWithVersionsAsync(Guid ehrId);
    Task<IEnumerable<EhrRecord>> GetByPatientIdAsync(Guid patientId);
    Task<IEnumerable<EhrRecord>> GetByOrgIdAsync(Guid orgId);
    Task<EhrVersion?> GetLatestVersionAsync(Guid ehrId);
    Task<IEnumerable<EhrVersion>> GetVersionsAsync(Guid ehrId);
    Task<IEnumerable<EhrFile>> GetFilesAsync(Guid ehrId);
    Task<EhrVersion?> GetVersionByIdAsync(Guid ehrId, Guid versionId);
    Task<EhrFile?> GetFileByIdAsync(Guid ehrId, Guid fileId);
    Task DeleteFileAsync(EhrFile file);
}
