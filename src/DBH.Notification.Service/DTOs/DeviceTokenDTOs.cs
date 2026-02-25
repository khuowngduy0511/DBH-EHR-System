namespace DBH.Notification.Service.DTOs;

public class RegisterDeviceRequest
{
    public string UserDid { get; set; }
    public Guid? UserId { get; set; }
    public string FcmToken { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceName { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
}

public class DeviceTokenResponse
{
    public Guid Id { get; set; }
    public string FcmToken { get; set; }
    public string DeviceType { get; set; }
    public string? DeviceName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
