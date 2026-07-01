using CinemaBooking.Application.Contracts.Payment;
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
        var userId = GetCurrentUserId();
        var isStaff = User.IsInRole(Roles.Staff);

        var result = await _paymentService.InitiatePaymentAsync(
            request, userId, isStaff, ipAddress, cancellationToken);
        return MapResult(result);
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

        return MapResult(result);
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

    [HttpPost("payos/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessPayOSWebhook(
        [FromBody] CinemaBooking.API.Contracts.Payment.PayOSWebhookRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid PayOS webhook payload." });

        var result = await _paymentService.ProcessPayOSWebhookAsync(
            request.ToApplicationModel(), cancellationToken);

        return result.Success
            ? Ok(result)
            : BadRequest(new { success = false, message = result.Message });
    }

    [HttpGet("{id}")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> GetPaymentById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPaymentByIdAsync(
            id, GetCurrentUserId(), User.IsInRole(Roles.Staff), cancellationToken);
        return MapResult(result);
    }

    [HttpGet("booking/{bookingId}")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> GetPaymentByBookingId(
        int bookingId,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPaymentByBookingIdAsync(
            bookingId, GetCurrentUserId(), User.IsInRole(Roles.Staff), cancellationToken);
        return MapResult(result);
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirst("userId")!.Value);

    private IActionResult MapResult(PaymentOperationResult result)
    {
        if (result.Succeeded)
            return Ok(result.Value);

        var body = new { success = false, message = result.ErrorMessage };
        return result.ErrorType switch
        {
            PaymentErrorType.NotFound => NotFound(body),
            PaymentErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, body),
            PaymentErrorType.Conflict => Conflict(body),
            PaymentErrorType.Gateway => StatusCode(StatusCodes.Status502BadGateway, body),
            _ => BadRequest(body)
        };
    }
}
