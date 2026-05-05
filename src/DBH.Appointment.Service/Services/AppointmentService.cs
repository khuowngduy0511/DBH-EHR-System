using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DBH.Appointment.Service.DbContext;
using DBH.Appointment.Service.DTOs;
using DBH.Appointment.Service.Models.Entities;
using DBH.Appointment.Service.Models.Enums;
using DBH.Shared.Contracts;
using DBH.Shared.Contracts.Events;
using DBH.Shared.Contracts.Commands;
using DBH.Shared.Infrastructure.Messaging;
using DBH.Shared.Infrastructure.Notification;
using DBH.Shared.Infrastructure.Time;
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
    private readonly IOrganizationServiceClient _organizationServiceClient;
    private readonly INotificationServiceClient? _notificationClient;

    public AppointmentService(
        AppointmentDbContext context,
        ILogger<AppointmentService> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IAuthServiceClient authServiceClient,
        IOrganizationServiceClient organizationServiceClient,
        IMessagePublisher? messagePublisher = null,
        INotificationServiceClient? notificationClient = null)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _authServiceClient = authServiceClient;
        _organizationServiceClient = organizationServiceClient;
        _messagePublisher = messagePublisher;
        _notificationClient = notificationClient;
    }

    // =========================================================================
    // APPOINTMENTS - CRUD
    // =========================================================================

    public async Task<ApiResponse<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        var actorUserId = GetCurrentActorId();
        var scheduledAtUtc = request.ScheduledAt.Kind == DateTimeKind.Utc
            ? request.ScheduledAt
            : request.ScheduledAt.ToUniversalTime();

        if (scheduledAtUtc < VietnamTimeHelper.Now)
        {
            _logger.LogWarning("Create appointment rejected: scheduled time {ScheduledAtUtc} is in the past", scheduledAtUtc);
            _logger.LogInformation("Current server time is {CurrentTimeVn}", VietnamTime.Now);
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Ngày hẹn không được nằm trong quá khứ."
            };
        }

        var patientUserId = await _authServiceClient.GetUserIdByPatientIdAsync(request.PatientId);
        if (!patientUserId.HasValue)
        {
            _logger.LogWarning("Create appointment rejected: patient profile {PatientId} not found in Auth Service", request.PatientId);
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Không tìm thấy hồ sơ bệnh nhân."
            };
        }

        // FIX IDOR
        var userClaims = _httpContextAccessor.HttpContext?.User;
        if (userClaims != null && 
            !userClaims.IsInRole("Admin") && 
            !userClaims.IsInRole("Receptionist") && 
            !userClaims.IsInRole("Doctor"))
        {
            var currentUserIdStr = userClaims.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserIdStr != patientUserId.Value.ToString())
            {
                _logger.LogWarning("IDOR Attempt: User {CurrentUserId} tried to book an appt for Patient's UserId {PatientUserId}", 
                    currentUserIdStr, patientUserId.Value);

                return new ApiResponse<AppointmentResponse>
                {
                    Success = false,
                    Message = "Bạn không có quyền đặt lịch khám thay cho bệnh nhân khác."
                };
            }
        }
        //
        

        var doctorValidation = await ValidateDoctorAndOrganizationAsync(request.DoctorId, request.OrgId);
        if (!doctorValidation.Success)
        {
            _logger.LogWarning(
                "Create appointment rejected: doctor/org validation failed for Doctor {DoctorId}, Org {OrgId}. Reason: {Reason}",
                request.DoctorId, request.OrgId, doctorValidation.Message);
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
            _logger.LogWarning(
                "Create appointment rejected: doctor {DoctorId} already has appointment at {ScheduledAtUtc}",
                request.DoctorId, scheduledAtUtc);
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
            _logger.LogWarning(
                "Create appointment rejected: patient {PatientId} already has appointment at {ScheduledAtUtc}",
                request.PatientId, scheduledAtUtc);
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Bệnh nhân đã có lịch hẹn vào thời gian này. Vui lòng chọn khung giờ khác."
            };
        }

        var patientSameDayValidation = await ValidatePatientSameDayAppointmentRulesAsync(
            request.PatientId,
            request.OrgId,
            scheduledAtUtc);

        if (!patientSameDayValidation.Success)
        {
            _logger.LogWarning(
                "Create appointment rejected: same-day rule validation failed for Patient {PatientId}, Org {OrgId}, ScheduledAt {ScheduledAtUtc}. Reason: {Reason}",
                request.PatientId,
                request.OrgId,
                scheduledAtUtc,
                patientSameDayValidation.Message);

            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = patientSameDayValidation.Message
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
            Status = AppointmentStatus.PENDING,
            CreatedAt = VietnamTime.DatabaseNow,
            CreatedBy = actorUserId,
            UpdatedAt = VietnamTime.DatabaseNow,
            UpdatedBy = actorUserId
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
        var vnTime = TimeZoneInfo.ConvertTimeFromUtc(appointment.ScheduledAt, VietnamTime.TimeZone);
        await NotifyDoctorAsync(appointment.DoctorId,
            "Lịch hẹn mới",
            $"Bạn có lịch hẹn mới vào lúc {vnTime:dd/MM/yyyy HH:mm}",
            "AppointmentCreated", "Normal",
            appointment.AppointmentId.ToString(), "Appointment");

        // Notify patient about appointment created
        await NotifyPatientAsync(appointment.PatientId,
            "Đặt lịch hẹn thành công",
            $"Bạn đã đặt lịch hẹn vào lúc {vnTime:dd/MM/yyyy HH:mm}.",
            "AppointmentCreated", "Normal",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Đặt lịch hẹn thành công.",
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
                Message = "Không tìm thấy lịch hẹn."
            };
        }

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Data = await MapToResponseWithProfilesAsync(appointment)
        };
    }

    public async Task<PagedResponse<AppointmentResponse>> GetAppointmentsAsync(
        Guid? patientId, Guid? doctorId, Guid? orgId, AppointmentStatus? status, string? statusList = null,
        DateTime? fromDate = null, DateTime? toDate = null, string? searchTerm = null,
        int page = 1, int pageSize = 10)
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

        if (!string.IsNullOrEmpty(statusList))
        {
            var statuses = statusList.Split(',')
                .Select(s => Enum.TryParse<AppointmentStatus>(s, true, out var parsed) ? parsed : (AppointmentStatus?)null)
                .Where(s => s.HasValue)
                .Select(s => s.Value)
                .ToList();

            if (statuses.Any())
            {
                query = query.Where(a => statuses.Contains(a.Status));
            }
        }

        if (fromDate.HasValue)
        {
            var fromUtc = fromDate.Value.Kind == DateTimeKind.Utc ? fromDate.Value : DateTime.SpecifyKind(fromDate.Value, DateTimeKind.Utc);
            query = query.Where(a => a.ScheduledAt >= fromUtc);
        }

        if (toDate.HasValue)
        {
            var toUtc = toDate.Value.Kind == DateTimeKind.Utc ? toDate.Value : DateTime.SpecifyKind(toDate.Value, DateTimeKind.Utc);
            query = query.Where(a => a.ScheduledAt <= toUtc);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var bearerToken = GetBearerToken();
            var matchingUserIds = await _authServiceClient.SearchUserIdsAsync(searchTerm);
            var matchingOrgIds = await _organizationServiceClient.SearchOrganizationIdsAsync(searchTerm, bearerToken ?? "");
            
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(a => 
                a.AppointmentId.ToString().ToLower().Contains(lowerSearchTerm) ||
                matchingUserIds.Contains(a.PatientId) ||
                matchingUserIds.Contains(a.DoctorId) ||
                matchingOrgIds.Contains(a.OrgId) ||
                a.ScheduledAt.ToString().Contains(searchTerm) ||
                a.Encounters.Any(e => e.EncounterId.ToString().ToLower().Contains(lowerSearchTerm)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.Status == AppointmentStatus.PENDING ? 1 :
                          a.Status == AppointmentStatus.CONFIRMED ? 2 :
                          a.Status == AppointmentStatus.CHECKED_IN ? 3 :
                          a.Status == AppointmentStatus.IN_PROGRESS ? 4 :
                          a.Status == AppointmentStatus.COMPLETED ? 5 :
                          a.Status == AppointmentStatus.CANCELLED ? 6 : 99)
            .ThenBy(a => a.ScheduledAt)
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
        var actorUserId = GetCurrentActorId();
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Không tìm thấy lịch hẹn."
            };
        }

        var oldStatus = appointment.Status.ToString();
        appointment.Status = status;
        appointment.UpdatedAt = VietnamTime.DatabaseNow;
        appointment.UpdatedBy = actorUserId;
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
            Message = $"Cập nhật trạng thái lịch hẹn thành {status} thành công.",
            Data = await MapToResponseWithProfilesAsync(appointment)
        };
    }

    public async Task<ApiResponse<AppointmentResponse>> RescheduleAppointmentAsync(Guid appointmentId, DateTime newDate)
    {
        var actorUserId = GetCurrentActorId();
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Không tìm thấy lịch hẹn."
            };
        }

        var newDateUtc = newDate.Kind == DateTimeKind.Utc
            ? newDate
            : newDate.ToUniversalTime();

        if (newDateUtc < VietnamTimeHelper.Now)
        {
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = "Ngày hẹn không được nằm trong quá khứ."
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
        appointment.UpdatedAt = VietnamTime.DatabaseNow;
        appointment.UpdatedBy = actorUserId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Rescheduled appointment {Id} to {Date}", appointmentId, newDateUtc);

        // Notify both parties about reschedule
        var vnTime = TimeZoneInfo.ConvertTimeFromUtc(newDateUtc, VietnamTime.TimeZone);
        await NotifyPatientAsync(appointment.PatientId,
            "Lịch hẹn đã được đổi",
            $"Lịch hẹn đã được đổi sang {vnTime:dd/MM/yyyy HH:mm}.",
            "AppointmentRescheduled", "High",
            appointment.AppointmentId.ToString(), "Appointment");
        await NotifyDoctorAsync(appointment.DoctorId,
            "Lịch hẹn đã được đổi",
            $"Lịch hẹn đã được đổi sang {vnTime:dd/MM/yyyy HH:mm}.",
            "AppointmentRescheduled", "Normal",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Dời lịch hẹn thành công.",
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
        var actorUserId = GetCurrentActorId();
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
            return new ApiResponse<AppointmentResponse> { Success = false, Message = "Không tìm thấy lịch hẹn." };

        if (appointment.Status != AppointmentStatus.PENDING && appointment.Status != AppointmentStatus.RESCHEDULED)
            return new ApiResponse<AppointmentResponse> 
            { 
                Success = false, 
                Message = $"Không thể xác nhận lịch hẹn ở trạng thái {appointment.Status}. Chỉ những lịch hẹn PENDING hoặc RESCHEDULED mới có thể được xác nhận." 
            };

        var oldStatus = appointment.Status.ToString();
        appointment.Status = AppointmentStatus.CONFIRMED;
        appointment.UpdatedAt = VietnamTime.DatabaseNow;
        appointment.UpdatedBy = actorUserId;
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

        await NotifyPatientAsync(appointment.PatientId,
            "Lịch hẹn đã được xác nhận",
            $"Lịch hẹn vào lúc {TimeZoneInfo.ConvertTimeFromUtc(appointment.ScheduledAt, VietnamTime.TimeZone):dd/MM/yyyy HH:mm} đã được bác sĩ xác nhận.",
            "AppointmentReminder", "High",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Xác nhận lịch hẹn thành công.",
            Data = await MapToResponseWithProfilesAsync(appointment)
        };
    }

    /// <summary>
    /// Bác sĩ từ chối lịch hẹn → PENDING → CANCELLED
    /// </summary>
    public async Task<ApiResponse<AppointmentResponse>> RejectAppointmentAsync(Guid appointmentId, string reason)
    {
        var actorUserId = GetCurrentActorId();
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
            return new ApiResponse<AppointmentResponse> { Success = false, Message = "Không tìm thấy lịch hẹn." };

        if (appointment.Status != AppointmentStatus.PENDING)
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = $"Không thể từ chối lịch hẹn ở trạng thái {appointment.Status}. Chỉ những lịch hẹn PENDING mới có thể bị từ chối."
            };

        var oldStatus = appointment.Status.ToString();
        appointment.Status = AppointmentStatus.CANCELLED;
        appointment.CancelReason = reason;
        appointment.UpdatedAt = VietnamTime.DatabaseNow;
        appointment.UpdatedBy = actorUserId;
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

        await NotifyPatientAsync(appointment.PatientId,
            "Lịch hẹn bị từ chối",
            $"Lịch hẹn của bạn đã bị từ chối. Lý do: {reason}",
            "AppointmentReminder", "High",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Đã từ chối lịch hẹn.",
            Data = await MapToResponseWithProfilesAsync(appointment)
        };
    }

    /// <summary>
    /// Hủy lịch hẹn (bệnh nhân hoặc bác sĩ) → CANCELLED
    /// </summary>
    public async Task<ApiResponse<AppointmentResponse>> CancelAppointmentAsync(Guid appointmentId, string reason)
    {
        var actorUserId = GetCurrentActorId();
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
            return new ApiResponse<AppointmentResponse> { Success = false, Message = "Không tìm thấy lịch hẹn." };

        if (appointment.Status == AppointmentStatus.COMPLETED || appointment.Status == AppointmentStatus.CANCELLED)
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = $"Không thể hủy lịch hẹn đang ở trạng thái {appointment.Status}."
            };

        var oldStatus = appointment.Status.ToString();
        appointment.Status = AppointmentStatus.CANCELLED;
        appointment.CancelReason = reason;
        appointment.UpdatedAt = VietnamTime.DatabaseNow;
        appointment.UpdatedBy = actorUserId;
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
        var vnTime = TimeZoneInfo.ConvertTimeFromUtc(appointment.ScheduledAt, VietnamTime.TimeZone);
        await NotifyPatientAsync(appointment.PatientId,
            "Lịch hẹn đã bị hủy",
            $"Lịch hẹn vào lúc {vnTime:dd/MM/yyyy HH:mm} đã bị hủy. Lý do: {reason}",
            "AppointmentReminder", "High",
            appointment.AppointmentId.ToString(), "Appointment");
        await NotifyDoctorAsync(appointment.DoctorId,
            "Lịch hẹn đã bị hủy",
            $"Lịch hẹn vào lúc {vnTime:dd/MM/yyyy HH:mm} đã bị hủy. Lý do: {reason}",
            "AppointmentReminder", "Normal",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Đã hủy lịch hẹn.",
            Data = await MapToResponseWithProfilesAsync(appointment)
        };
    }

    /// <summary>
    /// Bệnh nhân check-in tại cơ sở y tế → CONFIRMED → CHECKED_IN
    /// </summary>
    public async Task<ApiResponse<AppointmentResponse>> CheckInAsync(Guid appointmentId)
    {
        var actorUserId = GetCurrentActorId();
        var appointment = await _context.Appointments.FindAsync(appointmentId);
        if (appointment == null)
            return new ApiResponse<AppointmentResponse> { Success = false, Message = "Không tìm thấy lịch hẹn." };

        if (appointment.Status != AppointmentStatus.CONFIRMED)
            return new ApiResponse<AppointmentResponse>
            {
                Success = false,
                Message = $"Không thể check-in khi lịch hẹn ở trạng thái {appointment.Status}. Chỉ lịch hẹn CONFIRMED mới được phép check-in."
            };

        appointment.Status = AppointmentStatus.CHECKED_IN;
        appointment.UpdatedAt = VietnamTime.DatabaseNow;
        appointment.UpdatedBy = actorUserId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Patient checked in for appointment {Id}", appointmentId);

        await PublishEventAsync(new AppointmentCheckedInEvent
        {
            AppointmentId = appointment.AppointmentId,
            PatientId = appointment.PatientId,
            OrganizationId = appointment.OrgId
        });

        // Notify doctor that patient checked in
        await NotifyDoctorAsync(appointment.DoctorId,
            "Bệnh nhân đã check-in",
            "Bệnh nhân đã đến và check-in cho lịch hẹn.",
            "AppointmentCheckedIn", "High",
            appointment.AppointmentId.ToString(), "Appointment");

        return new ApiResponse<AppointmentResponse>
        {
            Success = true,
            Message = "Check-in thành công.",
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
        var actorUserId = GetCurrentActorId();
        var appointment = await _context.Appointments.FindAsync(request.AppointmentId);
        if (appointment == null)
        {
            return new ApiResponse<EncounterResponse>
            {
                Success = false,
                Message = "Không tìm thấy lịch hẹn liên kết để tạo lượt khám."
            };
        }

        // Update appointment status to IN_PROGRESS
        if (appointment.Status == AppointmentStatus.CONFIRMED || appointment.Status == AppointmentStatus.CHECKED_IN)
        {
            appointment.Status = AppointmentStatus.IN_PROGRESS;
            appointment.UpdatedAt = VietnamTime.DatabaseNow;
            appointment.UpdatedBy = actorUserId;
        }

        var encounter = new Encounter
        {
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            AppointmentId = request.AppointmentId,
            OrgId = request.OrgId,
            Notes = request.Notes,
            CreatedAt = VietnamTime.DatabaseNow,
            CreatedBy = actorUserId,
            UpdatedAt = VietnamTime.DatabaseNow,
            UpdatedBy = actorUserId
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
        await NotifyPatientAsync(encounter.PatientId,
            "Bắt đầu khám bệnh",
            "Buổi khám bệnh của bạn đã bắt đầu.",
            "EncounterCreated", "Normal",
            encounter.EncounterId.ToString(), "Encounter");

        return new ApiResponse<EncounterResponse>
        {
            Success = true,
            Message = "Khởi tạo phiên khám bệnh thành công.",
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
                Message = "Không tìm thấy hồ sơ phiên khám."
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
        var actorUserId = GetCurrentActorId();
        var encounter = await _context.Encounters.FindAsync(encounterId);
        if (encounter == null)
        {
            return new ApiResponse<EncounterResponse>
            {
                Success = false,
                Message = "Không tìm thấy hồ sơ phiên khám."
            };
        }

        if (request.Notes != null)
        {
            encounter.Notes = request.Notes;
        }

        encounter.UpdatedAt = VietnamTime.DatabaseNow;
        encounter.UpdatedBy = actorUserId;

        await _context.SaveChangesAsync();

        return new ApiResponse<EncounterResponse>
        {
            Success = true,
            Message = "Cập nhật phiếu khám thành công.",
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
        var actorUserId = GetCurrentActorId();
        var encounter = await _context.Encounters
            .Include(e => e.Appointment)
            .FirstOrDefaultAsync(e => e.EncounterId == encounterId);

        if (encounter == null)
            return new ApiResponse<EncounterResponse> { Success = false, Message = "Không tìm thấy hồ sơ phiên khám." };

        // Update notes if provided
        if (!string.IsNullOrEmpty(request.Notes))
        {
            encounter.Notes = request.Notes;
        }

        encounter.UpdatedAt = VietnamTime.DatabaseNow;
        encounter.UpdatedBy = actorUserId;

        // Mark appointment as COMPLETED
        if (encounter.Appointment != null && encounter.Appointment.Status != AppointmentStatus.COMPLETED)
        {
            encounter.Appointment.Status = AppointmentStatus.COMPLETED;
            encounter.Appointment.UpdatedAt = VietnamTime.DatabaseNow;
            encounter.Appointment.UpdatedBy = actorUserId;
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
        await NotifyPatientAsync(encounter.PatientId,
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
        Guid doctorId, int page = 1, int pageSize = 10, string? searchTerm = null)
    {
        // Step 1: Lấy danh sách record encounters đã khám của bác sĩ (Status = COMPLETED)
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
        var listPatientIds = encounterGroups.Select(g => g.PatientId).ToList();
        var consentedPatientIds = await GetConsentedPatientIdsAsync(doctorId, listPatientIds);

        // Filter chỉ giữ patient có consent
        var filteredGroups = encounterGroups
            .Where(g => consentedPatientIds.Contains(g.PatientId))
            .ToList();

        // Step 3: Xử lý tìm kiếm (Search)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            // Để tìm theo tên, ta cần load thông tin patient từ AuthService trước cho danh sách này
            var allPatientInfos = await GetPatientInfosAsync(filteredGroups.Select(g => g.PatientId).ToList());
            
            filteredGroups = filteredGroups.Where(g => {
                var info = allPatientInfos.GetValueOrDefault(g.PatientId);
                return g.PatientId.ToString().ToLower().Contains(term) || 
                       (info != null && (info.FullName?.ToLower().Contains(term) == true || info.Email?.ToLower().Contains(term) == true));
            }).ToList();
        }

        var totalCount = filteredGroups.Count;

        // Phân trang
        var pagedGroups = filteredGroups
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Step 4: Lấy thông tin chi tiết cho trang hiện tại (nếu chưa load ở trên hoặc load lại để đảm bảo)
        var pagedPatientInfos = await GetPatientInfosAsync(pagedGroups.Select(g => g.PatientId).ToList());

        // Map kết quả
        var result = pagedGroups.Select(g =>
        {
            var info = pagedPatientInfos.GetValueOrDefault(g.PatientId);
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

    private Guid? GetCurrentActorId()
    {
        var claimValue = _httpContextAccessor.HttpContext?.User
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
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

    private async Task NotifyPatientAsync(Guid patientId, string title, string body,
        string type, string priority, string? referenceId, string? referenceType)
    {
        if (_notificationClient == null) return;
        var userId = await _authServiceClient.GetUserIdByPatientIdAsync(patientId);
        var recipientId = userId ?? patientId; // fallback to patientId if resolve fails
        await _notificationClient.SendAsync(recipientId, title, body, type, priority, referenceId, referenceType);
    }

    private async Task NotifyDoctorAsync(Guid doctorId, string title, string body,
        string type, string priority, string? referenceId, string? referenceType)
    {
        if (_notificationClient == null) return;
        var userId = await _authServiceClient.GetUserIdByDoctorIdAsync(doctorId);
        var recipientId = userId ?? doctorId; // fallback if resolve fails
        await _notificationClient.SendAsync(recipientId, title, body, type, priority, referenceId, referenceType);
    }

    private async Task<ApiResponse<bool>> ValidateDoctorAndOrganizationAsync(Guid doctorId, Guid orgId)
    {
        var doctorUserId = await _authServiceClient.GetUserIdByDoctorIdAsync(doctorId);
        if (!doctorUserId.HasValue)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Không tìm thấy bác sĩ."
            };
        }

        var organizationExists = await OrganizationExistsAsync(orgId);
        if (!organizationExists)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Không tìm thấy cơ sở y tế."
            };
        }

        var doctorInOrganization = await IsDoctorInOrganizationAsync(doctorUserId.Value, orgId);
        if (!doctorInOrganization)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Bác sĩ không thuộc cơ sở y tế này. Vui lòng chọn lại."
            };
        }

        return new ApiResponse<bool>
        {
            Success = true,
            Data = true
        };
    }

    private async Task<ApiResponse<bool>> ValidatePatientSameDayAppointmentRulesAsync(
        Guid patientId,
        Guid orgId,
        DateTime scheduledAtUtc)
    {
        var dayStart = scheduledAtUtc.Date;
        var dayEnd = dayStart.AddDays(1);

        var sameDayPendingOrConfirmedAppointments = await _context.Appointments
            .Where(a =>
                a.PatientId == patientId &&
                (a.Status == AppointmentStatus.PENDING || a.Status == AppointmentStatus.CONFIRMED) &&
                a.ScheduledAt >= dayStart &&
                a.ScheduledAt < dayEnd)
            .ToListAsync();

        var hasPendingOrConfirmedInSameOrg = sameDayPendingOrConfirmedAppointments.Any(a => a.OrgId == orgId);
        if (hasPendingOrConfirmedInSameOrg)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Trong cùng một ngày, bệnh nhân chỉ được có tối đa 1 lịch hẹn PENDING/CONFIRMED tại cùng cơ sở y tế."
            };
        }

        if (sameDayPendingOrConfirmedAppointments.Count >= 2)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Trong cùng một ngày, bệnh nhân chỉ được có tối đa 2 lịch hẹn PENDING/CONFIRMED nếu khác cơ sở y tế."
            };
        }

        var hasLessThanTwoHoursGap = sameDayPendingOrConfirmedAppointments
            .Any(a => Math.Abs((a.ScheduledAt - scheduledAtUtc).TotalHours) < 2);

        if (hasLessThanTwoHoursGap)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Các lịch hẹn PENDING/CONFIRMED trong cùng ngày phải cách nhau ít nhất 2 giờ."
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
            if (reminderTime > VietnamTimeHelper.Now)
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
            CancelReason = appt.CancelReason,
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

    private string? GetBearerToken()
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        return authHeader.Substring("Bearer ".Length).Trim();
    }

}

