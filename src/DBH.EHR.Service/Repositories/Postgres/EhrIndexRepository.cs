using DBH.EHR.Service.Data;
using DBH.EHR.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DBH.EHR.Service.Repositories.Postgres;

public class EhrIndexRepository : IEhrIndexRepository
{
    private readonly EhrPrimaryDbContext _primaryDb;
    private readonly EhrReplicaDbContext _replicaDb;
    private readonly ILogger<EhrIndexRepository> _logger;

    public EhrIndexRepository(
        EhrPrimaryDbContext primaryDb,
        EhrReplicaDbContext replicaDb,
        ILogger<EhrIndexRepository> logger)
    {
        _primaryDb = primaryDb;
        _replicaDb = replicaDb;
        _logger = logger;
    }

    public async Task<EhrIndex> CreateAsync(EhrIndex ehrIndex)
    {
        ehrIndex.CreatedAt = DateTime.UtcNow;
        ehrIndex.UpdatedAt = DateTime.UtcNow;
        
        _primaryDb.EhrIndex.Add(ehrIndex);
        await _primaryDb.SaveChangesAsync();
        
        _logger.LogInformation(
            "Created EhrIndex {RecordId} for patient {PatientId} on PostgreSQL PRIMARY",
            ehrIndex.RecordId, ehrIndex.PatientId);
        
        return ehrIndex;
    }

    public async Task<EhrIndex?> GetByRecordIdAsync(Guid recordId, bool useReplica = false)
    {
        var db = useReplica ? (DbContext)_replicaDb : _primaryDb;
        var nodeName = useReplica ? "REPLICA" : "PRIMARY";
        
        _logger.LogDebug("Reading EhrIndex {RecordId} from PostgreSQL {Node}", recordId, nodeName);
        
        if (useReplica)
        {
            return await _replicaDb.EhrIndex
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.RecordId == recordId);
        }
        
        return await _primaryDb.EhrIndex
            .FirstOrDefaultAsync(e => e.RecordId == recordId);
    }

    public async Task<EhrIndex?> GetByOffchainDocIdAsync(string offchainDocId, bool useReplica = false)
    {
        if (useReplica)
        {
            return await _replicaDb.EhrIndex
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.OffchainDocId == offchainDocId);
        }
        
        return await _primaryDb.EhrIndex
            .FirstOrDefaultAsync(e => e.OffchainDocId == offchainDocId);
    }

    public async Task<IEnumerable<EhrIndex>> GetByPatientIdAsync(string patientId, bool useReplica = false)
    {
        if (useReplica)
        {
            return await _replicaDb.EhrIndex
                .AsNoTracking()
                .Where(e => e.PatientId == patientId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        
        return await _primaryDb.EhrIndex
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ExistsOnReplicaAsync(Guid recordId)
    {
        return await _replicaDb.EhrIndex
            .AsNoTracking()
            .AnyAsync(e => e.RecordId == recordId);
    }
}
