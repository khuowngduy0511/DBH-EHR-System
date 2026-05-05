using DBH.Payment.Service.DTOs;
using DBH.Payment.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Payment.Service.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentProcessingService _paymentService;

    public PaymentsController(IPaymentProcessingService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("invoices/{invoiceId}/checkout")]
    public async Task<IActionResult> Checkout(Guid invoiceId, [FromBody] CheckoutRequest? request)
    {
        var result = await _paymentService.CreatePayOSCheckoutAsync(invoiceId, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("invoices/{invoiceId}/pay-cash")]
    [Authorize(Roles = "Receptionist,Admin")]
    public async Task<IActionResult> PayCash(Guid invoiceId, [FromBody] PayCashRequest? request)
    {
        var result = await _paymentService.PayCashAsync(invoiceId, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpGet("payments/{paymentId}")]
    public async Task<IActionResult> GetPayment(Guid paymentId)
    {
        var result = await _paymentService.GetPaymentByIdAsync(paymentId);
        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpPost("payments/{paymentId}/verify")]
    public async Task<IActionResult> VerifyPayment(Guid paymentId)
    {
        var result = await _paymentService.VerifyPaymentAsync(paymentId);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("payments/verify-by-order-code/{orderCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyByOrderCode(long orderCode)
    {
        var result = await _paymentService.VerifyByOrderCodeAsync(orderCode);
        return Ok(result);
    }
}
