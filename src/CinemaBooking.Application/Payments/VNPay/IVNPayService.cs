using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Payments.VNPay;

public interface IVNPayService
{
    Task<string> CreatePaymentUrlAsync(
        Payment payment,
        Booking booking,
        string ipAddress,
        CancellationToken cancellationToken = default);

    Task<(bool IsValid, string ResponseCode, string TransactionNo)> ProcessCallbackAsync(
        Dictionary<string, string> vnpayData,
        CancellationToken cancellationToken = default);

    bool VerifySignature(
        Dictionary<string, string> vnpayData,
        string secureHash);
}
