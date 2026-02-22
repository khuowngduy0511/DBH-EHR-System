# DBH.Notification.Service - API Implementation TODO

## Overview
Notification Service quản lý tất cả thông báo (push, email, SMS) cho hệ thống DBH-EHR.
Gồm 3 entities: Notification, DeviceToken, NotificationPreference.

---

## 1. Folders cần tạo
```
DBH.Notification.Service/
├── Controllers/
│   ├── NotificationsController.cs
│   ├── DeviceTokensController.cs
│   └── PreferencesController.cs
├── DTOs/
│   └── NotificationDTOs.cs
├── Services/
│   ├── INotificationService.cs
│   ├── NotificationService.cs
│   ├── IPushNotificationService.cs
│   └── PushNotificationService.cs (Firebase FCM)
```

---

## 2. DTOs cần tạo (DTOs/NotificationDTOs.cs)

### Notification DTOs:
```csharp
// Gửi notification
public class SendNotificationRequest
{
    public string RecipientDid { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string Title { get; set; }
    public string? Body { get; set; }
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationChannel Channel { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? ActionUrl { get; set; }
    public string? Data { get; set; }  // JSON
    public DateTime? ExpiresAt { get; set; }
}

// Gửi broadcast
public class BroadcastNotificationRequest
{
    public List<string> RecipientDids { get; set; }  // hoặc RecipientUserIds
    public string Title { get; set; }
    public string Body { get; set; }
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
}

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string RecipientDid { get; set; }
    public string Title { get; set; }
    public string? Body { get; set; }
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ReferenceId { get; set; }
    public string? ActionUrl { get; set; }
}

public class MarkReadRequest
{
    public List<Guid> NotificationIds { get; set; }
}
```

### DeviceToken DTOs:
```csharp
public class RegisterDeviceRequest
{
    public string UserDid { get; set; }
    public Guid? UserId { get; set; }
    public string FcmToken { get; set; }
    public string DeviceType { get; set; }  // ios, android, web
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
```

### Preference DTOs:
```csharp
public class UpdatePreferencesRequest
{
    public bool? EhrAccessEnabled { get; set; }
    public bool? ConsentRequestEnabled { get; set; }
    public bool? EhrUpdateEnabled { get; set; }
    public bool? AppointmentReminderEnabled { get; set; }
    public bool? SecurityAlertEnabled { get; set; }
    public bool? SystemNotificationEnabled { get; set; }
    public bool? PushEnabled { get; set; }
    public bool? EmailEnabled { get; set; }
    public bool? SmsEnabled { get; set; }
    public string? QuietTimeStart { get; set; }  // "22:00"
    public string? QuietTimeEnd { get; set; }    // "07:00"
}

public class PreferencesResponse
{
    public Guid Id { get; set; }
    public string UserDid { get; set; }
    public bool EhrAccessEnabled { get; set; }
    public bool ConsentRequestEnabled { get; set; }
    public bool EhrUpdateEnabled { get; set; }
    public bool AppointmentReminderEnabled { get; set; }
    public bool SecurityAlertEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public string? QuietTimeStart { get; set; }
    public string? QuietTimeEnd { get; set; }
}
```

---

## 3. Service Interfaces

### INotificationService.cs:
```csharp
public interface INotificationService
{
    // Notifications
    Task<ApiResponse<NotificationResponse>> SendNotificationAsync(SendNotificationRequest request);
    Task<ApiResponse<int>> BroadcastNotificationAsync(BroadcastNotificationRequest request);
    Task<PagedResponse<NotificationResponse>> GetNotificationsByUserAsync(string userDid, int page, int pageSize);
    Task<PagedResponse<NotificationResponse>> GetUnreadNotificationsAsync(string userDid);
    Task<ApiResponse<int>> MarkAsReadAsync(string userDid, MarkReadRequest request);
    Task<ApiResponse<int>> MarkAllAsReadAsync(string userDid);
    Task<ApiResponse<bool>> DeleteNotificationAsync(Guid notificationId);
    Task<int> GetUnreadCountAsync(string userDid);
    
    // Device Tokens
    Task<ApiResponse<DeviceTokenResponse>> RegisterDeviceAsync(RegisterDeviceRequest request);
    Task<List<DeviceTokenResponse>> GetUserDevicesAsync(string userDid);
    Task<ApiResponse<bool>> DeactivateDeviceAsync(Guid deviceTokenId);
    Task<ApiResponse<bool>> DeactivateAllDevicesAsync(string userDid);
    
    // Preferences
    Task<PreferencesResponse> GetPreferencesAsync(string userDid);
    Task<ApiResponse<PreferencesResponse>> UpdatePreferencesAsync(string userDid, UpdatePreferencesRequest request);
}
```

### IPushNotificationService.cs (Firebase):
```csharp
public interface IPushNotificationService
{
    Task<bool> SendPushAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);
    Task<int> SendMulticastAsync(List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null);
}
```

---

## 4. Controller Endpoints

### NotificationsController:
```csharp
[ApiController]
[Route("api/notifications")]
public class NotificationsController
{
    // POST /api/notifications - Gửi notification (internal)
    // POST /api/notifications/broadcast - Broadcast (admin)
    // GET /api/notifications/me - List notifications của user hiện tại
    // GET /api/notifications/me/unread - Unread notifications
    // GET /api/notifications/me/unread-count - Đếm unread
    // POST /api/notifications/me/mark-read - Mark as read
    // POST /api/notifications/me/mark-all-read - Mark all as read
    // DELETE /api/notifications/{id} - Xóa notification
}
```

### DeviceTokensController:
```csharp
[ApiController]
[Route("api/device-tokens")]
public class DeviceTokensController
{
    // POST /api/device-tokens - Đăng ký device
    // GET /api/device-tokens/me - List devices của user
    // DELETE /api/device-tokens/{id} - Xóa device token
    // DELETE /api/device-tokens/me/all - Xóa tất cả devices
}
```

### PreferencesController:
```csharp
[ApiController]
[Route("api/notification-preferences")]
public class PreferencesController
{
    // GET /api/notification-preferences/me - Get preferences
    // PUT /api/notification-preferences/me - Update preferences
}
```

---

## 5. Program.cs cần update

```csharp
// Thêm using
using DBH.Notification.Service.Services;

// Thêm service registration
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();

// Firebase configuration
builder.Services.Configure<FirebaseConfig>(builder.Configuration.GetSection("Firebase"));
```

---

## 6. appsettings.json cần thêm

```json
{
  "Firebase": {
    "ProjectId": "dbh-ehr-project",
    "CredentialsFile": "firebase-credentials.json"
  }
}
```

---

## 7. Authorization Requirements

| Endpoint | Roles Allowed |
|----------|---------------|
| POST /notifications | Internal/Service |
| POST /broadcast | Admin, SystemAdmin |
| GET /me | Authenticated User |
| POST /mark-read | Authenticated User |
| POST /device-tokens | Authenticated User |
| GET /preferences/me | Authenticated User |

---

## 8. Integration với các Service khác

Các service sẽ gọi Notification Service để gửi thông báo:

### Consent Service:
- Khi có access request mới → notify patient
- Khi consent được approve/revoke → notify requester

### EHR Service:
- Khi EHR được truy cập → notify patient
- Khi có EHR mới → notify patient

### Auth Service:
- Khi đăng nhập từ device mới → security alert
- Khi đổi mật khẩu → security alert

---

## 9. Background Jobs cần implement

- Job cleanup expired notifications (daily)
- Job retry failed notifications
- Job sync FCM token validity (weekly)

---

## 10. References

- Entities: 
  - [Models/Entities/Notification.cs](Models/Entities/Notification.cs)
  - [Models/Entities/DeviceToken.cs](Models/Entities/DeviceToken.cs)
  - [Models/Entities/NotificationPreference.cs](Models/Entities/NotificationPreference.cs)
- Enums: [Models/Enums/NotificationEnums.cs](Models/Enums/NotificationEnums.cs)
- DbContext: [Data/NotificationDbContext.cs](Data/NotificationDbContext.cs)
