using CinemaBooking.Application.Contracts.Payment;

namespace CinemaBooking.Application.Payments;

public interface IPaymentService
{
    Task<object> InitiatePaymentAsync(
        InitiatePaymentRequest request,
        string ipAddress = "127.0.0.1",
        CancellationToken cancellationToken = default);

    Task<PaymentResponse?> ConfirmCashPaymentAsync(
        ConfirmCashPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<VNPayCallbackResult> ProcessVNPayCallbackAsync(
        Dictionary<string, string> vnpayData,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse?> GetPaymentByIdAsync(
        int paymentId,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse?> GetPaymentByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);
}
