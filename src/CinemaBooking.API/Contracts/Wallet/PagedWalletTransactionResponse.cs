namespace CinemaBooking.API.Contracts.Wallet;

public sealed record PagedWalletTransactionResponse(
    List<WalletTransactionResponse> Transactions,
    int TotalCount,
    int Page,
    int PageSize
);
