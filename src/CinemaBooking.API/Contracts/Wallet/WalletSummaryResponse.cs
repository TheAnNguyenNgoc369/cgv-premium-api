namespace CinemaBooking.API.Contracts.Wallet;

public sealed record WalletSummaryResponse(
    decimal CurrentBalance,
    decimal TotalRefundReceived,
    decimal TotalSpent,
    int TransactionCount
);
