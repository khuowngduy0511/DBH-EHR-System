using DBH.Payment.Service.DTOs;

namespace DBH.Payment.Service.Services;

public interface IInvoiceService
{
    Task<ApiResponse<InvoiceResponse>> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task<ApiResponse<InvoiceResponse>> GetInvoiceByIdAsync(Guid invoiceId);
    Task<PagedResponse<InvoiceResponse>> GetInvoicesByPatientAsync(Guid patientId, int page = 1, int pageSize = 10);
    Task<PagedResponse<InvoiceResponse>> GetInvoicesByOrgAsync(Guid orgId, int page = 1, int pageSize = 10);
    Task<ApiResponse<InvoiceResponse>> CancelInvoiceAsync(Guid invoiceId);
}
