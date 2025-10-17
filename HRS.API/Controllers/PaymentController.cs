using HRS.API.Contracts.DTOs;
using HRS.API.Contracts.DTOs.Payment;
using HRS.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace HRS.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("create-session")]
    public async Task<ActionResult> GetAvailability([FromBody] PaymentRequestDto request)
    {
        var result = await _paymentService.CreatePayments(request.OrderId, request.Amount);
        return Ok(ApiResponse<Session>.OkResponse(result));
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequestDto request)
    {
        await _paymentService.VerifyPaymentAsync(request.SecretKey);
        return Ok(ApiResponse<object>.OkResponse(null, "Payment verified successfully"));
    }
}
