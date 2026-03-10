using DBH.Appointment.Service.DbContext;
using DBH.Appointment.Service.DTOs;
using DBH.Appointment.Service.Models.Entities;
using DBH.Appointment.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DBH.Appointment.Service.Services;

public class AppointmentService : IAppointmentService
{
    private readonly AppointmentDbContext _context;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(AppointmentDbContext context, ILogger<AppointmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // =========================================================================
    // APPOINTMENTS
    // =========================================================================

    public async Task<ApiResponse<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        // Add basic validation for date
        if (request.ScheduledAt < DateTime.UtcNow)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Scheduled date cannot be in the past"
            };
        }

        var appointment = new Models.Entities.Appointment
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            OrgId = request.OrgId,
            ScheduledAt = request.ScheduledAt.ToUniversalTime(),
            Status = AppointmentStatus.PENDING
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new appointment {Id} for Patient {PatientId} with Doctor {DoctorId}", 
            appointment.AppointmentId, appointment.PatientId, appointment.DoctorId);

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Appointment created successfully",
            Data = MapToResponse(appointment)
        };
    }

    public async Task<ApiResponse<AppointmentResponse>> GetAppointmentByIdAsync(Guid appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Encounters)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appointment == null)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Appointment not found"
            };
        }

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Data = MapToResponse(appointment)
        };
    }

    public async Task<PagedResponse<AppointmentResponse>> GetAppointmentsAsync(
        Guid? patientId, Guid? doctorId, Guid? orgId, AppointmentStatus? status, int page = 1, int pageSize = 10)
    {
        var query = _context.Appointments
            .Include(a => a.Encounters)
            .AsQueryable();

        if (patientId.HasValue)
            query = query.Where(a => a.PatientId == patientId.Value);

        if (doctorId.HasValue)
            query = query.Where(a => a.DoctorId == doctorId.Value);

        if (orgId.HasValue)
            query = query.Where(a => a.OrgId == orgId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<AppointmentResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ApiResponse<AppointmentResponse>> UpdateAppointmentStatusAsync(Guid appointmentId, AppointmentStatus status)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Appointment not found"
            };
        }

        appointment.Status = status;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated appointment {Id} status to {Status}", appointmentId, status);

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = $"Appointment status updated to {status}",
            Data = MapToResponse(appointment)
        };
    }

    public async Task<ApiResponse<AppointmentResponse>> RescheduleAppointmentAsync(Guid appointmentId, DateTime newDate)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Appointment not found"
            };
        }

        if (newDate < DateTime.UtcNow)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Scheduled date cannot be in the past"
            };
        }

        appointment.ScheduledAt = newDate.ToUniversalTime();
        appointment.Status = AppointmentStatus.RESCHEDULED; // Optional depending on flow, could also be PENDING 
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rescheduled appointment {Id} to {Date}", appointmentId, newDate);

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Appointment rescheduled successfully",
            Data = MapToResponse(appointment)
        };
    }

    // =========================================================================
    // ENCOUNTERS
    // =========================================================================

    public async Task<ApiResponse<EncounterResponse>> CreateEncounterAsync(CreateEncounterRequest request)
    {
        // Ensure appointment exists
        var appointment = await _context.Appointments.FindAsync(request.AppointmentId);
        if (appointment == null)
        {
            return new ApiResponse<EncounterResponse>
            {
                Success = false,
                Message = "Linked appointment not found"
            };
        }

        // Update appointment status to IN_PROGRESS or COMPLETED
        if (appointment.Status == AppointmentStatus.CONFIRMED || appointment.Status == AppointmentStatus.CHECKED_IN)
        {
            appointment.Status = AppointmentStatus.IN_PROGRESS;
        }

        var encounter = new Encounter
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            AppointmentId = request.AppointmentId,
            OrgId = request.OrgId,
            Notes = request.Notes
        };

        _context.Encounters.Add(encounter);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created encounter {Id} for Appointment {ApptId}", encounter.EncounterId, encounter.AppointmentId);

        return new ApiResponse<EncounterResponse>
        {
            Success = true,
            Message = "Encounter created successfully",
            Data = MapToResponse(encounter)
        };
    }

    public async Task<ApiResponse<EncounterResponse>> GetEncounterByIdAsync(Guid encounterId)
    {
        var encounter = await _context.Encounters.FindAsync(encounterId);
        if (encounter == null)
        {
            return new ApiResponse<EncounterResponse>
            {
                Success = false,
                Message = "Encounter not found"
            };
        }

        return new ApiResponse<EncounterResponse>
        {
            Success = true,
            Data = MapToResponse(encounter)
        };
    }

    public async Task<PagedResponse<EncounterResponse>> GetEncountersByAppointmentIdAsync(Guid appointmentId, int page = 1, int pageSize = 10)
    {
        var query = _context.Encounters.Where(e => e.AppointmentId == appointmentId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<EncounterResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResponse<EncounterResponse>> GetEncountersByPatientIdAsync(Guid patientId, int page = 1, int pageSize = 10)
    {
        var query = _context.Encounters.Where(e => e.PatientId == patientId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<EncounterResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ApiResponse<EncounterResponse>> UpdateEncounterAsync(Guid encounterId, UpdateEncounterRequest request)
    {
        var encounter = await _context.Encounters.FindAsync(encounterId);
        if (encounter == null)
        {
            return new ApiResponse<EncounterResponse>
            {
                Success = false,
                Message = "Encounter not found"
            };
        }

        if (request.Notes != null)
        {
            encounter.Notes = request.Notes;
        }

        await _context.SaveChangesAsync();

        return new ApiResponse<EncounterResponse>
        {
            Success = true,
            Message = "Encounter updated successfully",
            Data = MapToResponse(encounter)
        };
    }

    // =========================================================================
    // MAPPERS
    // =========================================================================

    private static AppointmentResponse MapToResponse(Models.Entities.Appointment appt)
    {
        return new AppointmentResponse
        {
            AppointmentId = appt.AppointmentId,
            PatientId = appt.PatientId,
            DoctorId = appt.DoctorId,
            OrgId = appt.OrgId,
            ScheduledAt = appt.ScheduledAt,
            Status = appt.Status,
            CreatedAt = appt.CreatedAt,
            EncounterCount = appt.Encounters?.Count ?? 0
        };
    }

    private static EncounterResponse MapToResponse(Encounter encounter)
    {
        return new EncounterResponse
        {
            EncounterId = encounter.EncounterId,
            PatientId = encounter.PatientId,
            DoctorId = encounter.DoctorId,
            AppointmentId = encounter.AppointmentId,
            OrgId = encounter.OrgId,
            Notes = encounter.Notes,
            CreatedAt = encounter.CreatedAt
        };
    }
}
