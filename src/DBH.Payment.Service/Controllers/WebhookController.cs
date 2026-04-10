using DBH.Payment.Service.DTOs;
using DBH.Payment.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Payment.Service.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class WebhookController : ControllerBase
{
    private readonly IPaymentProcessingService _paymentService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IPaymentProcessingService paymentService, ILogger<WebhookController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> PayOSWebhook([FromBody] PayOSWebhookRequest request)
    {
        _logger.LogInformation("Received PayOS webhook: code={Code}, orderCode={OrderCode}",
            request.Code, request.Data?.OrderCode);

        var result = await _paymentService.HandleWebhookAsync(request);

        // Always return 200 to PayOS to acknowledge receipt
        return Ok(result);
    }
}
