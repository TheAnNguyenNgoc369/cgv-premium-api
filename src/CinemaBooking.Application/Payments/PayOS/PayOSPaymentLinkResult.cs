namespace CinemaBooking.Application.Payments.PayOS;

public sealed record PayOSPaymentLinkResult(
    long OrderCode,
    string PaymentLinkId,
    string CheckoutUrl,
    string QrCode);
