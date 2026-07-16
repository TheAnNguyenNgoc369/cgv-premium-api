using CinemaBooking.Application.Contracts.Payment;
using CinemaBooking.Application.Payments.PayOS;

namespace CinemaBooking.Application.Payments;

public interface IPaymentService
{
    Task<PaymentOperationResult> InitiatePaymentAsync(
        InitiatePaymentRequest request,
        int actorUserId,
        bool isStaff,
        string? frontendOrigin = null,
        string? backendOrigin = null,
        string ipAddress = "127.0.0.1",
        CancellationToken cancellationToken = default);

    Task<PaymentOperationResult> ConfirmCashPaymentAsync(
        ConfirmCashPaymentRequest request,
        int staffUserId,
        CancellationToken cancellationToken = default);

    Task<PayOSWebhookResult> ProcessPayOSWebhookAsync(
        PayOSWebhook webhook,
        CancellationToken cancellationToken = default);

    Task<PaymentOperationResult> SyncPayOSPaymentAsync(
        int bookingId,
        long orderCode,
        int actorUserId,
        bool isStaff,
        CancellationToken cancellationToken = default);

    Task<int> ReconcilePendingPayOSPaymentsAsync(
        int batchSize = 50,
        CancellationToken cancellationToken = default);

    Task<PayOSRedirectResult> HandlePayOSRedirectAsync(
        long orderCode,
        bool isCancel,
        CancellationToken cancellationToken = default);

    Task<PaymentOperationResult> GetPaymentByIdAsync(
        int paymentId,
        int actorUserId,
        bool isStaff,
        CancellationToken cancellationToken = default);

    Task<PaymentOperationResult> GetPaymentByBookingIdAsync(
        int bookingId,
        int actorUserId,
        bool isStaff,
        CancellationToken cancellationToken = default);
}
