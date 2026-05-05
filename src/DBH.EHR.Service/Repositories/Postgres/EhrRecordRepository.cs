using DBH.EHR.Service.DbContext;
using DBH.EHR.Service.Models.Entities;
using DBH.Shared.Contracts;
using DBH.Shared.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace DBH.EHR.Service.Repositories.Postgres;

public class EhrRecordRepository : IEhrRecordRepository
{
    private readonly EhrPrimaryDbContext _db;
    private readonly ILogger<EhrRecordRepository> _logger;

    public EhrRecordRepository(
        EhrPrimaryDbContext db,
        ILogger<EhrRecordRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Ghi

    public async Task<EhrRecord> CreateAsync(EhrRecord record)
    {
        record.CreatedAt = VietnamTime.DatabaseNow;
        
        _db.EhrRecords.Add(record);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation(
            "Tạo EhrRecord {EhrId} cho bệnh nhân {PatientId}",
            record.EhrId, record.PatientId);
        
        return record;
    }

    public async Task<EhrVersion> CreateVersionAsync(EhrVersion version)
    {
        version.CreatedAt = VietnamTime.DatabaseNow;
        
        _db.EhrVersions.Add(version);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation(
            "Tạo EhrVersion {VersionId} (v{VersionNumber}) cho EHR {EhrId}",
            version.VersionId, version.VersionNumber, version.EhrId);
        
        return version;
    }

    public async Task<EhrFile> CreateFileAsync(EhrFile file)
    {
        file.CreatedAt = VietnamTime.DatabaseNow;
        
        _db.EhrFiles.Add(file);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation(
            "Tạo EhrFile {FileId} cho EHR {EhrId}",
            file.FileId, file.EhrId);
        
        return file;
    }

    public async Task<EhrRecord> UpdateAsync(EhrRecord record)
    {
        _db.EhrRecords.Update(record);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Cập nhật EhrRecord {EhrId}", record.EhrId);
        return record;
    }

    public async Task<EhrVersion> UpdateVersionAsync(EhrVersion version)
    {
        _db.EhrVersions.Update(version);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Cập nhật EhrVersion {VersionId}", version.VersionId);
        return version;
    }

    // Đọc

    public async Task<EhrRecord?> GetByIdAsync(Guid ehrId)
    {
        return await _db.EhrRecords.FirstOrDefaultAsync(e => e.EhrId == ehrId);
    }

    public async Task<EhrRecord?> GetByIdWithVersionsAsync(Guid ehrId)
    {
        return await _db.EhrRecords
            .Include(e => e.Versions.OrderByDescending(v => v.VersionNumber))
            .Include(e => e.Files)
            .FirstOrDefaultAsync(e => e.EhrId == ehrId);
    }

    public async Task<IEnumerable<EhrRecord>> GetByPatientIdAsync(Guid patientId)
    {
        return await _db.EhrRecords
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<EhrRecord>> GetByOrgIdAsync(Guid orgId)
    {
        return await _db.EhrRecords
            .Where(e => e.OrgId == orgId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<EhrVersion?> GetLatestVersionAsync(Guid ehrId)
    {
        return await _db.EhrVersions
            .Where(v => v.EhrId == ehrId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<EhrVersion>> GetVersionsAsync(Guid ehrId)
    {
        return await _db.EhrVersions
            .Where(v => v.EhrId == ehrId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }

    public async Task<IEnumerable<EhrFile>> GetFilesAsync(Guid ehrId)
    {
        return await _db.EhrFiles
            .Where(f => f.EhrId == ehrId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<EhrVersion?> GetVersionByIdAsync(Guid ehrId, Guid versionId)
    {
        return await _db.EhrVersions
            .FirstOrDefaultAsync(v => v.EhrId == ehrId && v.VersionId == versionId);
    }

    public async Task<EhrFile?> GetFileByIdAsync(Guid ehrId, Guid fileId)
    {
        return await _db.EhrFiles
            .FirstOrDefaultAsync(f => f.EhrId == ehrId && f.FileId == fileId);
    }

    public async Task DeleteFileAsync(EhrFile file)
    {
        _db.EhrFiles.Remove(file);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Deleted EhrFile {FileId} from EHR {EhrId}", file.FileId, file.EhrId);
    }

    public async Task<(IEnumerable<EhrRecord> Items, int TotalCount)> GetAccessibleRecordsPaginatedAsync(
        Guid? orgId,
        List<Guid> consentedEhrIds,
        List<Guid> consentedPatientIds,
        string? search,
        List<Guid>? matchingUserIds = null,
        List<Guid>? matchingOrgIds = null,
        int page = 1,
        int pageSize = 10)
    {
        var query = _db.EhrRecords
            .Include(e => e.Versions.OrderByDescending(v => v.VersionNumber))
            .Include(e => e.Files)
            .AsQueryable();

        // Access Control Filter: Allow if in same Org, has specific Consent, or matches global Search results
        query = query.Where(r => 
            (orgId.HasValue && r.OrgId == orgId) || 
            consentedEhrIds.Contains(r.EhrId) ||
            consentedPatientIds.Contains(r.PatientId) ||
            (matchingUserIds != null && matchingUserIds.Contains(r.PatientId)));

        // Search Filter (EhrId, PatientId, OrgId, EncounterId, Date, or matchingUserIds/matchingOrgIds)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            
            // Handle Guid parsing for ID fields
            Guid.TryParse(search.Trim(), out var searchGuid);

            query = query.Where(r => 
                (searchGuid != Guid.Empty && (r.EhrId == searchGuid || r.PatientId == searchGuid || (r.EncounterId.HasValue && r.EncounterId.Value == searchGuid))) || 
                (matchingUserIds != null && matchingUserIds.Contains(r.PatientId)) ||
                (matchingOrgIds != null && r.OrgId.HasValue && matchingOrgIds.Contains(r.OrgId.Value))
            );
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
