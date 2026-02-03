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
    
    // Đọc (Primary hoặc Replica)
    Task<EhrRecord?> GetByIdAsync(Guid ehrId, bool useReplica = false);
    Task<EhrRecord?> GetByIdWithVersionsAsync(Guid ehrId, bool useReplica = false);
    Task<IEnumerable<EhrRecord>> GetByPatientIdAsync(Guid patientId, bool useReplica = false);
    Task<IEnumerable<EhrRecord>> GetByDoctorIdAsync(Guid doctorId, bool useReplica = false);
    Task<IEnumerable<EhrRecord>> GetByHospitalIdAsync(Guid hospitalId, bool useReplica = false);
    Task<EhrVersion?> GetLatestVersionAsync(Guid ehrId, bool useReplica = false);
    Task<IEnumerable<EhrVersion>> GetVersionsAsync(Guid ehrId, bool useReplica = false);
    Task<IEnumerable<EhrFile>> GetFilesAsync(Guid ehrId, int? version = null, bool useReplica = false);
    
    //  Kiểm tra replication 
    Task<bool> ExistsOnReplicaAsync(Guid ehrId);
}
