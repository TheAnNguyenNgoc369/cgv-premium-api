namespace CinemaBooking.API.Contracts.Wallet;

public sealed record WalletTransactionDetailResponse(
    int TransactionID,
    string TransactionType,
    decimal Amount,
    decimal BalanceAfter,
    string? BookingCode,
    int? RefundID,
    string? Description,
    DateTime CreatedAt
);
