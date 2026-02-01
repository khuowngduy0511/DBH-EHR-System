using DBH.EHR.Service.Data;
using DBH.EHR.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DBH.EHR.Service.Repositories.Postgres;

public class ChangeRequestRepository : IChangeRequestRepository
{
    private readonly EhrPrimaryDbContext _primaryDb;
    private readonly ILogger<ChangeRequestRepository> _logger;

    public ChangeRequestRepository(
        EhrPrimaryDbContext primaryDb,
        ILogger<ChangeRequestRepository> logger)
    {
        _primaryDb = primaryDb;
        _logger = logger;
    }

    public async Task<ChangeRequest> CreateAsync(ChangeRequest request)
    {
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        
        _primaryDb.ChangeRequests.Add(request);
        await _primaryDb.SaveChangesAsync();
        
        _logger.LogInformation(
            "Created ChangeRequest {RequestId} for patient {PatientId} on PostgreSQL PRIMARY",
            request.Id, request.PatientId);
        
        return request;
    }

    public async Task<ChangeRequest?> GetByIdAsync(Guid id)
    {
        return await _primaryDb.ChangeRequests
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<ChangeRequest> UpdateAsync(ChangeRequest request)
    {
        request.UpdatedAt = DateTime.UtcNow;
        
        _primaryDb.ChangeRequests.Update(request);
        await _primaryDb.SaveChangesAsync();
        
        _logger.LogInformation(
            "Updated ChangeRequest {RequestId} status to {Status} on PostgreSQL PRIMARY",
            request.Id, request.Status);
        
        return request;
    }

    public async Task<IEnumerable<ChangeRequest>> GetByPatientIdAsync(string patientId)
    {
        return await _primaryDb.ChangeRequests
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChangeRequest>> GetPendingRequestsAsync()
    {
        return await _primaryDb.ChangeRequests
            .Where(r => r.Status == RequestStatus.PENDING)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }
}
