using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IUserVoucherRepository
{
    Task<List<UserVoucher>> GetUserVouchersAsync(int userId, CancellationToken cancellationToken);
    Task<UserVoucher?> GetByIdAsync(int userVoucherId, CancellationToken cancellationToken);
    /// <summary>
    /// Concurrency-safe redemption. Locks the user AND voucher rows, re-validates points,
    /// MaxUses (global redemption cap), exchange limit (per-user cap) and voucher validity
    /// INSIDE the transaction, then deducts points, creates the UserVoucher and bumps
    /// Voucher.UsedCount. Returns (false, error) on a failed re-check without mutating state.
    /// </summary>
    Task<(bool Succeeded, string? Error)> RedeemVoucherAsync(
        UserVoucher userVoucher,
        LoyaltyPoints loyaltyPoint,
        int pointsToDeduct,
        int? maxUses,
        int? exchangeLimit,
        AdminActionLog log,
        CancellationToken cancellationToken);

    /// <summary>
    /// Read-only (no lock) lookup of an Available, non-expired UserVoucher owned by the user.
    /// Used at pricing/validation time for fast-fail UX before the booking transaction.
    /// </summary>
    Task<UserVoucher?> GetAvailableOwnedAsync(
        int userId,
        int voucherId,
        DateTime now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads the current user's Available UserVoucher for a voucher with a row lock (UPDLOCK),
    /// so a concurrent booking cannot reserve the same UserVoucher. Must run inside a transaction.
    /// </summary>
    Task<UserVoucher?> GetAvailableForUpdateAsync(
        int userId,
        int voucherId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Transitions a reserved UserVoucher for a booking to Used (sets UsedAt).
    /// Called from the payment-completion transaction.
    /// </summary>
    Task MarkReservedAsUsedByBookingAsync(
        int bookingId,
        DateTime usedAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases a reserved UserVoucher for a booking back to Available (clears BookingID).
    /// Called when a booking expires, is cancelled, or payment fails.
    /// </summary>
    Task ReleaseReservedByBookingAsync(
        int bookingId,
        CancellationToken cancellationToken);
}
