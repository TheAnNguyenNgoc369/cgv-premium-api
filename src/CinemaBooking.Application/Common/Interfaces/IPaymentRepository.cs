using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IPaymentRepository
{
    Task<Payment> CreatePaymentAsync(
        Payment payment,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetPaymentByIdAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetPaymentByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task UpdatePaymentStatusAsync(
        int paymentId,
        string status,
        DateTime? paidAt,
        string? transactionCode = null,
        CancellationToken cancellationToken = default);

    Task<bool> TryCompletePendingPaymentAsync(
        int paymentId,
        DateTime paidAt,
        string transactionCode,
        CancellationToken cancellationToken = default);

    Task<bool> TryCancelPendingPaymentAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<PaymentSession> CreatePaymentSessionAsync(
        PaymentSession session,
        CancellationToken cancellationToken = default);

    Task<PaymentSession?> GetPaymentSessionByIdAsync(
        int sessionId,
        CancellationToken cancellationToken = default);

    Task<PaymentSession?> GetPaymentSessionByOrderNoAsync(
        string orderNo,
        CancellationToken cancellationToken = default);

    Task<PaymentSession?> GetLatestPaymentSessionAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task UpdatePaymentSessionStatusAsync(
        int sessionId,
        string status,
        CancellationToken cancellationToken = default);

    Task UpdatePaymentSessionsForPaymentAsync(
        int paymentId,
        string status,
        CancellationToken cancellationToken = default);

    Task ResetPaymentForRetryAsync(
        int paymentId,
        string paymentMethod,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task UpdatePaymentForRefundAsync(
        int paymentId,
        decimal refundAmount,
        string refundReason,
        int refundedBy,
        CancellationToken cancellationToken = default);
}
