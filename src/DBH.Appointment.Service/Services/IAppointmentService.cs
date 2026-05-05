using DBH.Appointment.Service.DTOs;
using DBH.Appointment.Service.Models.Enums;

namespace DBH.Appointment.Service.Services;

public interface IAppointmentService
{
    // Appointments - CRUD
    Task<ApiResponse<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request);
    Task<ApiResponse<AppointmentResponse>> GetAppointmentByIdAsync(Guid appointmentId);
    Task<PagedResponse<AppointmentResponse>> GetAppointmentsAsync(
        Guid? patientId, Guid? doctorId, Guid? orgId, AppointmentStatus? status, string? statusList = null,
        DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null,
        int page = 1, int pageSize = 10);
    Task<ApiResponse<AppointmentResponse>> UpdateAppointmentStatusAsync(Guid appointmentId, AppointmentStatus status);
    Task<ApiResponse<AppointmentResponse>> RescheduleAppointmentAsync(Guid appointmentId, DateTime newDate);
    
    // Appointments - Lifecycle (Flow 3: Đặt lịch khám)
    Task<ApiResponse<AppointmentResponse>> ConfirmAppointmentAsync(Guid appointmentId);
    Task<ApiResponse<AppointmentResponse>> RejectAppointmentAsync(Guid appointmentId, string reason);
    Task<ApiResponse<AppointmentResponse>> CancelAppointmentAsync(Guid appointmentId, string reason);
    Task<ApiResponse<AppointmentResponse>> CheckInAsync(Guid appointmentId);
    
    // Doctor Search
    Task<PagedResponse<DoctorSearchResult>> SearchDoctorsAsync(SearchDoctorQuery query);
    
    // Encounters
    Task<ApiResponse<EncounterResponse>> CreateEncounterAsync(CreateEncounterRequest request);
    Task<ApiResponse<EncounterResponse>> GetEncounterByIdAsync(Guid encounterId);
    Task<PagedResponse<EncounterResponse>> GetEncountersByAppointmentIdAsync(Guid appointmentId, int page = 1, int pageSize = 10);
    Task<PagedResponse<EncounterResponse>> GetEncountersByPatientIdAsync(Guid patientId, int page = 1, int pageSize = 10);
    Task<ApiResponse<EncounterResponse>> UpdateEncounterAsync(Guid encounterId, UpdateEncounterRequest request);
    
    // Encounters - Complete + Auto-create EHR (Flow 4: Khám bệnh và tạo hồ sơ bệnh án)
    Task<ApiResponse<EncounterResponse>> CompleteEncounterAsync(Guid encounterId, CompleteEncounterRequest request);
    
    // Doctor - Patients
    Task<PagedResponse<DoctorPatientResponse>> GetPatientsByDoctorAsync(Guid doctorId, int page = 1, int pageSize = 10, string? searchTerm = null);
}
