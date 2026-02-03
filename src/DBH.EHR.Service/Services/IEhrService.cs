using DBH.EHR.Service.Models.DTOs;
using DBH.EHR.Service.Models.Entities;

namespace DBH.EHR.Service.Services;


public interface IEhrService
{
    Task<CreateEhrRecordResponseDto> CreateEhrRecordAsync(CreateEhrRecordDto request);

    Task<EhrRecordResponseDto?> GetEhrRecordAsync(Guid ehrId, bool useReplica = false);
    
    Task<IEnumerable<EhrRecordResponseDto>> GetPatientEhrRecordsAsync(Guid patientId, bool useReplica = false);

    Task<IEnumerable<EhrRecordResponseDto>> GetHospitalEhrRecordsAsync(Guid hospitalId, bool useReplica = false);
    
    Task<IEnumerable<EhrVersionDto>> GetEhrVersionsAsync(Guid ehrId, bool useReplica = false);
    
    Task<IEnumerable<EhrFileDto>> GetEhrFilesAsync(Guid ehrId, int? version = null, bool useReplica = false);
    
}
