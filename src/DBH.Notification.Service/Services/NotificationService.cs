using DBH.Notification.Service.DbContext;
using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Models.Entities;
using DBH.Notification.Service.Models.Enums;
using DBH.Shared.Contracts;
using DBH.Shared.Infrastructure.Caching;
using DBH.Shared.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;

namespace DBH.Notification.Service.Services;

public class NotificationService : INotificationService
{
    private readonly NotificationDbContext _db;
    private readonly ILogger<NotificationService> _logger;
    private readonly ICacheService _cache;

    private static readonly TimeSpan UnreadCacheTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PrefsCacheTtl = TimeSpan.FromMinutes(15);

    public NotificationService(NotificationDbContext db, ILogger<NotificationService> logger, ICacheService cache)
    {
        _db = db;
        _logger = logger;
        _cache = cache;
    }

    // ========================================================================
    // Notifications
    // ========================================================================

    public async Task<ApiResponse<NotificationResponse>> SendNotificationAsync(SendNotificationRequest request)
    {
        try
        {
            var notification = new Models.Entities.NotificationEntity
            {
                Id = Guid.NewGuid(),
                RecipientDid = request.RecipientDid,
                RecipientUserId = request.RecipientUserId,
                Title = request.Title,
                Body = request.Body,
                Type = request.Type,
                Priority = request.Priority,
                Channel = request.Channel,
                Status = NotificationStatus.Pending,
                ReferenceId = request.ReferenceId,
                ReferenceType = request.ReferenceType,
                ActionUrl = request.ActionUrl,
                Data = request.Data,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = VietnamTime.DatabaseNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            // TODO: Push notification via Firebase FCM if channel is Push
            // For now, mark as sent immediately for in-app
            if (notification.Channel == NotificationChannel.InApp)
            {
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = VietnamTime.DatabaseNow;
                await _db.SaveChangesAsync();
            }

            _logger.LogInformation("Notification sent: {Id} type={Type} to={RecipientDid}",
                notification.Id, notification.Type, notification.RecipientDid);

            await _cache.RemoveAsync($"unread:{request.RecipientDid}");

            return ApiResponse<NotificationResponse>.Ok(MapToResponse(notification));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to {RecipientDid}", request.RecipientDid);
            return ApiResponse<NotificationResponse>.Fail($"Failed to send notification: {ex.Message}");
        }
    }

    public async Task<ApiResponse<int>> BroadcastNotificationAsync(BroadcastNotificationRequest request)
    {
        var count = 0;
        foreach (var did in request.RecipientDids)
        {
            var notification = new Models.Entities.NotificationEntity
            {
                RecipientDid = did,
                Title = request.Title,
                Body = request.Body,
                Type = request.Type,
                Priority = request.Priority,
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Sent,
                SentAt = VietnamTime.DatabaseNow,
                CreatedAt = VietnamTime.DatabaseNow
            };
            _db.Notifications.Add(notification);
            count++;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Broadcast notification sent to {Count} recipients", count);
        return ApiResponse<int>.Ok(count);
    }

    public async Task<PagedResponse<NotificationResponse>> GetNotificationsByUserAsync(string userDid, int page, int pageSize)
    {
        var query = _db.Notifications
            .Where(n => n.RecipientDid == userDid)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResponse<NotificationResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResponse<NotificationResponse>> GetUnreadNotificationsAsync(string userDid, int page, int pageSize)
    {
        var query = _db.Notifications
            .Where(n => n.RecipientDid == userDid && n.Status != NotificationStatus.Read)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResponse<NotificationResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ApiResponse<int>> MarkAsReadAsync(string userDid, MarkReadRequest request)
    {
        var notifications = await _db.Notifications
            .Where(n => n.RecipientDid == userDid && request.NotificationIds.Contains(n.Id))
            .ToListAsync();

        foreach (var n in notifications)
        {
            n.Status = NotificationStatus.Read;
            n.ReadAt = VietnamTime.DatabaseNow;
        }

        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"unread:{userDid}");
        return ApiResponse<int>.Ok(notifications.Count);
    }

    public async Task<ApiResponse<int>> MarkAllAsReadAsync(string userDid)
    {
        var unread = await _db.Notifications
            .Where(n => n.RecipientDid == userDid && n.Status != NotificationStatus.Read)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.Status = NotificationStatus.Read;
            n.ReadAt = VietnamTime.DatabaseNow;
        }

        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"unread:{userDid}");
        return ApiResponse<int>.Ok(unread.Count);
    }

    public async Task<ApiResponse<bool>> DeleteNotificationAsync(Guid notificationId)
    {
        var notification = await _db.Notifications.FindAsync(notificationId);
        if (notification == null)
            return ApiResponse<bool>.Fail("Notification not found");

        var recipientDid = notification.RecipientDid;
        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"unread:{recipientDid}");
        return ApiResponse<bool>.Ok(true);
    }

    public async Task<int> GetUnreadCountAsync(string userDid)
    {
        var cacheKey = $"unread:{userDid}";
        var cached = await _cache.GetAsync<int?>(cacheKey);
        if (cached.HasValue) return cached.Value;

        var count = await _db.Notifications
            .CountAsync(n => n.RecipientDid == userDid && n.Status != NotificationStatus.Read);
        await _cache.SetAsync(cacheKey, count, UnreadCacheTtl);
        return count;
    }

    // ========================================================================
    // Device Tokens
    // ========================================================================

    public async Task<ApiResponse<DeviceTokenResponse>> RegisterDeviceAsync(RegisterDeviceRequest request)
    {
        // Check if token already exists
        var existing = await _db.DeviceTokens
            .FirstOrDefaultAsync(d => d.FcmToken == request.FcmToken);

        if (existing != null)
        {
            existing.UserDid = request.UserDid;
            existing.UserId = request.UserId;
            existing.DeviceName = request.DeviceName;
            existing.OsVersion = request.OsVersion;
            existing.AppVersion = request.AppVersion;
            existing.IsActive = true;
            existing.UpdatedAt = VietnamTime.DatabaseNow;
        }
        else
        {
            existing = new DeviceToken
            {
                UserDid = request.UserDid,
                UserId = request.UserId,
                FcmToken = request.FcmToken,
                DeviceType = request.DeviceType,
                DeviceName = request.DeviceName,
                OsVersion = request.OsVersion,
                AppVersion = request.AppVersion,
                IsActive = true,
                CreatedAt = VietnamTime.DatabaseNow,
                UpdatedAt = VietnamTime.DatabaseNow
            };
            _db.DeviceTokens.Add(existing);
        }

        await _db.SaveChangesAsync();
        return ApiResponse<DeviceTokenResponse>.Ok(MapDeviceToResponse(existing));
    }

    public async Task<List<DeviceTokenResponse>> GetUserDevicesAsync(string userDid)
    {
        var devices = await _db.DeviceTokens
            .Where(d => d.UserDid == userDid && d.IsActive)
            .OrderByDescending(d => d.UpdatedAt)
            .ToListAsync();

        return devices.Select(MapDeviceToResponse).ToList();
    }

    public async Task<ApiResponse<bool>> DeactivateDeviceAsync(Guid deviceTokenId)
    {
        var device = await _db.DeviceTokens.FindAsync(deviceTokenId);
        if (device == null) return ApiResponse<bool>.Fail("Device not found");

        device.IsActive = false;
        device.UpdatedAt = VietnamTime.DatabaseNow;
        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<bool>> DeactivateAllDevicesAsync(string userDid)
    {
        var devices = await _db.DeviceTokens
            .Where(d => d.UserDid == userDid && d.IsActive)
            .ToListAsync();

        foreach (var d in devices)
        {
            d.IsActive = false;
            d.UpdatedAt = VietnamTime.DatabaseNow;
        }

        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }

    // ========================================================================
    // Preferences
    // ========================================================================

    public async Task<PreferencesResponse> GetPreferencesAsync(string userDid)
    {
        var prefKey = $"prefs:{userDid}";
        var prefCached = await _cache.GetAsync<PreferencesResponse>(prefKey);
        if (prefCached != null) return prefCached;

        var pref = await _db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserDid == userDid);

        if (pref == null)
        {
            // Create default preferences
            pref = new NotificationPreference
            {
                UserDid = userDid,
                CreatedAt = VietnamTime.DatabaseNow,
                UpdatedAt = VietnamTime.DatabaseNow
            };
            _db.NotificationPreferences.Add(pref);
            await _db.SaveChangesAsync();
        }

        var prefResult = MapPreferencesToResponse(pref);
        await _cache.SetAsync(prefKey, prefResult, PrefsCacheTtl);
        return prefResult;
    }

    public async Task<ApiResponse<PreferencesResponse>> UpdatePreferencesAsync(string userDid, UpdatePreferencesRequest request)
    {
        var pref = await _db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserDid == userDid);

        if (pref == null)
        {
            pref = new NotificationPreference { UserDid = userDid };
            _db.NotificationPreferences.Add(pref);
        }

        if (request.EhrAccessEnabled.HasValue) pref.EhrAccessEnabled = request.EhrAccessEnabled.Value;
        if (request.ConsentRequestEnabled.HasValue) pref.ConsentRequestEnabled = request.ConsentRequestEnabled.Value;
        if (request.EhrUpdateEnabled.HasValue) pref.EhrUpdateEnabled = request.EhrUpdateEnabled.Value;
        if (request.AppointmentReminderEnabled.HasValue) pref.AppointmentReminderEnabled = request.AppointmentReminderEnabled.Value;
        if (request.SecurityAlertEnabled.HasValue) pref.SecurityAlertEnabled = request.SecurityAlertEnabled.Value;
        if (request.SystemNotificationEnabled.HasValue) pref.SystemNotificationEnabled = request.SystemNotificationEnabled.Value;
        if (request.PushEnabled.HasValue) pref.PushEnabled = request.PushEnabled.Value;
        if (request.EmailEnabled.HasValue) pref.EmailEnabled = request.EmailEnabled.Value;
        if (request.SmsEnabled.HasValue) pref.SmsEnabled = request.SmsEnabled.Value;
        if (request.QuietHoursEnabled.HasValue) pref.QuietHoursEnabled = request.QuietHoursEnabled.Value;
        if (request.QuietHoursStart.HasValue) pref.QuietHoursStart = request.QuietHoursStart.Value;
        if (request.QuietHoursEnd.HasValue) pref.QuietHoursEnd = request.QuietHoursEnd.Value;
        pref.UpdatedAt = VietnamTime.DatabaseNow;

        await _db.SaveChangesAsync();
        await _cache.RemoveAsync($"prefs:{userDid}");
        return ApiResponse<PreferencesResponse>.Ok(MapPreferencesToResponse(pref));
    }

    // ========================================================================
    // Mapping
    // ========================================================================

    private static NotificationResponse MapToResponse(Models.Entities.NotificationEntity n) => new()
    {
        Id = n.Id,
        RecipientDid = n.RecipientDid,
        Title = n.Title,
        Body = n.Body,
        Type = n.Type,
        Priority = n.Priority,
        Channel = n.Channel,
        Status = n.Status,
        CreatedAt = n.CreatedAt,
        SentAt = n.SentAt,
        ReadAt = n.ReadAt,
        ReferenceId = n.ReferenceId,
        ActionUrl = n.ActionUrl
    };

    private static DeviceTokenResponse MapDeviceToResponse(DeviceToken d) => new()
    {
        Id = d.Id,
        FcmToken = d.FcmToken,
        DeviceType = d.DeviceType,
        DeviceName = d.DeviceName,
        IsActive = d.IsActive,
        CreatedAt = d.CreatedAt
    };

    private static PreferencesResponse MapPreferencesToResponse(NotificationPreference p) => new()
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
        QuietHoursEnabled = p.QuietHoursEnabled,
        QuietHoursStart = p.QuietHoursStart,
        QuietHoursEnd = p.QuietHoursEnd
    };
}
