using System.Text;
using System.Text.Json;
using DBH.Appointment.Service.DbContext;
using DBH.Appointment.Service.DTOs;
using DBH.Appointment.Service.Models.Entities;
using DBH.Appointment.Service.Models.Enums;
using DBH.Shared.Contracts.Events;
using DBH.Shared.Contracts.Commands;
using DBH.Shared.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace DBH.Appointment.Service.Services;

public class AppointmentService : IAppointmentService
{
    private readonly AppointmentDbContext _context;
    private readonly ILogger<AppointmentService> _logger;
    private readonly IMessagePublisher? _messagePublisher;
    private readonly IHttpClientFactory _httpClientFactory;

    public AppointmentService(
        AppointmentDbContext context,
        ILogger<AppointmentService> logger,
        IHttpClientFactory httpClientFactory,
        IMessagePublisher? messagePublisher = null)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _messagePublisher = messagePublisher;
    }

    // =========================================================================
    // APPOINTMENTS - CRUD
    // =========================================================================

    public async Task<ApiResponse<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
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

        // Publish event
        await PublishEventAsync(new AppointmentCreatedEvent
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            OrganizationId = appointment.OrgId,
            ScheduledAt = appointment.ScheduledAt
        });

        // Schedule appointment reminder (1 hour before)
        await ScheduleReminderAsync(appointment);

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

        var oldStatus = appointment.Status.ToString();
        appointment.Status = status;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated appointment {Id} status to {Status}", appointmentId, status);

        await PublishEventAsync(new AppointmentStatusChangedEvent
        {
            AppointmentId = appointmentId,
            OldStatus = oldStatus,
            NewStatus = status.ToString()
        });

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
        appointment.Status = AppointmentStatus.RESCHEDULED;
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
    // APPOINTMENTS - LIFECYCLE (Flow 3: Đặt lịch khám)
    // =========================================================================

    /// <summary>
    /// Bác sĩ xác nhận lịch hẹn → PENDING → CONFIRMED
    /// </summary>
    public async Task<ApiResponse<AppointmentResponse>> ConfirmAppointmentAsync(Guid appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
            return new ApiResponse<AppointmentResponse> { Success = false, Message = "Appointment not found" };

        if (appointment.Status != AppointmentStatus.PENDING)
            return new ApiResponse<AppointmentResponse> 
            { 
                Success = false, 
                Message = $"Cannot confirm appointment with status {appointment.Status}. Only PENDING appointments can be confirmed." 
            };

        var oldStatus = appointment.Status.ToString();
        appointment.Status = AppointmentStatus.CONFIRMED;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Confirmed appointment {Id}", appointmentId);

        // Publish events
        await PublishEventAsync(new AppointmentConfirmedEvent
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            OrganizationId = appointment.OrgId,
            ScheduledAt = appointment.ScheduledAt
        });

        await PublishEventAsync(new AppointmentStatusChangedEvent
        {
            AppointmentId = appointmentId,
            OldStatus = oldStatus,
            NewStatus = AppointmentStatus.CONFIRMED.ToString()
        });

        // Notify patient
        await PublishEventAsync(new NotifyAppointmentConfirmedCommand
        {
            PatientUserId = appointment.PatientId,
            DoctorName = $"Doctor {appointment.DoctorId}",
            OrganizationName = $"Org {appointment.OrgId}",
            ScheduledAt = appointment.ScheduledAt,
            AppointmentId = appointment.AppointmentId
        });

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Appointment confirmed successfully",
            Data = MapToResponse(appointment)
        };
    }

    /// <summary>
    /// Bác sĩ từ chối lịch hẹn → PENDING → CANCELLED
    /// </summary>
    public async Task<ApiResponse<AppointmentResponse>> RejectAppointmentAsync(Guid appointmentId, string reason)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
            return new ApiResponse<AppointmentResponse> { Success = false, Message = "Appointment not found" };

        if (appointment.Status != AppointmentStatus.PENDING)
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = $"Cannot reject appointment with status {appointment.Status}. Only PENDING appointments can be rejected."
            };

        var oldStatus = appointment.Status.ToString();
        appointment.Status = AppointmentStatus.CANCELLED;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rejected appointment {Id}. Reason: {Reason}", appointmentId, reason);

        await PublishEventAsync(new AppointmentCancelledEvent
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            OrganizationId = appointment.OrgId,
            Reason = reason
        });

        await PublishEventAsync(new NotifyAppointmentCancelledCommand
        {
            RecipientUserId = appointment.PatientId,
            CancelledByName = $"Doctor {appointment.DoctorId}",
            Reason = reason,
            AppointmentId = appointment.AppointmentId
        });

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Appointment rejected",
            Data = MapToResponse(appointment)
        };
    }

    /// <summary>
    /// Hủy lịch hẹn (bệnh nhân hoặc bác sĩ) → CANCELLED
    /// </summary>
    public async Task<ApiResponse<AppointmentResponse>> CancelAppointmentAsync(Guid appointmentId, string reason)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
            return new ApiResponse<AppointmentResponse> { Success = false, Message = "Appointment not found" };

        if (appointment.Status == AppointmentStatus.COMPLETED || appointment.Status == AppointmentStatus.CANCELLED)
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = $"Cannot cancel appointment with status {appointment.Status}"
            };

        var oldStatus = appointment.Status.ToString();
        appointment.Status = AppointmentStatus.CANCELLED;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cancelled appointment {Id}. Reason: {Reason}", appointmentId, reason);

        await PublishEventAsync(new AppointmentCancelledEvent
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            OrganizationId = appointment.OrgId,
            Reason = reason
        });

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Appointment cancelled",
            Data = MapToResponse(appointment)
        };
    }

    /// <summary>
    /// Bệnh nhân check-in tại cơ sở y tế → CONFIRMED → CHECKED_IN
    /// </summary>
    public async Task<ApiResponse<AppointmentResponse>> CheckInAsync(Guid appointmentId)
    {
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
            return new ApiResponse<AppointmentResponse> { Success = false, Message = "Appointment not found" };

        if (appointment.Status != AppointmentStatus.CONFIRMED)
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = $"Cannot check in with status {appointment.Status}. Only CONFIRMED appointments can be checked in."
            };

        appointment.Status = AppointmentStatus.CHECKED_IN;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Patient checked in for appointment {Id}", appointmentId);

        await PublishEventAsync(new AppointmentCheckedInEvent
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            OrganizationId = appointment.OrgId
        });

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Checked in successfully",
            Data = MapToResponse(appointment)
        };
    }

    // =========================================================================
    // DOCTOR SEARCH
    // =========================================================================

    /// <summary>
    /// Tìm kiếm bác sĩ theo chuyên khoa — calls Organization Service
    /// </summary>
    public async Task<PagedResponse<DoctorSearchResult>> SearchDoctorsAsync(SearchDoctorQuery query)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OrganizationService");

            var url = $"api/v1/memberships/search?page={query.Page}&pageSize={query.PageSize}";
            if (!string.IsNullOrEmpty(query.Specialty))
                url += $"&specialty={Uri.EscapeDataString(query.Specialty)}";
            if (query.OrgId.HasValue)
                url += $"&orgId={query.OrgId.Value}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Organization Service returned {StatusCode} for doctor search", response.StatusCode);
                return new PagedResponse<DoctorSearchResult>
                {
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalCount = 0
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PagedResponse<DoctorSearchResult>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result ?? new PagedResponse<DoctorSearchResult>
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching doctors");
            return new PagedResponse<DoctorSearchResult>
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = 0
            };
        }
    }

    // =========================================================================
    // ENCOUNTERS
    // =========================================================================

    public async Task<ApiResponse<EncounterResponse>> CreateEncounterAsync(CreateEncounterRequest request)
    {
        var appointment = await _context.Appointments.FindAsync(request.AppointmentId);
        if (appointment == null)
        {
            return new ApiResponse<EncounterResponse>
            {
                Success = false,
                Message = "Linked appointment not found"
            };
        }

        // Update appointment status to IN_PROGRESS
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

        await PublishEventAsync(new EncounterCreatedEvent
        {
            EncounterId = encounter.EncounterId,
            AppointmentId = encounter.AppointmentId,
            PatientId = encounter.PatientId,
            DoctorId = encounter.DoctorId,
            OrganizationId = encounter.OrgId
        });

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
    // ENCOUNTER - COMPLETE + AUTO-CREATE EHR (Flow 4)
    // =========================================================================

    /// <summary>
    /// Complete encounter: update notes, mark appointment COMPLETED, optionally auto-create EHR
    /// Flow 4: Khám bệnh và tạo hồ sơ bệnh án
    /// </summary>
    public async Task<ApiResponse<EncounterResponse>> CompleteEncounterAsync(Guid encounterId, CompleteEncounterRequest request)
    {
        var encounter = await _context.Encounters
            .Include(e => e.Appointment)
            .FirstOrDefaultAsync(e => e.EncounterId == encounterId);

        if (encounter == null)
            return new ApiResponse<EncounterResponse> { Success = false, Message = "Encounter not found" };

        // Update notes if provided
        if (!string.IsNullOrEmpty(request.Notes))
        {
            encounter.Notes = request.Notes;
        }

        // Mark appointment as COMPLETED
        if (encounter.Appointment != null && encounter.Appointment.Status != AppointmentStatus.COMPLETED)
        {
            encounter.Appointment.Status = AppointmentStatus.COMPLETED;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Completed encounter {Id} for appointment {ApptId}", encounterId, encounter.AppointmentId);

        // Auto-create EHR if EhrData is provided
        Guid? ehrId = null;
        if (request.EhrData.HasValue)
        {
            ehrId = await CreateEhrFromEncounterAsync(encounter, request.EhrData.Value);
        }

        // Publish encounter completed event
        await PublishEventAsync(new EncounterCompletedEvent
        {
            EncounterId = encounter.EncounterId,
            AppointmentId = encounter.AppointmentId,
            PatientId = encounter.PatientId,
            DoctorId = encounter.DoctorId,
            OrganizationId = encounter.OrgId,
            EhrId = ehrId
        });

        return new ApiResponse<EncounterResponse>
        {
            Success = true,
            Message = ehrId.HasValue 
                ? $"Encounter completed and EHR created ({ehrId})" 
                : "Encounter completed successfully",
            Data = MapToResponse(encounter)
        };
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    /// <summary>
    /// Auto-create EHR by calling EHR Service
    /// </summary>
    private async Task<Guid?> CreateEhrFromEncounterAsync(Encounter encounter, JsonElement ehrData)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("EhrService");

            var createEhrRequest = new
            {
                PatientId = encounter.PatientId,
                EncounterId = encounter.EncounterId,
                OrgId = encounter.OrgId,
                Data = ehrData
            };

            var content = new StringContent(
                JsonSerializer.Serialize(createEhrRequest),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("Ehr/records", content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<JsonElement>(json);
                if (result.TryGetProperty("ehrId", out var ehrIdProp))
                {
                    var ehrId = ehrIdProp.GetGuid();
                    _logger.LogInformation("Auto-created EHR {EhrId} from encounter {EncounterId}", ehrId, encounter.EncounterId);
                    return ehrId;
                }
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("EHR Service returned {StatusCode} when creating EHR from encounter: {Error}",
                    response.StatusCode, errorBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-create EHR from encounter {EncounterId}", encounter.EncounterId);
        }

        return null;
    }

    private async Task PublishEventAsync<T>(T @event) where T : class
    {
        if (_messagePublisher != null)
        {
            try
            {
                await _messagePublisher.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish event {EventType}", typeof(T).Name);
            }
        }
    }

    private async Task ScheduleReminderAsync(Models.Entities.Appointment appointment)
    {
        if (_messagePublisher == null) return;

        try
        {
            var reminderTime = appointment.ScheduledAt.AddHours(-1);
            if (reminderTime > DateTime.UtcNow)
            {
                await _messagePublisher.ScheduleAsync(
                    new NotifyAppointmentReminderCommand
                    {
                        PatientUserId = appointment.PatientId,
                        DoctorName = $"Doctor {appointment.DoctorId}",
                        OrganizationName = $"Org {appointment.OrgId}",
                        ScheduledAt = appointment.ScheduledAt,
                        AppointmentId = appointment.AppointmentId
                    },
                    new DateTimeOffset(reminderTime, TimeSpan.Zero));

                _logger.LogInformation("Scheduled reminder for appointment {Id} at {ReminderTime}",
                    appointment.AppointmentId, reminderTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to schedule reminder for appointment {Id}", appointment.AppointmentId);
        }
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
            Status = appt.Status.ToString(),
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
