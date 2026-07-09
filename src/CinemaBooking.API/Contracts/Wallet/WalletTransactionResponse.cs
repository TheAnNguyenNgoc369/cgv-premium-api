namespace CinemaBooking.API.Contracts.Wallet;

public sealed record WalletTransactionResponse(
    int TransactionID,
    string TransactionType,
    decimal Amount,
    decimal BalanceAfter,
    string? BookingCode,
    string? Description,
    DateTime CreatedAt
);
