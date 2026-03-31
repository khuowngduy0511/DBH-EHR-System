using DBH.Appointment.Service.DTOs;

namespace DBH.Appointment.Service.Services;

public interface IAuthServiceClient
{
    Task<Guid?> GetUserIdByPatientIdAsync(Guid patientId);
    Task<Guid?> GetUserIdByDoctorIdAsync(Guid doctorId);
    Task<AuthUserProfileDetailDto?> GetUserProfileDetailAsync(Guid userId);
}
