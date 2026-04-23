using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Models.Enums;
using DBH.Notification.Service.Services;
using DBH.Shared.Contracts.Events;
using MassTransit;

namespace DBH.Notification.Service.Consumers;

public class UserEventConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserEventConsumer> _logger;

    public UserEventConsumer(INotificationService notificationService, ILogger<UserEventConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming UserRegisteredEvent for UserId={UserId}", evt.UserId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = evt.Did ?? $"did:user:{evt.UserId}",
            RecipientUserId = evt.UserId,
            Title = "Chào mừng đến với DBH-EHR",
            Body = $"Xin chào {evt.FirstName} {evt.LastName}! Tài khoản của bạn đã được tạo thành công.",
            Type = NotificationType.System,
            Priority = NotificationPriority.Normal,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.UserId.ToString(),
            ReferenceType = "User"
        });
    }
}
