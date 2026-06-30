using CinemaBooking.Application.Contracts.Payment;

namespace CinemaBooking.Application.Payments;

public interface IPaymentService
{
    Task<PaymentOperationResult> InitiatePaymentAsync(
        InitiatePaymentRequest request,
        int actorUserId,
        bool isStaff,
        string ipAddress = "127.0.0.1",
        CancellationToken cancellationToken = default);

    Task<PaymentOperationResult> ConfirmCashPaymentAsync(
        ConfirmCashPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<VNPayCallbackResult> ProcessVNPayCallbackAsync(
        Dictionary<string, string> vnpayData,
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
