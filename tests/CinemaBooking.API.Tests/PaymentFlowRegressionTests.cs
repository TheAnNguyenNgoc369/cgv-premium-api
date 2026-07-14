using System.Security.Claims;
using CinemaBooking.API.Controllers;
using CinemaBooking.Application.Contracts.Payment;
using CinemaBooking.Application.Payments;
using CinemaBooking.Application.Payments.PayOS;
using CinemaBooking.Application.Common.Enums;
using System.Text.Json;
using CinemaBooking.Shared.Constants;
using CinemaBooking.Infrastructure.Payments.PayOS;
using CinemaBooking.Application.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CinemaBooking.API.Tests;

public sealed class PaymentFlowRegressionTests
{
    [Theory]
    [InlineData("https://frontend.example/payment-result",
        "https://frontend.example/payment-result?bookingId=42&orderCode=123456")]
    [InlineData("https://frontend.example/payment-result?source=payos",
        "https://frontend.example/payment-result?source=payos&bookingId=42&orderCode=123456")]
    public void PayOSRedirectUrl_IncludesBookingAndOrderIdentifiers(
        string baseUrl,
        string expected)
    {
        var result = PayOSService.BuildRedirectUrl(baseUrl, 42, 123456);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", "payment/result",
        "https://api.example/payment/result")]
    [InlineData("https://api.example/api/payments/payos/return", "api/payments/payos/return",
        "https://api.example/api/payments/payos/return")]
    [InlineData("", "payment/cancel",
        "https://api.example/payment/cancel")]
    [InlineData("https://api.example/api/payments/payos/cancel", "api/payments/payos/cancel",
        "https://api.example/api/payments/payos/cancel")]
    public void PayOSRedirectUrl_UsesBackendRoutes(
        string configuredUrl,
        string backendPath,
        string expected)
    {
        var service = new PayOSService(
            Options.Create(new PayOSSettings()),
            Options.Create(new FrontendSettings { BaseUrl = "https://api.example" }));

        var result = service.ResolveRedirectUrl(configuredUrl, backendPath);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void PayOSRedirectUrl_UsesRuntimeBackendOriginWhenConfigIsEmpty()
    {
        var service = new PayOSService(
            Options.Create(new PayOSSettings()),
            Options.Create(new FrontendSettings { BaseUrl = "https://frontend.example" }));

        var result = service.ResolveRedirectUrl(
            configuredUrl: "",
            frontendPath: "payment/result",
            backendOrigin: "https://api.example");

        Assert.Equal("https://api.example/payment/result", result);
    }

    [Fact]
    public void PayOSRedirectUrl_FallsBackToFrontendWhenRuntimeOriginIsLocalhost()
    {
        var service = new PayOSService(
            Options.Create(new PayOSSettings()),
            Options.Create(new FrontendSettings { BaseUrl = "https://frontend.example" }));

        var result = service.ResolveRedirectUrl(
            configuredUrl: "",
            frontendPath: "payment/result",
            backendOrigin: "http://localhost:5000");

        Assert.Equal("https://frontend.example/payment/result", result);
    }

    [Fact]
    public void PaymentMethodMapping_AcceptsPayOS()
    {
        var result = EnumValueMapper.Validate(
            "PAYOS", "PaymentMethod", DatabaseEnumMappings.PaymentMethods);

        Assert.True(result.Succeeded);
        Assert.Equal("payos", result.DatabaseValue);
    }

    [Fact]
    public void PayOSWebhookRequest_DeserializesBothDescFields()
    {
        const string json = """
            {
              "code": "00",
              "desc": "success",
              "success": true,
              "data": {
                "orderCode": 123,
                "amount": 100000,
                "desc": "Thanh cong"
              },
              "signature": "signature"
            }
            """;

        var request = JsonSerializer.Deserialize<CinemaBooking.API.Contracts.Payment.PayOSWebhookRequest>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.Equal("success", request.Description);
        Assert.Equal("Thanh cong", request.Data.DescriptionDetail);
    }

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

    [Fact]
    public async Task InitiatePayment_PayOS_ReturnsRootCheckoutUrlForFrontendCompatibility()
    {
        var controller = CreateController(new ExistingPaymentService());

        var result = await controller.InitiatePayment(
            new InitiatePaymentRequest { BookingId = 2, PaymentMethod = "PAYOS" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<InitiatePaymentResponse>(ok.Value);
        var payment = Assert.IsType<PayOSPaymentResponse>(response.Payment);
        Assert.Equal(payment.PaymentId, response.PaymentId);
        Assert.Equal(payment.CheckoutUrl, response.CheckoutUrl);
    }

    [Fact]
    public async Task ProcessPayOSWebhook_ValidWebhook_ReturnsOk()
    {
        var controller = CreateController(new ExistingPaymentService());

        var result = await controller.ProcessPayOSWebhook(
            CreatePayOSWebhookRequest("valid"), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ProcessPayOSWebhook_InvalidWebhook_ReturnsBadRequest()
    {
        var controller = CreateController(new ExistingPaymentService());

        var result = await controller.ProcessPayOSWebhook(
            CreatePayOSWebhookRequest("invalid"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static CinemaBooking.API.Contracts.Payment.PayOSWebhookRequest CreatePayOSWebhookRequest(
        string signature) => new()
        {
            Code = "00",
            Description = "success",
            Success = true,
            Signature = signature,
            Data = new CinemaBooking.API.Contracts.Payment.PayOSWebhookDataRequest
            {
                OrderCode = 1,
                Amount = 100000,
                Code = "00"
            }
        };

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
            string? frontendOrigin = null,
            string? backendOrigin = null,
            string ipAddress = "127.0.0.1",
            CancellationToken cancellationToken = default) => Task.FromResult(
                request.BookingId switch
                {
                    10 => PaymentOperationResult.Failure(
                        PaymentErrorType.Forbidden,
                        "You cannot access another customer's booking."),
                    1 => PaymentOperationResult.Success(new { paymentId = 1 }),
                    2 => PaymentOperationResult.Success(new InitiatePaymentResponse(
                        true,
                        new PayOSPaymentResponse(
                            true,
                            22,
                            2,
                            "payos",
                            100000,
                            "pending",
                            "https://pay.payos.vn/checkout/abc",
                            "qr",
                            "link-id",
                            123456,
                            7,
                            DateTime.UtcNow.AddMinutes(15)),
                        new PaymentBookingResponse(
                            2,
                            "BK0002",
                            100000,
                            0,
                            100000,
                            "pending",
                            DateTime.UtcNow),
                        22,
                        2,
                        "payos",
                        100000,
                        "pending",
                        "https://pay.payos.vn/checkout/abc",
                        "qr",
                        "link-id",
                        123456,
                        7,
                        DateTime.UtcNow.AddMinutes(15))),
                    _ => PaymentOperationResult.Failure(
                        PaymentErrorType.Conflict,
                        "Payment already exists for this booking.")
                });

        public Task<PaymentOperationResult> ConfirmCashPaymentAsync(
            ConfirmCashPaymentRequest request,
            int staffUserId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PayOSWebhookResult> ProcessPayOSWebhookAsync(
            PayOSWebhook webhook,
            CancellationToken cancellationToken = default) => Task.FromResult(
                webhook.Signature == "valid"
                    ? new PayOSWebhookResult(true, "Payment completed successfully.", 1, 1, "success", "paid")
                    : new PayOSWebhookResult(false, "Invalid signature from PayOS."));

        public Task<PaymentOperationResult> SyncPayOSPaymentAsync(
            int bookingId,
            long orderCode,
            int actorUserId,
            bool isStaff,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int> ReconcilePendingPayOSPaymentsAsync(
            int batchSize = 50,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<PayOSRedirectResult> HandlePayOSRedirectAsync(
            long orderCode,
            bool isCancel,
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
