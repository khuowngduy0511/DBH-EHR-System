using DBH.Notification.Service.Data;
using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DBH.Notification.Service.Services;

public class DeviceTokenService : IDeviceTokenService
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<DeviceTokenService> _logger;

    public DeviceTokenService(NotificationDbContext context, ILogger<DeviceTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<DeviceTokenResponse>> RegisterDeviceAsync(RegisterDeviceRequest request)
    {
        // Upsert: update existing token if same FCM token already registered
        var existingToken = await _context.DeviceTokens
            .FirstOrDefaultAsync(d => d.FcmToken == request.FcmToken);

        if (existingToken != null)
        {
            existingToken.UserDid = request.UserDid;
            existingToken.UserId = request.UserId;
            existingToken.DeviceType = request.DeviceType;
            existingToken.DeviceName = request.DeviceName;
            existingToken.OsVersion = request.OsVersion;
            existingToken.AppVersion = request.AppVersion;
            existingToken.IsActive = true;
            existingToken.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated device token {DeviceTokenId} for user {UserDid}", existingToken.Id, request.UserDid);

            return new ApiResponse<DeviceTokenResponse>
            {
                Success = true,
                Message = "Device token updated",
                Data = MapToResponse(existingToken)
            };
        }

        var deviceToken = new DeviceToken
        {
            UserDid = request.UserDid,
            UserId = request.UserId,
            FcmToken = request.FcmToken,
            DeviceType = request.DeviceType,
            DeviceName = request.DeviceName,
            OsVersion = request.OsVersion,
            AppVersion = request.AppVersion,
            IsActive = true
        };

        _context.DeviceTokens.Add(deviceToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Registered device token {DeviceTokenId} for user {UserDid}", deviceToken.Id, request.UserDid);

        return new ApiResponse<DeviceTokenResponse>
        {
            Success = true,
            Message = "Device registered successfully",
            Data = MapToResponse(deviceToken)
        };
    }

    public async Task<List<DeviceTokenResponse>> GetUserDevicesAsync(string userDid)
    {
        var devices = await _context.DeviceTokens
            .Where(d => d.UserDid == userDid && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return devices.Select(MapToResponse).ToList();
    }

    public async Task<ApiResponse<bool>> DeactivateDeviceAsync(Guid deviceTokenId)
    {
        var device = await _context.DeviceTokens.FindAsync(deviceTokenId);
        if (device == null)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Device token not found",
                Data = false
            };
        }

        device.IsActive = false;
        device.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new ApiResponse<bool>
        {
            Success = true,
            Message = "Device deactivated",
            Data = true
        };
    }

    public async Task<ApiResponse<bool>> DeactivateAllDevicesAsync(string userDid)
    {
        var devices = await _context.DeviceTokens
            .Where(d => d.UserDid == userDid && d.IsActive)
            .ToListAsync();

        foreach (var device in devices)
        {
            device.IsActive = false;
            device.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new ApiResponse<bool>
        {
            Success = true,
            Message = $"{devices.Count} devices deactivated",
            Data = true
        };
    }

    private static DeviceTokenResponse MapToResponse(DeviceToken d) => new()
    {
        Id = d.Id,
        FcmToken = d.FcmToken,
        DeviceType = d.DeviceType,
        DeviceName = d.DeviceName,
        IsActive = d.IsActive,
        CreatedAt = d.CreatedAt
    };
}
