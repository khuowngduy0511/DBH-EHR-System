using DBH.Payment.Service.DTOs;

namespace DBH.Payment.Service.Services;

public interface IPaymentProcessingService
{
    Task<ApiResponse<CheckoutResponse>> CreatePayOSCheckoutAsync(Guid invoiceId, CheckoutRequest? request);
    Task<ApiResponse<PaymentResponse>> PayCashAsync(Guid invoiceId, PayCashRequest? request);
    Task<ApiResponse<PaymentResponse>> GetPaymentByIdAsync(Guid paymentId);
    Task<ApiResponse<bool>> HandleWebhookAsync(PayOSWebhookRequest webhookRequest);
    Task<ApiResponse<PaymentResponse>> VerifyPaymentAsync(Guid paymentId);
    Task<ApiResponse<PaymentResponse>> VerifyByOrderCodeAsync(long orderCode);
}
