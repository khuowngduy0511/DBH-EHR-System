using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Entities;

namespace DBH.EHR.Service.Services;

public interface IEhrService
{
    /// <summary>
    /// Create a new EHR change request.
    /// Writes document to MongoDB primary, creates PENDING request in PostgreSQL primary.
    /// </summary>
    Task<CreateEhrResponseDto> CreateChangeRequestAsync(CreateEhrRequestDto request);
    
    /// <summary>
    /// Get a change request by ID
    /// </summary>
    Task<ChangeRequest?> GetChangeRequestAsync(Guid requestId);
    
    /// <summary>
    /// Get all change requests for a patient
    /// </summary>
    Task<IEnumerable<ChangeRequest>> GetChangeRequestsByPatientAsync(string patientId);
}
