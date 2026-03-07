using DBH.Appointment.Service.DTOs;
using DBH.Appointment.Service.Models.Enums;

namespace DBH.Appointment.Service.Services;

public interface IAppointmentService
{
    // Appointments
    Task<ApiResponse<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request);
    Task<ApiResponse<AppointmentResponse>> GetAppointmentByIdAsync(Guid appointmentId);
    Task<PagedResponse<AppointmentResponse>> GetAppointmentsAsync(Guid? patientId, Guid? doctorId, Guid? orgId, AppointmentStatus? status, int page = 1, int pageSize = 10);
    Task<ApiResponse<AppointmentResponse>> UpdateAppointmentStatusAsync(Guid appointmentId, AppointmentStatus status);
    Task<ApiResponse<AppointmentResponse>> RescheduleAppointmentAsync(Guid appointmentId, DateTime newDate);
    
    // Encounters
    Task<ApiResponse<EncounterResponse>> CreateEncounterAsync(CreateEncounterRequest request);
    Task<ApiResponse<EncounterResponse>> GetEncounterByIdAsync(Guid encounterId);
    Task<PagedResponse<EncounterResponse>> GetEncountersByAppointmentIdAsync(Guid appointmentId, int page = 1, int pageSize = 10);
    Task<PagedResponse<EncounterResponse>> GetEncountersByPatientIdAsync(Guid patientId, int page = 1, int pageSize = 10);
    Task<ApiResponse<EncounterResponse>> UpdateEncounterAsync(Guid encounterId, UpdateEncounterRequest request);
}
