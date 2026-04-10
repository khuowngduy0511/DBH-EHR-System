using DBH.Payment.Service.DbContext;
using DBH.Payment.Service.DTOs;
using DBH.Payment.Service.Models.Entities;
using DBH.Payment.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DBH.Payment.Service.Services;

public class InvoiceService : IInvoiceService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(PaymentDbContext context, ILogger<InvoiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<InvoiceResponse>> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
            return new ApiResponse<InvoiceResponse> { Success = false, Message = "Invoice must have at least one item." };

        var invoice = new Invoice
        {
            PatientId = request.PatientId,
            EncounterId = request.EncounterId,
            OrgId = request.OrgId,
            Notes = request.Notes,
            Status = InvoiceStatus.UNPAID
        };

        foreach (var itemReq in request.Items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                InvoiceId = invoice.InvoiceId,
                EhrId = itemReq.EhrId,
                Description = itemReq.Description,
                Quantity = itemReq.Quantity,
                Amount = itemReq.Amount
            });
        }

        invoice.TotalAmount = invoice.Items.Sum(i => i.Quantity * i.Amount);

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created invoice {InvoiceId} for patient {PatientId}, total: {Total}",
            invoice.InvoiceId, invoice.PatientId, invoice.TotalAmount);

        return new ApiResponse<InvoiceResponse>
        {
            Success = true,
            Message = "Invoice created successfully",
            Data = MapToResponse(invoice)
        };
    }

    public async Task<ApiResponse<InvoiceResponse>> GetInvoiceByIdAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice == null)
            return new ApiResponse<InvoiceResponse> { Success = false, Message = "Invoice not found." };

        return new ApiResponse<InvoiceResponse>
        {
            Success = true,
            Data = MapToResponse(invoice)
        };
    }

    public async Task<PagedResponse<InvoiceResponse>> GetInvoicesByPatientAsync(Guid patientId, int page = 1, int pageSize = 10)
    {
        var query = _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.PatientId == patientId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<InvoiceResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResponse<InvoiceResponse>> GetInvoicesByOrgAsync(Guid orgId, int page = 1, int pageSize = 10)
    {
        var query = _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Where(i => i.OrgId == orgId);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<InvoiceResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ApiResponse<InvoiceResponse>> CancelInvoiceAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice == null)
            return new ApiResponse<InvoiceResponse> { Success = false, Message = "Invoice not found." };

        if (invoice.Status == InvoiceStatus.PAID)
            return new ApiResponse<InvoiceResponse> { Success = false, Message = "Cannot cancel a paid invoice." };

        invoice.Status = InvoiceStatus.CANCELLED;

        // Cancel any pending payments
        foreach (var payment in invoice.Payments.Where(p => p.Status == PaymentStatus.PENDING))
        {
            payment.Status = PaymentStatus.CANCELLED;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Cancelled invoice {InvoiceId}", invoiceId);

        return new ApiResponse<InvoiceResponse>
        {
            Success = true,
            Message = "Invoice cancelled successfully",
            Data = MapToResponse(invoice)
        };
    }

    private static InvoiceResponse MapToResponse(Invoice invoice)
    {
        return new InvoiceResponse
        {
            InvoiceId = invoice.InvoiceId,
            PatientId = invoice.PatientId,
            EncounterId = invoice.EncounterId,
            OrgId = invoice.OrgId,
            TotalAmount = invoice.TotalAmount,
            Status = invoice.Status.ToString(),
            Notes = invoice.Notes,
            PaidAt = invoice.PaidAt,
            CreatedAt = invoice.CreatedAt,
            Items = invoice.Items?.Select(i => new InvoiceItemResponse
            {
                ItemId = i.ItemId,
                EhrId = i.EhrId,
                Description = i.Description,
                Quantity = i.Quantity,
                Amount = i.Amount
            }).ToList() ?? new(),
            Payments = invoice.Payments?.Select(p => new PaymentSummaryResponse
            {
                PaymentId = p.PaymentId,
                Method = p.Method.ToString(),
                Amount = p.Amount,
                Status = p.Status.ToString(),
                PaidAt = p.PaidAt
            }).ToList() ?? new()
        };
    }
}
