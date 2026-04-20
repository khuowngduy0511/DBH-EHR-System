using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DBH.Payment.Service.DbContext;
using DBH.Payment.Service.DTOs;
using DBH.Payment.Service.Models.Enums;
using DBH.Shared.Contracts;
using DBH.Shared.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace DBH.Payment.Service.Services;

public class PaymentProcessingService : IPaymentProcessingService
{
    private readonly PaymentDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaymentProcessingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMessagePublisher? _messagePublisher;

    public PaymentProcessingService(
        PaymentDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<PaymentProcessingService> logger,
        IConfiguration configuration,
        IMessagePublisher? messagePublisher = null)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
        _messagePublisher = messagePublisher;
    }

    public async Task<ApiResponse<CheckoutResponse>> CreatePayOSCheckoutAsync(Guid invoiceId, CheckoutRequest? request)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice == null)
            return new ApiResponse<CheckoutResponse> { Success = false, Message = "Invoice not found." };

        if (invoice.Status != InvoiceStatus.UNPAID)
            return new ApiResponse<CheckoutResponse> { Success = false, Message = $"Invoice is {invoice.Status}, cannot checkout." };

        // Check for existing PENDING payment to prevent duplicate charges
        var existingPending = await _context.Payments
            .FirstOrDefaultAsync(p => p.InvoiceId == invoiceId && p.Status == PaymentStatus.PENDING);

        if (existingPending != null)
        {
            _logger.LogWarning("Duplicate checkout attempt for invoice {InvoiceId}, existing payment {PaymentId}", invoiceId, existingPending.PaymentId);
            return new ApiResponse<CheckoutResponse>
            {
                Success = true,
                Message = "A pending payment already exists for this invoice.",
                Data = new CheckoutResponse
                {
                    PaymentId = existingPending.PaymentId,
                    OrderCode = existingPending.OrderCode,
                    CheckoutUrl = existingPending.CheckoutUrl,
                    Status = existingPending.Status.ToString()
                }
            };
        }

        // Get PayOS keys from Organization Service
        var keys = await GetPaymentKeysAsync(invoice.OrgId);
        if (keys == null)
            return new ApiResponse<CheckoutResponse> { Success = false, Message = "Organization has no active payment config." };

        // Generate unique order code (PayOS requires long type)
        var orderCode = GenerateOrderCode();

        var payOSClient = new PayOSClient(keys.ClientId, keys.ApiKey, keys.ChecksumKey);

        var description = $"Thanh toan hoa don #{invoice.InvoiceId.ToString()[..8]}";
        if (description.Length > 25)
            description = description[..25];

        var paymentRequest = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = (int)Math.Ceiling(invoice.TotalAmount),
            Description = description,
            CancelUrl = request?.CancelUrl ?? _configuration["PayOS:DefaultCancelUrl"] ?? "https://localhost:3000/payment/cancel",
            ReturnUrl = request?.ReturnUrl ?? _configuration["PayOS:DefaultReturnUrl"] ?? "https://localhost:3000/payment/success"
        };

        dynamic paymentLinkResponse;
        try
        {
            paymentLinkResponse = await payOSClient.PaymentRequests.CreateAsync(paymentRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PayOS payment link for invoice {InvoiceId}. Error: {Message}", invoiceId, ex.Message);
            return new ApiResponse<CheckoutResponse> { Success = false, Message = $"Failed to create payment link: {ex.Message}" };
        }

        string? checkoutUrl = paymentLinkResponse?.CheckoutUrl?.ToString();
        string? paymentLinkId = paymentLinkResponse?.PaymentLinkId?.ToString();

        var payment = new Models.Entities.Payment
        {
            InvoiceId = invoiceId,
            Method = PaymentMethod.PAYOS_VIETQR,
            Amount = invoice.TotalAmount,
            Status = PaymentStatus.PENDING,
            OrderCode = orderCode,
            PaymentLinkId = paymentLinkId,
            CheckoutUrl = checkoutUrl
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created PayOS checkout for invoice {InvoiceId}, orderCode: {OrderCode}", invoiceId, orderCode);

        return new ApiResponse<CheckoutResponse>
        {
            Success = true,
            Message = "Checkout link created successfully",
            Data = new CheckoutResponse
            {
                PaymentId = payment.PaymentId,
                OrderCode = orderCode,
                CheckoutUrl = checkoutUrl,
                Status = payment.Status.ToString()
            }
        };
    }

    public async Task<ApiResponse<PaymentResponse>> PayCashAsync(Guid invoiceId, PayCashRequest? request)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice == null)
            return new ApiResponse<PaymentResponse> { Success = false, Message = "Invoice not found." };

        if (invoice.Status != InvoiceStatus.UNPAID)
            return new ApiResponse<PaymentResponse> { Success = false, Message = $"Invoice is {invoice.Status}, cannot pay." };

        var payment = new Models.Entities.Payment
        {
            InvoiceId = invoiceId,
            Method = PaymentMethod.CASH,
            Amount = invoice.TotalAmount,
            Status = PaymentStatus.PAID,
            OrderCode = GenerateOrderCode(),
            TransactionRef = request?.TransactionRef,
            PaidAt = VietnamTimeHelper.Now
        };

        invoice.Status = InvoiceStatus.PAID;
        invoice.PaidAt = VietnamTimeHelper.Now;

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cash payment recorded for invoice {InvoiceId}", invoiceId);

        await PublishInvoicePaidEventAsync(invoice);

        return new ApiResponse<PaymentResponse>
        {
            Success = true,
            Message = "Cash payment recorded successfully",
            Data = MapToResponse(payment)
        };
    }

    public async Task<ApiResponse<PaymentResponse>> GetPaymentByIdAsync(Guid paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null)
            return new ApiResponse<PaymentResponse> { Success = false, Message = "Payment not found." };

        return new ApiResponse<PaymentResponse>
        {
            Success = true,
            Data = MapToResponse(payment)
        };
    }

    public async Task<ApiResponse<bool>> HandleWebhookAsync(PayOSWebhookRequest webhookRequest)
    {
        if (webhookRequest.Data == null)
            return new ApiResponse<bool> { Success = false, Message = "Invalid webhook data." };

        var orderCode = webhookRequest.Data.OrderCode;

        var payment = await _context.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.OrderCode == orderCode);

        if (payment == null)
        {
            _logger.LogWarning("Webhook received for unknown orderCode: {OrderCode}", orderCode);
            return new ApiResponse<bool> { Success = false, Message = "Payment not found for this order code." };
        }

        if (payment.Status == PaymentStatus.PAID)
        {
            _logger.LogInformation("Webhook received for already paid orderCode: {OrderCode}", orderCode);
            return new ApiResponse<bool> { Success = true, Data = true, Message = "Already processed." };
        }

        // Verify webhook signature (MANDATORY — reject if keys unavailable)
        var invoice = payment.Invoice;
        var keys = await GetPaymentKeysAsync(invoice.OrgId);
        if (keys == null)
        {
            _logger.LogError("Cannot verify webhook: payment keys unavailable for org {OrgId}, orderCode: {OrderCode}", invoice.OrgId, orderCode);
            return new ApiResponse<bool> { Success = false, Message = "Cannot verify webhook signature: payment configuration unavailable." };
        }

        var payOSClient = new PayOSClient(keys.ClientId, keys.ApiKey, keys.ChecksumKey);
        try
        {
            var webhook = new Webhook
            {
                Code = webhookRequest.Code,
                Description = webhookRequest.Desc,
                Success = webhookRequest.Success,
                Data = new WebhookData
                {
                    OrderCode = webhookRequest.Data.OrderCode,
                    Amount = (int)Math.Ceiling(webhookRequest.Data.Amount),
                    Description = webhookRequest.Data.Description,
                    AccountNumber = webhookRequest.Data.AccountNumber,
                    Reference = webhookRequest.Data.Reference,
                    TransactionDateTime = webhookRequest.Data.TransactionDateTime,
                    Currency = webhookRequest.Data.Currency,
                    PaymentLinkId = webhookRequest.Data.PaymentLinkId,
                    Code = webhookRequest.Data.Code,
                    Description2 = webhookRequest.Data.Desc,
                    CounterAccountBankId = webhookRequest.Data.CounterAccountBankId,
                    CounterAccountBankName = webhookRequest.Data.CounterAccountBankName,
                    CounterAccountName = webhookRequest.Data.CounterAccountName,
                    CounterAccountNumber = webhookRequest.Data.CounterAccountNumber,
                    VirtualAccountName = webhookRequest.Data.VirtualAccountName,
                    VirtualAccountNumber = webhookRequest.Data.VirtualAccountNumber
                },
                Signature = webhookRequest.Signature
            };
            await payOSClient.Webhooks.VerifyAsync(webhook);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webhook signature verification failed for orderCode: {OrderCode}", orderCode);
            return new ApiResponse<bool> { Success = false, Message = "Invalid webhook signature." };
        }

        if (webhookRequest.Code == "00" && webhookRequest.Success)
        {
            payment.Status = PaymentStatus.PAID;
            payment.PaidAt = VietnamTimeHelper.Now;
            payment.TransactionRef = webhookRequest.Data.Reference;

            invoice.Status = InvoiceStatus.PAID;
            invoice.PaidAt = VietnamTimeHelper.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment confirmed via webhook for orderCode: {OrderCode}, invoiceId: {InvoiceId}",
                orderCode, invoice.InvoiceId);

            await PublishInvoicePaidEventAsync(invoice);
        }
        else
        {
            payment.Status = PaymentStatus.CANCELLED;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Payment cancelled via webhook for orderCode: {OrderCode}", orderCode);
        }

        return new ApiResponse<bool> { Success = true, Data = true };
    }

    // =========================================================================
    // Verify Payment (poll PayOS for real status)
    // =========================================================================

    public async Task<ApiResponse<PaymentResponse>> VerifyPaymentAsync(Guid paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null)
            return new ApiResponse<PaymentResponse> { Success = false, Message = "Payment not found." };

        if (payment.Status == PaymentStatus.PAID)
            return new ApiResponse<PaymentResponse> { Success = true, Message = "Already paid.", Data = MapToResponse(payment) };

        if (payment.Method != PaymentMethod.PAYOS_VIETQR)
            return new ApiResponse<PaymentResponse> { Success = false, Message = "Only PayOS payments can be verified." };

        var keys = await GetPaymentKeysAsync(payment.Invoice.OrgId);
        if (keys == null)
            return new ApiResponse<PaymentResponse> { Success = false, Message = "Organization payment config not found." };

        var payOSClient = new PayOSClient(keys.ClientId, keys.ApiKey, keys.ChecksumKey);

        try
        {
            var paymentInfo = await payOSClient.PaymentRequests.GetAsync(payment.OrderCode);

            string status = paymentInfo.Status.ToString();
            _logger.LogInformation("PayOS status for orderCode {OrderCode}: {Status}", payment.OrderCode, status);

            if (string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = PaymentStatus.PAID;
                payment.PaidAt = VietnamTimeHelper.Now;

                var invoice = payment.Invoice;
                invoice.Status = InvoiceStatus.PAID;
                invoice.PaidAt = VietnamTimeHelper.Now;

                await _context.SaveChangesAsync();
                await PublishInvoicePaidEventAsync(invoice);

                _logger.LogInformation("Payment {PaymentId} verified as PAID via PayOS API", paymentId);
            }
            else if (string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(status, "EXPIRED", StringComparison.OrdinalIgnoreCase))
            {
                payment.Status = PaymentStatus.CANCELLED;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify payment {PaymentId} with PayOS", paymentId);
            return new ApiResponse<PaymentResponse> { Success = false, Message = "Failed to verify with PayOS: " + ex.Message };
        }

        return new ApiResponse<PaymentResponse> { Success = true, Data = MapToResponse(payment) };
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private async Task<OrgPaymentKeysData?> GetPaymentKeysAsync(Guid orgId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OrganizationService");

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"/api/v1/internal/organizations/{orgId}/payment-keys");

            var internalKey = _configuration["InternalApi:SecretKey"];
            if (!string.IsNullOrEmpty(internalKey))
                request.Headers.Add("X-Internal-Api-Key", internalKey);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OrgPaymentKeysResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Success == true ? result.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get payment keys for org {OrgId}", orgId);
            return null;
        }
    }

    private async Task PublishInvoicePaidEventAsync(Models.Entities.Invoice invoice)
    {
        if (_messagePublisher == null) return;

        try
        {
            await _messagePublisher.PublishAsync(new DBH.Shared.Contracts.Events.InvoicePaidEvent
            {
                InvoiceId = invoice.InvoiceId,
                PatientId = invoice.PatientId,
                OrgId = invoice.OrgId,
                EncounterId = invoice.EncounterId,
                TotalAmount = invoice.TotalAmount,
                PaidAt = invoice.PaidAt ?? VietnamTimeHelper.Now
            });

            _logger.LogInformation("Published InvoicePaidEvent for invoice {InvoiceId}", invoice.InvoiceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish InvoicePaidEvent for invoice {InvoiceId}", invoice.InvoiceId);
        }
    }

    private static long GenerateOrderCode()
    {
        // Generate a unique order code using timestamp + random
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10000000000L;
        var random = RandomNumberGenerator.GetInt32(100, 999);
        return timestamp * 1000 + random;
    }

    private static PaymentResponse MapToResponse(Models.Entities.Payment payment)
    {
        return new PaymentResponse
        {
            PaymentId = payment.PaymentId,
            InvoiceId = payment.InvoiceId,
            Method = payment.Method.ToString(),
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            TransactionRef = payment.TransactionRef,
            OrderCode = payment.OrderCode,
            PaymentLinkId = payment.PaymentLinkId,
            CheckoutUrl = payment.CheckoutUrl,
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt
        };
    }
}
