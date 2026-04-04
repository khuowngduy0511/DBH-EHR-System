using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DBH.Appointment.Service.DbContext;
using DBH.Appointment.Service.DTOs;
using DBH.Appointment.Service.Models.Entities;
using DBH.Appointment.Service.Models.Enums;
using DBH.Shared.Contracts.Events;
using DBH.Shared.Contracts.Commands;
using DBH.Shared.Infrastructure.Messaging;
using DBH.Shared.Infrastructure.Notification;
using Microsoft.EntityFrameworkCore;

namespace DBH.Appointment.Service.Services;

public class AppointmentService : IAppointmentService
{
    private readonly AppointmentDbContext _context;
    private readonly ILogger<AppointmentService> _logger;
    private readonly IMessagePublisher? _messagePublisher;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthServiceClient _authServiceClient;
    private readonly INotificationServiceClient? _notificationClient;

    public AppointmentService(
        AppointmentDbContext context,
        ILogger<AppointmentService> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IAuthServiceClient authServiceClient,
        IMessagePublisher? messagePublisher = null,
        INotificationServiceClient? notificationClient = null)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _authServiceClient = authServiceClient;
        _messagePublisher = messagePublisher;
        _notificationClient = notificationClient;
    }

    // =========================================================================
    // APPOINTMENTS - CRUD
    // =========================================================================

    public async Task<ApiResponse<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        var scheduledAtUtc = request.ScheduledAt.Kind == DateTimeKind.Utc
            ? request.ScheduledAt
            : request.ScheduledAt.ToUniversalTime();

        if (scheduledAtUtc < DateTime.UtcNow)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Scheduled date cannot be in the past"
            };
        }

        var patientUserId = await _authServiceClient.GetUserIdByPatientIdAsync(request.PatientId);
        if (!patientUserId.HasValue)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Patient not found"
            };
        }

        var doctorValidation = await ValidateDoctorAndOrganizationAsync(request.DoctorId, request.OrgId);
        if (!doctorValidation.Success)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = doctorValidation.Message
            };
        }

        var doctorConflictExists = await _context.Appointments.AnyAsync(a =>
            a.DoctorId == request.DoctorId &&
            a.ScheduledAt == scheduledAtUtc &&
            a.Status != AppointmentStatus.CANCELLED);

        if (doctorConflictExists)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Bác sĩ đã có lịch hẹn vào thời gian này. Vui lòng chọn khung giờ khác."
            };
        }

        var patientConflictExists = await _context.Appointments.AnyAsync(a =>
            a.PatientId == request.PatientId &&
            a.ScheduledAt == scheduledAtUtc &&
            a.Status != AppointmentStatus.CANCELLED);

        if (patientConflictExists)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Bệnh nhân đã có lịch hẹn vào thời gian này. Vui lòng chọn khung giờ khác."
            };
        }

        // var patientDoctorOrgConflictExists = await _context.Appointments.AnyAsync(a =>
        //     a.PatientId == request.PatientId &&
        //     a.OrgId == request.OrgId &&
        //     a.ScheduledAt == scheduledAtUtc &&
        //     a.Status != AppointmentStatus.CANCELLED);

        // if (patientDoctorOrgConflictExists)
        // {
        //     return new ApiResponse<AppointmentResponse>
        //     {
        //         Success = false,
        //         Message = "Bệnh nhân đã có lịch hẹn tại cơ sở y tế này vào thời gian này."
        //     };
        // }

        var appointment = new Models.Entities.Appointment
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            OrgId = request.OrgId,
            ScheduledAt = scheduledAtUtc,
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

        // Notify doctor about new appointment
        await NotifyAsync(appointment.DoctorId,
            "Lịch hẹn mới",
            $"Bạn có lịch hẹn mới vào lúc {appointment.ScheduledAt:dd/MM/yyyy HH:mm}",
            "AppointmentCreated", "Normal",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Appointment created successfully",
            Data = await MapToResponseWithProfilesAsync(appointment)
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
            Data = await MapToResponseWithProfilesAsync(appointment)
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

        var responses = items.Select(MapToResponse).ToList();
        await AttachAppointmentProfilesAsync(responses);

        return new PagedResponse<AppointmentResponse>
        {
            Data = responses,
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
            Data = await MapToResponseWithProfilesAsync(appointment)
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

        var newDateUtc = newDate.Kind == DateTimeKind.Utc
            ? newDate
            : newDate.ToUniversalTime();

        if (newDateUtc < DateTime.UtcNow)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Scheduled date cannot be in the past"
            };
        }

        var doctorConflictExists = await _context.Appointments.AnyAsync(a =>
            a.AppointmentId != appointmentId &&
            a.DoctorId == appointment.DoctorId &&
            a.OrgId == appointment.OrgId &&
            a.ScheduledAt == newDateUtc &&
            a.Status != AppointmentStatus.CANCELLED);

        if (doctorConflictExists)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Bác sĩ đã có lịch hẹn vào khung giờ này. Vui lòng chọn khung giờ khác."
            };
        }

        var patientConflictExists = await _context.Appointments.AnyAsync(a =>
            a.AppointmentId != appointmentId &&
            a.PatientId == appointment.PatientId &&
            a.OrgId == appointment.OrgId &&
            a.ScheduledAt == newDateUtc &&
            a.Status != AppointmentStatus.CANCELLED);

        if (patientConflictExists)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Bệnh nhân đã có lịch hẹn vào khung giờ này. Vui lòng chọn khung giờ khác."
            };
        }

        var patientDoctorOrgConflictExists = await _context.Appointments.AnyAsync(a =>
            a.AppointmentId != appointmentId &&
            a.PatientId == appointment.PatientId &&
            a.DoctorId == appointment.DoctorId &&
            a.OrgId == appointment.OrgId &&
            a.ScheduledAt == newDateUtc &&
            a.Status != AppointmentStatus.CANCELLED);

        if (patientDoctorOrgConflictExists)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Bệnh nhân đã có lịch hẹn với bác sĩ này tại cơ sở y tế này vào khung giờ này."
            };
        }

        appointment.ScheduledAt = newDateUtc;
        appointment.Status = AppointmentStatus.RESCHEDULED;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rescheduled appointment {Id} to {Date}", appointmentId, newDateUtc);

        // Notify both parties about reschedule
        await NotifyAsync(appointment.PatientId,
            "Lịch hẹn đã được đổi",
            $"Lịch hẹn đã được đổi sang {newDateUtc:dd/MM/yyyy HH:mm}.",
            "AppointmentRescheduled", "High",
            appointment.AppointmentId.ToString(), "Appointment");
        await NotifyAsync(appointment.DoctorId,
            "Lịch hẹn đã được đổi",
            $"Lịch hẹn đã được đổi sang {newDateUtc:dd/MM/yyyy HH:mm}.",
            "AppointmentRescheduled", "Normal",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Appointment rescheduled successfully",
            Data = await MapToResponseWithProfilesAsync(appointment)
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

        await NotifyAsync(appointment.PatientId,
            "Lịch hẹn đã được xác nhận",
            $"Lịch hẹn vào lúc {appointment.ScheduledAt:dd/MM/yyyy HH:mm} đã được bác sĩ xác nhận.",
            "AppointmentReminder", "High",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Appointment confirmed successfully",
            Data = await MapToResponseWithProfilesAsync(appointment)
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

        await NotifyAsync(appointment.PatientId,
            "Lịch hẹn bị từ chối",
            $"Lịch hẹn của bạn đã bị từ chối. Lý do: {reason}",
            "AppointmentReminder", "High",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Appointment rejected",
            Data = await MapToResponseWithProfilesAsync(appointment)
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

        // Notify both patient and doctor
        await NotifyAsync(appointment.PatientId,
            "Lịch hẹn đã bị hủy",
            $"Lịch hẹn vào lúc {appointment.ScheduledAt:dd/MM/yyyy HH:mm} đã bị hủy. Lý do: {reason}",
            "AppointmentReminder", "High",
            appointment.AppointmentId.ToString(), "Appointment");
        await NotifyAsync(appointment.DoctorId,
            "Lịch hẹn đã bị hủy",
            $"Lịch hẹn vào lúc {appointment.ScheduledAt:dd/MM/yyyy HH:mm} đã bị hủy. Lý do: {reason}",
            "AppointmentReminder", "Normal",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Appointment cancelled",
            Data = await MapToResponseWithProfilesAsync(appointment)
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

        // Notify doctor that patient checked in
        await NotifyAsync(appointment.DoctorId,
            "Bệnh nhân đã check-in",
            "Bệnh nhân đã đến và check-in cho lịch hẹn.",
            "AppointmentCheckedIn", "High",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Checked in successfully",
            Data = await MapToResponseWithProfilesAsync(appointment)
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

            if (result?.Data != null)
            {
                var profileTasks = result.Data.Select(async doctor =>
                {
                    if (doctor.DoctorId != Guid.Empty)
                    {
                        var resolvedUserId = await _authServiceClient.GetUserIdByDoctorIdAsync(doctor.DoctorId);
                        if (resolvedUserId.HasValue)
                        {
                            doctor.UserId = resolvedUserId.Value;
                        }
                    }

                    doctor.UserProfile = await _authServiceClient.GetUserProfileDetailAsync(doctor.UserId);
                });
                await Task.WhenAll(profileTasks);
            }

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

        // Notify patient that encounter started
        await NotifyAsync(encounter.PatientId,
            "Bắt đầu khám bệnh",
            "Buổi khám bệnh của bạn đã bắt đầu.",
            "EncounterCreated", "Normal",
            encounter.EncounterId.ToString(), "Encounter");

        return new ApiResponse<EncounterResponse>
        {
            Success = true,
            Message = "Encounter created successfully",
            Data = await MapToResponseWithProfilesAsync(encounter)
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
            Data = await MapToResponseWithProfilesAsync(encounter)
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

        var responses = items.Select(MapToResponse).ToList();
        await AttachEncounterProfilesAsync(responses);

        return new PagedResponse<EncounterResponse>
        {
            Data = responses,
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

        var responses = items.Select(MapToResponse).ToList();
        await AttachEncounterProfilesAsync(responses);

        return new PagedResponse<EncounterResponse>
        {
            Data = responses,
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
            Data = await MapToResponseWithProfilesAsync(encounter)
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

        // Notify patient that encounter is completed
        var body = ehrId.HasValue
            ? "Buổi khám bệnh đã hoàn tất. Hồ sơ bệnh án đã được tạo."
            : "Buổi khám bệnh đã hoàn tất.";
        await NotifyAsync(encounter.PatientId,
            "Khám bệnh hoàn tất",
            body,
            "EncounterCompleted", "Normal",
            encounter.EncounterId.ToString(), "Encounter");

        return new ApiResponse<EncounterResponse>
        {
            Success = true,
            Message = ehrId.HasValue 
                ? $"Encounter completed and EHR created ({ehrId})" 
                : "Encounter completed successfully",
            Data = await MapToResponseWithProfilesAsync(encounter)
        };
    }

    // =========================================================================
    // DOCTOR - PATIENTS (Danh sách bệnh nhân đã khám + có consent)
    // =========================================================================

    /// <summary>
    /// Lấy danh sách bệnh nhân đã khám của bác sĩ.
    /// Chỉ trả về bệnh nhân có cả encounter đã hoàn tất VÀ consent hợp lệ.
    /// Flow: Encounters → Consent Service (verify) → Auth Service (patient info)
    /// </summary>
    public async Task<PagedResponse<DoctorPatientResponse>> GetPatientsByDoctorAsync(
        Guid doctorId, int page = 1, int pageSize = 10)
    {
        // Step 1: Lấy danh sách patientIds đã khám (từ encounters có appointment COMPLETED)
        var encounterGroups = await _context.Encounters
            .Include(e => e.Appointment)
            .Where(e => e.DoctorId == doctorId 
                && e.Appointment != null 
                && e.Appointment.Status == AppointmentStatus.COMPLETED)
            .GroupBy(e => e.PatientId)
            .Select(g => new
            {
                PatientId = g.Key,
                TotalEncounters = g.Count(),
                LastVisitAt = g.Max(e => e.CreatedAt),
                LastEncounterId = g.OrderByDescending(e => e.CreatedAt).First().EncounterId,
                LastOrgId = g.OrderByDescending(e => e.CreatedAt).First().OrgId
            })
            .OrderByDescending(g => g.LastVisitAt)
            .ToListAsync();

        if (!encounterGroups.Any())
        {
            return new PagedResponse<DoctorPatientResponse>
            {
                Data = new List<DoctorPatientResponse>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        // Step 2: Kiểm tra consent cho từng patient với doctor (gọi Consent Service)
        var consentedPatientIds = await GetConsentedPatientIdsAsync(
            doctorId, encounterGroups.Select(g => g.PatientId).ToList());

        // Filter chỉ giữ patient có consent
        var filteredGroups = encounterGroups
            .Where(g => consentedPatientIds.Contains(g.PatientId))
            .ToList();

        var totalCount = filteredGroups.Count;

        // Phân trang
        var pagedGroups = filteredGroups
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Step 3: Lấy thông tin patient (tên, email, sdt) từ Auth Service
        var patientInfos = await GetPatientInfosAsync(
            pagedGroups.Select(g => g.PatientId).ToList());

        // Map kết quả
        var result = pagedGroups.Select(g =>
        {
            var info = patientInfos.GetValueOrDefault(g.PatientId);
            return new DoctorPatientResponse
            {
                PatientId = g.PatientId,
                PatientProfile = info?.Profile,
                PatientName = info?.FullName,
                Email = info?.Email,
                Phone = info?.Phone,
                DateOfBirth = info?.DateOfBirth,
                Gender = info?.Gender,
                TotalEncounters = g.TotalEncounters,
                LastVisitAt = g.LastVisitAt,
                LastEncounterId = g.LastEncounterId,
                LastOrgId = g.LastOrgId
            };
        }).ToList();

        return new PagedResponse<DoctorPatientResponse>
        {
            Data = result,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Gọi Consent Service để kiểm tra patient nào có consent cho doctor
    /// </summary>
    private async Task<HashSet<Guid>> GetConsentedPatientIdsAsync(Guid doctorId, List<Guid> patientIds)
    {
        var consentedIds = new HashSet<Guid>();

        try
        {
            var client = _httpClientFactory.CreateClient("ConsentService");

            // Forward JWT token
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);

            // Gọi API lấy consents where grantee = doctor
            var response = await client.GetAsync($"api/v1/consents/by-grantee/{doctorId}?page=1&pageSize=1000");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                // Parse consents response — lấy danh sách patientId có consent ACTIVE
                if (doc.RootElement.TryGetProperty("data", out var dataArray))
                {
                    foreach (var consent in dataArray.EnumerateArray())
                    {
                        // Chỉ lấy consent có status ACTIVE
                        var status = consent.GetProperty("status").GetString();
                        if (status == "ACTIVE" || status == "Active")
                        {
                            var patientId = consent.GetProperty("patientId").GetGuid();
                            if (patientIds.Contains(patientId))
                            {
                                consentedIds.Add(patientId);
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("Consent Service returned {StatusCode} when checking consents for doctor {DoctorId}",
                    response.StatusCode, doctorId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check consents for doctor {DoctorId}. Consent Service may be unavailable.", doctorId);
        }

        return consentedIds;
    }

    /// <summary>
    /// Gọi Auth Service để lấy thông tin patient (tên, email, sdt, ngày sinh, giới tính)
    /// </summary>
    private async Task<Dictionary<Guid, PatientInfo>> GetPatientInfosAsync(List<Guid> patientIds)
    {
        var result = new Dictionary<Guid, PatientInfo>();

        try
        {
            // Resolve patientId -> userId, then fetch profile by userId.
            var tasks = patientIds.Select(async patientId =>
            {
                try
                {
                    var userId = await _authServiceClient.GetUserIdByPatientIdAsync(patientId);
                    if (!userId.HasValue)
                    {
                        return;
                    }

                    var profile = await _authServiceClient.GetUserProfileDetailAsync(userId.Value);
                    if (profile == null)
                    {
                        return;
                    }

                    var info = new PatientInfo
                    {
                        Profile = profile,
                        FullName = profile.FullName,
                        Email = profile.Email,
                        Phone = profile.Phone,
                        DateOfBirth = profile.DateOfBirth,
                        Gender = profile.Gender
                    };

                    lock (result)
                    {
                        result[patientId] = info;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get patient info for {PatientId}", patientId);
                }
            });

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get patient infos from Auth Service");
        }

        return result;
    }

    /// <summary>
    /// Internal DTO cho patient info từ Auth Service
    /// </summary>
    private class PatientInfo
    {
        public AuthUserProfileDetailDto? Profile { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
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

            // Forward JWT token for authentication
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
            }

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

            var response = await client.PostAsync("api/v1/ehr/records", content);

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

    private async Task NotifyAsync(Guid recipientUserId, string title, string body,
        string type, string priority, string? referenceId, string? referenceType)
    {
        if (_notificationClient != null)
        {
            await _notificationClient.SendAsync(recipientUserId, title, body, type, priority, referenceId, referenceType);
        }
    }

    private async Task<ApiResponse<bool>> ValidateDoctorAndOrganizationAsync(Guid doctorId, Guid orgId)
    {
        var doctorUserId = await _authServiceClient.GetUserIdByDoctorIdAsync(doctorId);
        if (!doctorUserId.HasValue)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Doctor not found"
            };
        }

        var organizationExists = await OrganizationExistsAsync(orgId);
        if (!organizationExists)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Organization not found"
            };
        }

        var doctorInOrganization = await IsDoctorInOrganizationAsync(doctorUserId.Value, orgId);
        if (!doctorInOrganization)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Doctor does not belong to this organization"
            };
        }

        return new ApiResponse<bool>
        {
            Success = true,
            Data = true
        };
    }

    private async Task<bool> OrganizationExistsAsync(Guid orgId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OrganizationService");
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            }

            var response = await client.GetAsync($"api/v1/organizations/{orgId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate organization {OrgId}", orgId);
            return false;
        }
    }

    private async Task<bool> IsDoctorInOrganizationAsync(Guid doctorUserId, Guid orgId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OrganizationService");
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
            }

            var response = await client.GetAsync($"api/v1/memberships/by-user/{doctorUserId}?page=1&pageSize=100");
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var membership in dataElement.EnumerateArray())
            {
                if (!membership.TryGetProperty("orgId", out var orgIdElement) || orgIdElement.GetGuid() != orgId)
                {
                    continue;
                }

                if (!membership.TryGetProperty("status", out var statusElement))
                {
                    continue;
                }

                var status = statusElement.GetString();
                if (string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate doctor membership for doctor user {DoctorUserId} in organization {OrgId}", doctorUserId, orgId);
            return false;
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

    private async Task<AppointmentResponse> MapToResponseWithProfilesAsync(Models.Entities.Appointment appt)
    {
        var response = MapToResponse(appt);
        await AttachAppointmentProfileAsync(response);
        return response;
    }

    private async Task<EncounterResponse> MapToResponseWithProfilesAsync(Encounter encounter)
    {
        var response = MapToResponse(encounter);
        await AttachEncounterProfileAsync(response);
        return response;
    }

    private async Task AttachAppointmentProfilesAsync(List<AppointmentResponse> responses)
    {
        var tasks = responses.Select(AttachAppointmentProfileAsync);
        await Task.WhenAll(tasks);
    }

    private async Task AttachEncounterProfilesAsync(List<EncounterResponse> responses)
    {
        var tasks = responses.Select(AttachEncounterProfileAsync);
        await Task.WhenAll(tasks);
    }

    private async Task AttachAppointmentProfileAsync(AppointmentResponse response)
    {
        var patientUserIdTask = _authServiceClient.GetUserIdByPatientIdAsync(response.PatientId);
        var doctorUserIdTask = _authServiceClient.GetUserIdByDoctorIdAsync(response.DoctorId);

        await Task.WhenAll(patientUserIdTask, doctorUserIdTask);

        if (patientUserIdTask.Result.HasValue)
        {
            response.PatientProfile = await _authServiceClient.GetUserProfileDetailAsync(patientUserIdTask.Result.Value);
        }

        if (doctorUserIdTask.Result.HasValue)
        {
            response.DoctorProfile = await _authServiceClient.GetUserProfileDetailAsync(doctorUserIdTask.Result.Value);
        }
    }

    private async Task AttachEncounterProfileAsync(EncounterResponse response)
    {
        var patientUserIdTask = _authServiceClient.GetUserIdByPatientIdAsync(response.PatientId);
        var doctorUserIdTask = _authServiceClient.GetUserIdByDoctorIdAsync(response.DoctorId);

        await Task.WhenAll(patientUserIdTask, doctorUserIdTask);

        if (patientUserIdTask.Result.HasValue)
        {
            response.PatientProfile = await _authServiceClient.GetUserProfileDetailAsync(patientUserIdTask.Result.Value);
        }

        if (doctorUserIdTask.Result.HasValue)
        {
            response.DoctorProfile = await _authServiceClient.GetUserProfileDetailAsync(doctorUserIdTask.Result.Value);
        }
    }
}
