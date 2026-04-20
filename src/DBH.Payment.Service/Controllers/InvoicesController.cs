using DBH.Payment.Service.DTOs;
using DBH.Payment.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Payment.Service.Controllers;

[ApiController]
[Route("api/v1/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost]
    [Authorize(Roles = "Receptionist,Doctor,Admin")]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        var result = await _invoiceService.CreateInvoiceAsync(request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetInvoice), new { invoiceId = result.Data!.InvoiceId }, result);
    }

    [HttpGet("{invoiceId}")]
    public async Task<IActionResult> GetInvoice(Guid invoiceId)
    {
        var result = await _invoiceService.GetInvoiceByIdAsync(invoiceId);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetInvoicesByPatient(Guid patientId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _invoiceService.GetInvoicesByPatientAsync(patientId, page, pageSize);
        return Ok(result);
    }

    [HttpGet("org/{orgId}")]
    [Authorize(Roles = "Receptionist,Admin")]
    public async Task<IActionResult> GetInvoicesByOrg(Guid orgId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _invoiceService.GetInvoicesByOrgAsync(orgId, page, pageSize);
        return Ok(result);
    }

    [HttpPost("{invoiceId}/cancel")]
    [Authorize(Roles = "Receptionist,Admin")]
    public async Task<IActionResult> CancelInvoice(Guid invoiceId)
    {
        var result = await _invoiceService.CancelInvoiceAsync(invoiceId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}
