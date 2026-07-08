namespace CinemaBooking.API.Contracts.Users;

public sealed record UserLookupResponse(
    bool Success,
    string Message,
    UserLookupResponse.UserInfo User)
{
    public sealed record UserInfo(
        int UserID,
        string FullName,
        string Email,
        string? Phone,
        string Role,
        string Status,
        string? AvatarURL,
        UserMembershipInfo Membership,
        UserWalletInfo? Wallet,
        object? Cinema);

    public sealed record UserMembershipInfo(
        string CurrentTier,
        int TotalPoints,
        decimal DiscountPercent,
        string? NextTier,
        int PointsToNextTier);

    public sealed record UserWalletInfo(
        int WalletID,
        decimal Balance);
}
