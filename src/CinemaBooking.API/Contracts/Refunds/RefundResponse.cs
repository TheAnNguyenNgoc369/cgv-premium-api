namespace CinemaBooking.API.Contracts.Refunds;

public sealed record RefundResponse(
    bool Success,
    decimal RefundAmount,
    decimal WalletBalance,
    string Status
);
