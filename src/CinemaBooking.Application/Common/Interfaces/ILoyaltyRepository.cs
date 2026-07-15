using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface ILoyaltyRepository
{
    Task<int> GetUserTotalPointsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetUserTotalSpentAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<LoyaltyTier?> GetUserTierAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<List<LoyaltyTier>> GetAllTiersAsync(
        CancellationToken cancellationToken = default);

    Task<LoyaltyTier?> GetTierByIdAsync(
        int tierId,
        CancellationToken cancellationToken = default);

    Task<bool> TierNameExistsAsync(
        string tierName,
        int? excludingTierId = null,
        CancellationToken cancellationToken = default);

    Task<bool> MinPointsExistsAsync(
        int minPoints,
        int? excludingTierId = null,
        CancellationToken cancellationToken = default);

    Task<LoyaltyTier> AddTierAsync(
        LoyaltyTier tier,
        CancellationToken cancellationToken = default);

    Task<LoyaltyTier?> UpdateTierAsync(
        int tierId,
        string tierName,
        int minPoints,
        decimal discountRate,
        int maxRefundPerMonth,
        CancellationToken cancellationToken = default);

    Task<bool> HasAssignedUsersAsync(
        int tierId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteTierAsync(
        int tierId,
        CancellationToken cancellationToken = default);

    Task AddLoyaltyPointAsync(
        LoyaltyPoints loyaltyPoint,
        CancellationToken cancellationToken = default);

    Task<List<LoyaltyPoints>> GetPointHistoryAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task UpdateUserTierAsync(
        int userId,
        int tierID,
        CancellationToken cancellationToken = default);

    Task UpdateUserTotalPointsAsync(
        int userId,
        int totalPoints,
        CancellationToken cancellationToken = default);

    Task<bool> HasPointsForBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default);
}
