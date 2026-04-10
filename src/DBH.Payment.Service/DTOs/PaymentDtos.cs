using System.ComponentModel.DataAnnotations;
using DBH.Payment.Service.Models.Enums;

namespace DBH.Payment.Service.DTOs;

// =============================================================================
// Response Wrappers
// =============================================================================

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public class PagedResponse<T>
{
    public bool Success { get; set; } = true;
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// =============================================================================
// Invoice DTOs
// =============================================================================

public class CreateInvoiceRequest
{
    [Required]
    public Guid PatientId { get; set; }

    public Guid? EncounterId { get; set; }

    [Required]
    public Guid OrgId { get; set; }

    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateInvoiceItemRequest> Items { get; set; } = new();
}

public class CreateInvoiceItemRequest
{
    public Guid? EhrId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }
}

public class InvoiceResponse
{
    public Guid InvoiceId { get; set; }
    public Guid PatientId { get; set; }
    public Guid? EncounterId { get; set; }
    public Guid OrgId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<InvoiceItemResponse> Items { get; set; } = new();
    public List<PaymentSummaryResponse> Payments { get; set; } = new();
}

public class InvoiceItemResponse
{
    public Guid ItemId { get; set; }
    public Guid? EhrId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Amount { get; set; }
}

// =============================================================================
// Payment DTOs
// =============================================================================

public class CheckoutRequest
{
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class CheckoutResponse
{
    public Guid PaymentId { get; set; }
    public long OrderCode { get; set; }
    public string? CheckoutUrl { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PayCashRequest
{
    public string? TransactionRef { get; set; }
}

public class PaymentResponse
{
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? TransactionRef { get; set; }
    public long OrderCode { get; set; }
    public string? PaymentLinkId { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentSummaryResponse
{
    public Guid PaymentId { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}

// =============================================================================
// Webhook DTOs
// =============================================================================

public class PayOSWebhookRequest
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public bool Success { get; set; }
    public PayOSWebhookData? Data { get; set; }
    public string Signature { get; set; } = string.Empty;
}

public class PayOSWebhookData
{
    public long OrderCode { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string TransactionDateTime { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string PaymentLinkId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public string CounterAccountBankId { get; set; } = string.Empty;
    public string CounterAccountBankName { get; set; } = string.Empty;
    public string CounterAccountName { get; set; } = string.Empty;
    public string CounterAccountNumber { get; set; } = string.Empty;
    public string VirtualAccountName { get; set; } = string.Empty;
    public string VirtualAccountNumber { get; set; } = string.Empty;
}

// =============================================================================
// Internal DTOs (for calling Organization Service)
// =============================================================================

public class OrgPaymentKeysResponse
{
    public bool Success { get; set; }
    public OrgPaymentKeysData? Data { get; set; }
}

public class OrgPaymentKeysData
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
}
