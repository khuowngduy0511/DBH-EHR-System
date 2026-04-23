using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Models.Enums;
using DBH.Notification.Service.Services;
using DBH.Shared.Contracts.Events;
using MassTransit;

namespace DBH.Notification.Service.Consumers;

public class ConsentEventConsumer :
    IConsumer<ConsentGrantedEvent>,
    IConsumer<ConsentRevokedEvent>,
    IConsumer<AccessRequestCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<ConsentEventConsumer> _logger;

    public ConsentEventConsumer(INotificationService notificationService, ILogger<ConsentEventConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ConsentGrantedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming ConsentGrantedEvent for ConsentId={ConsentId}", evt.ConsentId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = evt.GranteeDid,
            Title = "Consent đã được cấp",
            Body = $"Bệnh nhân đã cấp consent truy cập hồ sơ bệnh án cho bạn. Mục đích: {evt.Purpose}",
            Type = NotificationType.ConsentGranted,
            Priority = NotificationPriority.Normal,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.ConsentId.ToString(),
            ReferenceType = "Consent"
        });
    }

    public async Task Consume(ConsumeContext<ConsentRevokedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming ConsentRevokedEvent for ConsentId={ConsentId}", evt.ConsentId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = evt.GranteeDid,
            Title = "Consent đã bị thu hồi",
            Body = $"Bệnh nhân đã thu hồi consent truy cập hồ sơ bệnh án. Lý do: {evt.Reason}",
            Type = NotificationType.ConsentRevoked,
            Priority = NotificationPriority.High,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.ConsentId.ToString(),
            ReferenceType = "Consent"
        });
    }

    public async Task Consume(ConsumeContext<AccessRequestCreatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming AccessRequestCreatedEvent for RequestId={RequestId}", evt.RequestId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = evt.PatientDid,
            Title = "Yêu cầu truy cập hồ sơ mới",
            Body = $"{evt.RequesterName} từ {evt.RequesterOrganization} yêu cầu truy cập hồ sơ bệnh án. Mục đích: {evt.Purpose}",
            Type = NotificationType.AccessRequestCreated,
            Priority = NotificationPriority.High,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.RequestId.ToString(),
            ReferenceType = "AccessRequest"
        });
    }
}
