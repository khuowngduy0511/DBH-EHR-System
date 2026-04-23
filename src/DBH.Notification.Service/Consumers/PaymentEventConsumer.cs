using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Models.Enums;
using DBH.Notification.Service.Services;
using DBH.Shared.Contracts.Events;
using MassTransit;

namespace DBH.Notification.Service.Consumers;

public class PaymentEventConsumer : IConsumer<InvoicePaidEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<PaymentEventConsumer> _logger;

    public PaymentEventConsumer(INotificationService notificationService, ILogger<PaymentEventConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<InvoicePaidEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Consuming InvoicePaidEvent for InvoiceId={InvoiceId}", evt.InvoiceId);
        await _notificationService.SendNotificationAsync(new SendNotificationRequest
        {
            RecipientDid = $"did:user:{evt.PatientId}",
            RecipientUserId = evt.PatientId,
            Title = "Thanh toán thành công",
            Body = $"Hóa đơn {evt.InvoiceId} đã được thanh toán thành công. Tổng tiền: {evt.TotalAmount:N0} VNĐ.",
            Type = NotificationType.System,
            Priority = NotificationPriority.Normal,
            Channel = NotificationChannel.InApp,
            ReferenceId = evt.InvoiceId.ToString(),
            ReferenceType = "Invoice"
        });
    }
}
