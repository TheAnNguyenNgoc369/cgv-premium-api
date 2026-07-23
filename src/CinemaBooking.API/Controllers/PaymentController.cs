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
            return BadRequest(new { success = false, message = "Invalid request." });

        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        var isStaff = User.IsInRole(Roles.Staff);

        var result = await _paymentService.InitiatePaymentAsync(
            request,
            userId,
            isStaff,
            Request.Headers.Origin.ToString(),
            $"{Request.Scheme}://{Request.Host}",
            cancellationToken: cancellationToken);
        return MapResult(result);
    }

    [HttpPost("cash/confirm")]
    [Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> ConfirmCashPayment(
        [FromBody] ConfirmCashPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid request." });

        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _paymentService.ConfirmCashPaymentAsync(
            request, userId, cancellationToken);

        return MapResult(result);
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

    [HttpPost("payos/sync")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> SyncPayOSPayment(
        [FromQuery] int bookingId,
        [FromQuery] long orderCode,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _paymentService.SyncPayOSPaymentAsync(
            bookingId, orderCode, userId, User.IsInRole(Roles.Staff), cancellationToken);

        return MapResult(result);
    }

    [HttpGet("payos/return")]
    [AllowAnonymous]
    public async Task<IActionResult> PayOSReturn(
        [FromQuery] long orderCode,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.HandlePayOSRedirectAsync(
            orderCode, isCancel: false, cancellationToken);

        return result.Success && result.RedirectUrl is not null
            ? Redirect(result.RedirectUrl)
            : NotFound(new { success = false, message = result.Message });
    }

    [HttpGet("payos/cancel")]
    [AllowAnonymous]
    public async Task<IActionResult> PayOSCancel(
        [FromQuery] long orderCode,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.HandlePayOSRedirectAsync(
            orderCode, isCancel: true, cancellationToken);

        return result.Success && result.RedirectUrl is not null
            ? Redirect(result.RedirectUrl)
            : NotFound(new { success = false, message = result.Message });
    }

    [HttpGet("{id}")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> GetPaymentById(
        int id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _paymentService.GetPaymentByIdAsync(
            id, userId, User.IsInRole(Roles.Staff), cancellationToken);
        return MapResult(result);
    }

    [HttpGet("booking/{bookingId}")]
    [Authorize(Roles = $"{Roles.Customer},{Roles.Staff}")]
    public async Task<IActionResult> GetPaymentByBookingId(
        int bookingId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await _paymentService.GetPaymentByBookingIdAsync(
            bookingId, userId, User.IsInRole(Roles.Staff), cancellationToken);
        return MapResult(result);
    }

    private bool TryGetCurrentUserId(out int userId)
    {
        var userIdValue = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdValue, out userId);
    }

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
