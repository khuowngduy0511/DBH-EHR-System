using DBH.Notification.Service.Data;
using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DBH.Notification.Service.Services;

public class PreferencesService : IPreferencesService
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<PreferencesService> _logger;

    public PreferencesService(NotificationDbContext context, ILogger<PreferencesService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PreferencesResponse> GetPreferencesAsync(string userDid)
    {
        var prefs = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserDid == userDid);

        // Create default preferences if not found
        if (prefs == null)
        {
            prefs = new NotificationPreference
            {
                UserDid = userDid
            };
            _context.NotificationPreferences.Add(prefs);
            await _context.SaveChangesAsync();
        }

        return MapToResponse(prefs);
    }

    public async Task<ApiResponse<PreferencesResponse>> UpdatePreferencesAsync(string userDid, UpdatePreferencesRequest request)
    {
        var prefs = await _context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserDid == userDid);

        if (prefs == null)
        {
            prefs = new NotificationPreference { UserDid = userDid };
            _context.NotificationPreferences.Add(prefs);
        }

        // Partial update — only update non-null fields
        if (request.EhrAccessEnabled.HasValue) prefs.EhrAccessEnabled = request.EhrAccessEnabled.Value;
        if (request.ConsentRequestEnabled.HasValue) prefs.ConsentRequestEnabled = request.ConsentRequestEnabled.Value;
        if (request.EhrUpdateEnabled.HasValue) prefs.EhrUpdateEnabled = request.EhrUpdateEnabled.Value;
        if (request.AppointmentReminderEnabled.HasValue) prefs.AppointmentReminderEnabled = request.AppointmentReminderEnabled.Value;
        if (request.SecurityAlertEnabled.HasValue) prefs.SecurityAlertEnabled = request.SecurityAlertEnabled.Value;
        if (request.SystemNotificationEnabled.HasValue) prefs.SystemNotificationEnabled = request.SystemNotificationEnabled.Value;
        if (request.PushEnabled.HasValue) prefs.PushEnabled = request.PushEnabled.Value;
        if (request.EmailEnabled.HasValue) prefs.EmailEnabled = request.EmailEnabled.Value;
        if (request.SmsEnabled.HasValue) prefs.SmsEnabled = request.SmsEnabled.Value;

        if (request.QuietTimeStart != null && int.TryParse(request.QuietTimeStart.Replace(":", "").Substring(0, 2), out var startHour))
            prefs.QuietHoursStart = startHour;
        if (request.QuietTimeEnd != null && int.TryParse(request.QuietTimeEnd.Replace(":", "").Substring(0, 2), out var endHour))
            prefs.QuietHoursEnd = endHour;

        prefs.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated preferences for user {UserDid}", userDid);

        return new ApiResponse<PreferencesResponse>
        {
            Success = true,
            Message = "Preferences updated successfully",
            Data = MapToResponse(prefs)
        };
    }

    private static PreferencesResponse MapToResponse(NotificationPreference p) => new()
    {
        Id = p.Id,
        UserDid = p.UserDid,
        EhrAccessEnabled = p.EhrAccessEnabled,
        ConsentRequestEnabled = p.ConsentRequestEnabled,
        EhrUpdateEnabled = p.EhrUpdateEnabled,
        AppointmentReminderEnabled = p.AppointmentReminderEnabled,
        SecurityAlertEnabled = p.SecurityAlertEnabled,
        PushEnabled = p.PushEnabled,
        EmailEnabled = p.EmailEnabled,
        SmsEnabled = p.SmsEnabled,
        QuietTimeStart = $"{p.QuietHoursStart:D2}:00",
        QuietTimeEnd = $"{p.QuietHoursEnd:D2}:00"
    };
}
