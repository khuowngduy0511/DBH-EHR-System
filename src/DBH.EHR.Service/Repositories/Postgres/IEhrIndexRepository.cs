using DBH.EHR.Service.Models.Entities;

namespace DBH.EHR.Service.Repositories.Postgres;

public interface IEhrIndexRepository
{
    Task<EhrIndex> CreateAsync(EhrIndex ehrIndex);
    Task<EhrIndex?> GetByRecordIdAsync(Guid recordId, bool useReplica = false);
    Task<EhrIndex?> GetByOffchainDocIdAsync(string offchainDocId, bool useReplica = false);
    Task<IEnumerable<EhrIndex>> GetByPatientIdAsync(string patientId, bool useReplica = false);
    Task<bool> ExistsOnReplicaAsync(Guid recordId);
}
