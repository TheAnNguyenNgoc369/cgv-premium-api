using CinemaBooking.API.Contracts.Payment;
using CinemaBooking.Application.Payments;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost("initiate")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> InitiatePayment(
        [FromBody] InitiatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var result = await _paymentService.InitiatePaymentAsync(request, ipAddress, cancellationToken);

        return Ok(result);
    }

    [HttpPost("cash/confirm")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> ConfirmCashPayment(
        [FromBody] ConfirmCashPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _paymentService.ConfirmCashPaymentAsync(request, cancellationToken);

        if (result is null)
            return NotFound(new { message = "Không tìm thấy thanh toán" });

        return Ok(result);
    }

    [HttpPost("vnpay/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessVNPayCallback(CancellationToken cancellationToken)
    {
        var vnpayData = Request.Query.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToString());

        var result = await _paymentService.ProcessVNPayCallbackAsync(vnpayData, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPaymentById(
        int id,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(id, cancellationToken);

        if (payment is null)
            return NotFound(new { message = "Không tìm thấy thanh toán" });

        return Ok(payment);
    }

    [HttpGet("booking/{bookingId}")]
    public async Task<IActionResult> GetPaymentByBookingId(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetPaymentByBookingIdAsync(bookingId, cancellationToken);

        if (payment is null)
            return NotFound(new { message = "Không tìm thấy thanh toán cho booking này" });

        return Ok(payment);
    }
}
