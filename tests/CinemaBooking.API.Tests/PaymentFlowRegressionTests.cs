using System.Security.Claims;
using CinemaBooking.API.Controllers;
using CinemaBooking.Application.Contracts.Payment;
using CinemaBooking.Application.Payments;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Tests;

public sealed class PaymentFlowRegressionTests
{
    [Fact]
    public async Task InitiatePayment_ExistingPayment_ReturnsConflictInsteadOfThrowing()
    {
        var controller = CreateController(new ExistingPaymentService());

        var result = await controller.InitiatePayment(
            new InitiatePaymentRequest { BookingId = 9, PaymentMethod = "CASH" },
            CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task InitiatePayment_OtherCustomersBooking_ReturnsForbidden()
    {
        var controller = CreateController(new ExistingPaymentService());

        var result = await controller.InitiatePayment(
            new InitiatePaymentRequest { BookingId = 10, PaymentMethod = "VNPAY" },
            CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task InitiatePayment_OwnBooking_ReturnsOk()
    {
        var controller = CreateController(new ExistingPaymentService());

        var result = await controller.InitiatePayment(
            new InitiatePaymentRequest { BookingId = 1, PaymentMethod = "VNPAY" },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    private static PaymentController CreateController(IPaymentService service)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("userId", "1"),
            new Claim(ClaimTypes.Role, Roles.Customer)
        ], "Test");

        return new PaymentController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private sealed class ExistingPaymentService : IPaymentService
    {
        public Task<PaymentOperationResult> InitiatePaymentAsync(
            InitiatePaymentRequest request,
            int actorUserId,
            bool isStaff,
            string ipAddress = "127.0.0.1",
            CancellationToken cancellationToken = default) => Task.FromResult(
                request.BookingId switch
                {
                    10 => PaymentOperationResult.Failure(
                        PaymentErrorType.Forbidden,
                        "You cannot access another customer's booking."),
                    1 => PaymentOperationResult.Success(new { paymentId = 1 }),
                    _ => PaymentOperationResult.Failure(
                        PaymentErrorType.Conflict,
                        "Payment already exists for this booking.")
                });

        public Task<PaymentOperationResult> ConfirmCashPaymentAsync(
            ConfirmCashPaymentRequest request,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<VNPayCallbackResult> ProcessVNPayCallbackAsync(
            Dictionary<string, string> vnpayData,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PaymentOperationResult> GetPaymentByIdAsync(
            int paymentId,
            int actorUserId,
            bool isStaff,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PaymentOperationResult> GetPaymentByBookingIdAsync(
            int bookingId,
            int actorUserId,
            bool isStaff,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
