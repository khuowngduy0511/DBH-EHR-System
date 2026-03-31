using DBH.EHR.Service.Models.DTOs;

namespace DBH.EHR.Service.Services;

public interface IAuthServiceClient
{
    Task<Guid?> GetUserIdByPatientIdAsync(Guid patientId, string bearerToken);
    Task<Guid?> GetUserIdByDoctorIdAsync(Guid doctorId, string bearerToken);
    Task<AuthUserProfileDetailDto?> GetUserProfileDetailAsync(Guid userId, string bearerToken);
}
