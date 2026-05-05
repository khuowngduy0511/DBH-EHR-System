using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Models.Enums;
using DBH.Notification.Service.Services;
using DBH.Shared.Contracts.Events;
using MassTransit;

namespace DBH.Notification.Service.Consumers;

public class EhrEventConsumer :
    IConsumer<EhrCreatedEvent>,
    IConsumer<EhrUpdatedEvent>,
    IConsumer<EhrAccessedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<EhrEventConsumer> _logger;

    public EhrEventConsumer(INotificationService notificationService, ILogger<EhrEventConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EhrCreatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming EhrCreatedEvent for EhrId={EhrId}", evt.EhrId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = evt.PatientDid,
            Title = "Hồ sơ bệnh án mới đã được tạo",
            Body = $"Hồ sơ bệnh án \"{evt.Title}\" đã được thêm vào hệ thống.",
            Type = NotificationType.EhrCreated,
            Priority = NotificationPriority.Normal,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.EhrId.ToString(),
            ReferenceType = "EhrRecord"
        });
    }

    public async Task Consume(ConsumeContext<EhrUpdatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming EhrUpdatedEvent for EhrId={EhrId}", evt.EhrId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = evt.PatientDid,
            Title = "Hồ sơ bệnh án đã được cập nhật",
            Body = $"Hồ sơ bệnh án đã được cập nhật. {evt.ChangeDescription}",
            Type = NotificationType.EhrUpdated,
            Priority = NotificationPriority.Normal,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.EhrId.ToString(),
            ReferenceType = "EhrRecord"
        });
    }

    public async Task Consume(ConsumeContext<EhrAccessedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming EhrAccessedEvent for EhrId={EhrId}", evt.EhrId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = evt.PatientDid,
            Title = "Hồ sơ bệnh án vừa được truy cập",
            Body = $"{evt.AccessedByName} vừa truy cập hồ sơ bệnh án của bạn. Mục đích: {evt.Purpose}",
            Type = NotificationType.EhrAccess,
            Priority = NotificationPriority.Normal,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.EhrId.ToString(),
            ReferenceType = "EhrRecord"
        });
    }
}
