using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Models.Enums;
using DBH.Notification.Service.Services;
using DBH.Shared.Contracts.Events;
using MassTransit;

namespace DBH.Notification.Service.Consumers;

public class AppointmentEventConsumer :
    IConsumer<AppointmentCreatedEvent>,
    IConsumer<AppointmentConfirmedEvent>,
    IConsumer<AppointmentCancelledEvent>,
    IConsumer<AppointmentCheckedInEvent>,
    IConsumer<EncounterCreatedEvent>,
    IConsumer<EncounterCompletedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<AppointmentEventConsumer> _logger;

    public AppointmentEventConsumer(INotificationService notificationService, ILogger<AppointmentEventConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AppointmentCreatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming AppointmentCreatedEvent for AppointmentId={AppointmentId}", evt.AppointmentId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = $"did:user:{evt.PatientId}",
            RecipientUserId = evt.PatientId,
            Title = "Lịch hẹn mới đã được tạo",
            Body = $"Lịch hẹn vào lúc {evt.ScheduledAt:dd/MM/yyyy HH:mm} đã được đặt thành công.",
            Type = NotificationType.AppointmentCreated,
            Priority = NotificationPriority.Normal,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.AppointmentId.ToString(),
            ReferenceType = "Appointment"
        });
    }

    public async Task Consume(ConsumeContext<AppointmentConfirmedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming AppointmentConfirmedEvent for AppointmentId={AppointmentId}", evt.AppointmentId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = $"did:user:{evt.PatientId}",
            RecipientUserId = evt.PatientId,
            Title = "Lịch hẹn đã được xác nhận",
            Body = $"Lịch hẹn vào lúc {evt.ScheduledAt:dd/MM/yyyy HH:mm} đã được bác sĩ xác nhận.",
            Type = NotificationType.AppointmentReminder,
            Priority = NotificationPriority.High,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.AppointmentId.ToString(),
            ReferenceType = "Appointment"
        });
    }

    public async Task Consume(ConsumeContext<AppointmentCancelledEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming AppointmentCancelledEvent for AppointmentId={AppointmentId}", evt.AppointmentId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = $"did:user:{evt.PatientId}",
            RecipientUserId = evt.PatientId,
            Title = "Lịch hẹn đã bị hủy",
            Body = $"Lịch hẹn của bạn đã bị hủy. Lý do: {evt.Reason}",
            Type = NotificationType.AppointmentReminder,
            Priority = NotificationPriority.High,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.AppointmentId.ToString(),
            ReferenceType = "Appointment"
        });
    }

    public async Task Consume(ConsumeContext<AppointmentCheckedInEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming AppointmentCheckedInEvent for AppointmentId={AppointmentId}", evt.AppointmentId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = $"did:user:{evt.PatientId}",
            RecipientUserId = evt.PatientId,
            Title = "Check-in thành công",
            Body = "Bạn đã check-in thành công. Vui lòng chờ được gọi khám.",
            Type = NotificationType.AppointmentCheckedIn,
            Priority = NotificationPriority.Normal,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.AppointmentId.ToString(),
            ReferenceType = "Appointment"
        });
    }

    public async Task Consume(ConsumeContext<EncounterCreatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming EncounterCreatedEvent for EncounterId={EncounterId}", evt.EncounterId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = $"did:user:{evt.PatientId}",
            RecipientUserId = evt.PatientId,
            Title = "Bắt đầu khám bệnh",
            Body = "Buổi khám bệnh của bạn đã bắt đầu.",
            Type = NotificationType.EncounterCreated,
            Priority = NotificationPriority.Normal,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.EncounterId.ToString(),
            ReferenceType = "Encounter"
        });
    }

    public async Task Consume(ConsumeContext<EncounterCompletedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming EncounterCompletedEvent for EncounterId={EncounterId}", evt.EncounterId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = $"did:user:{evt.PatientId}",
            RecipientUserId = evt.PatientId,
            Title = "Khám bệnh hoàn thành",
            Body = "Buổi khám bệnh của bạn đã hoàn thành. Cảm ơn bạn đã sử dụng dịch vụ.",
            Type = NotificationType.EncounterCompleted,
            Priority = NotificationPriority.Normal,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.EncounterId.ToString(),
            ReferenceType = "Encounter"
        });
    }
}
