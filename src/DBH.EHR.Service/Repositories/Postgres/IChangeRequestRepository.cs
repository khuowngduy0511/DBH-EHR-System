using DBH.EHR.Service.Models.Entities;

namespace DBH.EHR.Service.Repositories.Postgres;

public interface IChangeRequestRepository
{
    Task<ChangeRequest> CreateAsync(ChangeRequest request);
    Task<ChangeRequest?> GetByIdAsync(Guid id);
    Task<ChangeRequest> UpdateAsync(ChangeRequest request);
    Task<IEnumerable<ChangeRequest>> GetByPatientIdAsync(string patientId);
    Task<IEnumerable<ChangeRequest>> GetPendingRequestsAsync();
}
