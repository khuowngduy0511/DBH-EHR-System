using DBH.EHR.Service.Data;
using DBH.EHR.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DBH.EHR.Service.Repositories.Postgres;

public class EhrRecordRepository : IEhrRecordRepository
{
    private readonly EhrPrimaryDbContext _primaryDb;
    private readonly EhrReplicaDbContext _replicaDb;
    private readonly ILogger<EhrRecordRepository> _logger;

    public EhrRecordRepository(
        EhrPrimaryDbContext primaryDb,
        EhrReplicaDbContext replicaDb,
        ILogger<EhrRecordRepository> logger)
    {
        _primaryDb = primaryDb;
        _replicaDb = replicaDb;
        _logger = logger;
    }

    //Ghi (chỉ Primary)

    public async Task<EhrRecord> CreateAsync(EhrRecord record)
    {
        record.CreatedAt = DateTime.UtcNow;
        
        _primaryDb.EhrRecords.Add(record);
        await _primaryDb.SaveChangesAsync();
        
        _logger.LogInformation(
            "Tạo EhrRecord {EhrId} cho bệnh nhân {PatientId} trên PRIMARY",
            record.EhrId, record.PatientId);
        
        return record;
    }

    public async Task<EhrVersion> CreateVersionAsync(EhrVersion version)
    {
        version.CreatedAt = DateTime.UtcNow;
        
        _primaryDb.EhrVersions.Add(version);
        await _primaryDb.SaveChangesAsync();
        
        _logger.LogInformation(
            "Tạo EhrVersion {VersionId} (v{Version}) cho EHR {EhrId} trên PRIMARY",
            version.VersionId, version.Version, version.EhrId);
        
        return version;
    }

    public async Task<EhrFile> CreateFileAsync(EhrFile file)
    {
        file.CreatedAt = DateTime.UtcNow;
        
        _primaryDb.EhrFiles.Add(file);
        await _primaryDb.SaveChangesAsync();
        
        _logger.LogInformation(
            "Tạo EhrFile {FileId} cho EHR {EhrId} v{Version} trên PRIMARY",
            file.FileId, file.EhrId, file.Version);
        
        return file;
    }

    public async Task<EhrRecord> UpdateAsync(EhrRecord record)
    {
        _primaryDb.EhrRecords.Update(record);
        await _primaryDb.SaveChangesAsync();
        
        _logger.LogInformation("Cập nhật EhrRecord {EhrId} trên PRIMARY", record.EhrId);
        return record;
    }

    public async Task<EhrVersion> UpdateVersionAsync(EhrVersion version)
    {
        _primaryDb.EhrVersions.Update(version);
        await _primaryDb.SaveChangesAsync();
        
        _logger.LogInformation("Cập nhật EhrVersion {VersionId} trên PRIMARY", version.VersionId);
        return version;
    }

    //Đọc (Primary hoặc Replica)

    public async Task<EhrRecord?> GetByIdAsync(Guid ehrId, bool useReplica = false)
    {
        var node = useReplica ? "REPLICA" : "PRIMARY";
        _logger.LogDebug("Đọc EhrRecord {EhrId} từ {Node}", ehrId, node);
        
        if (useReplica)
        {
            return await _replicaDb.EhrRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EhrId == ehrId);
        }
        
        return await _primaryDb.EhrRecords.FirstOrDefaultAsync(e => e.EhrId == ehrId);
    }

    public async Task<EhrRecord?> GetByIdWithVersionsAsync(Guid ehrId, bool useReplica = false)
    {
        if (useReplica)
        {
            return await _replicaDb.EhrRecords
                .AsNoTracking()
                .Include(e => e.Versions.OrderByDescending(v => v.Version))
                .Include(e => e.Files)
                .FirstOrDefaultAsync(e => e.EhrId == ehrId);
        }
        
        return await _primaryDb.EhrRecords
            .Include(e => e.Versions.OrderByDescending(v => v.Version))
            .Include(e => e.Files)
            .FirstOrDefaultAsync(e => e.EhrId == ehrId);
    }

    public async Task<IEnumerable<EhrRecord>> GetByPatientIdAsync(Guid patientId, bool useReplica = false)
    {
        if (useReplica)
        {
            return await _replicaDb.EhrRecords
                .AsNoTracking()
                .Where(e => e.PatientId == patientId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        
        return await _primaryDb.EhrRecords
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<EhrRecord>> GetByDoctorIdAsync(Guid doctorId, bool useReplica = false)
    {
        if (useReplica)
        {
            return await _replicaDb.EhrRecords
                .AsNoTracking()
                .Where(e => e.CreatedByDoctorId == doctorId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        
        return await _primaryDb.EhrRecords
            .Where(e => e.CreatedByDoctorId == doctorId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<EhrRecord>> GetByHospitalIdAsync(Guid hospitalId, bool useReplica = false)
    {
        if (useReplica)
        {
            return await _replicaDb.EhrRecords
                .AsNoTracking()
                .Where(e => e.HospitalId == hospitalId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        
        return await _primaryDb.EhrRecords
            .Where(e => e.HospitalId == hospitalId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<EhrVersion?> GetLatestVersionAsync(Guid ehrId, bool useReplica = false)
    {
        if (useReplica)
        {
            return await _replicaDb.EhrVersions
                .AsNoTracking()
                .Where(v => v.EhrId == ehrId)
                .OrderByDescending(v => v.Version)
                .FirstOrDefaultAsync();
        }
        
        return await _primaryDb.EhrVersions
            .Where(v => v.EhrId == ehrId)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<EhrVersion>> GetVersionsAsync(Guid ehrId, bool useReplica = false)
    {
        if (useReplica)
        {
            return await _replicaDb.EhrVersions
                .AsNoTracking()
                .Where(v => v.EhrId == ehrId)
                .OrderByDescending(v => v.Version)
                .ToListAsync();
        }
        
        return await _primaryDb.EhrVersions
            .Where(v => v.EhrId == ehrId)
            .OrderByDescending(v => v.Version)
            .ToListAsync();
    }

    public async Task<IEnumerable<EhrFile>> GetFilesAsync(Guid ehrId, int? version = null, bool useReplica = false)
    {
        if (useReplica)
        {
            var query = _replicaDb.EhrFiles.AsNoTracking().Where(f => f.EhrId == ehrId);
            if (version.HasValue)
                query = query.Where(f => f.Version == version.Value);
            return await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
        }
        
        var primaryQuery = _primaryDb.EhrFiles.Where(f => f.EhrId == ehrId);
        if (version.HasValue)
            primaryQuery = primaryQuery.Where(f => f.Version == version.Value);
        return await primaryQuery.OrderByDescending(f => f.CreatedAt).ToListAsync();
    }

    //Kiểm tra replication

    public async Task<bool> ExistsOnReplicaAsync(Guid ehrId)
    {
        return await _replicaDb.EhrRecords.AsNoTracking().AnyAsync(e => e.EhrId == ehrId);
    }
}
