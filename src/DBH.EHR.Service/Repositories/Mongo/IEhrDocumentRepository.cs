using DBH.EHR.Service.Models.Documents;
using DBH.EHR.Service.Models.Enums;

namespace DBH.EHR.Service.Repositories.Mongo;

/// <summary>
/// Interface quản lý EhrDocument trên MongoDB
/// </summary>
public interface IEhrDocumentRepository
{
    // Ghi (Primary)
    Task<EhrDocument> CreateAsync(EhrDocument document);
    
    // Đọc (Primary hoặc Secondary)
    Task<EhrDocument?> GetByIdAsync(string id, bool useSecondary = false);
    Task<EhrDocument?> GetByFileIdAsync(Guid fileId, bool useSecondary = false);
    Task<EhrDocument?> GetByEhrIdAsync(Guid ehrId, bool useSecondary = false);
    Task<EhrDocument?> GetByVersionIdAsync(Guid versionId, bool useSecondary = false);
    Task<IEnumerable<EhrDocument>> GetByPatientIdAsync(Guid patientId, bool useSecondary = false);
    Task<IEnumerable<EhrDocument>> GetVersionsByEhrIdAsync(Guid ehrId, bool useSecondary = false);
    Task<IEnumerable<EhrDocument>> GetByReportTypeAsync(Guid patientId, ReportType reportType, bool useSecondary = false);
    
    // Kiểm tra replication
    Task<bool> ExistsOnSecondaryAsync(string id);
    Task<string> GetReadNodeInfoAsync(bool useSecondary = false);
}
