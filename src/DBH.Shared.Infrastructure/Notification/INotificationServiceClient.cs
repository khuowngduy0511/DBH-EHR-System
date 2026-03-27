namespace DBH.Shared.Infrastructure.Notification;

/// <summary>
/// HTTP client interface để gọi Notification Service API từ các service khác.
/// Fire-and-forget: không block flow chính nếu notification fail.
/// </summary>
public interface INotificationServiceClient
{
    /// <summary>
    /// Gửi thông báo tới một user cụ thể qua Notification Service API
    /// </summary>
    /// <param name="recipientUserId">User ID người nhận</param>
    /// <param name="title">Tiêu đề thông báo</param>
    /// <param name="body">Nội dung thông báo</param>
    /// <param name="type">Loại thông báo (NotificationType enum string)</param>
    /// <param name="priority">Độ ưu tiên (Normal, High, Urgent, Low)</param>
    /// <param name="referenceId">ID đối tượng liên quan</param>
    /// <param name="referenceType">Loại đối tượng liên quan (Appointment, EHR, Consent, etc.)</param>
    /// <param name="actionUrl">Deep link URL (optional)</param>
    Task SendAsync(
        Guid recipientUserId,
        string title,
        string body,
        string type,
        string priority = "Normal",
        string? referenceId = null,
        string? referenceType = null,
        string? actionUrl = null);
}
