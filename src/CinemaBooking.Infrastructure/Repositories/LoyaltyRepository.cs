using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class LoyaltyRepository : ILoyaltyRepository
{
    private readonly CinemaBookingDbContext _db;

    public LoyaltyRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetUserTotalPointsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);

        return user?.TotalPoints ?? 0;
    }

    public async Task<decimal> GetUserTotalSpentAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var totalSpent = await _db.Bookings
            .Where(b => b.UserID == userId
                && (b.Status == BookingStatus.Paid || b.Status == BookingStatus.Used))
            .SumAsync(b => b.FinalAmount, cancellationToken);

        return totalSpent;
    }

    public async Task<LoyaltyTier?> GetUserTierAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.LoyaltyTier)
            .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);

        return user?.LoyaltyTier;
    }

    public async Task<List<LoyaltyTier>> GetAllTiersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.LoyaltyTiers
            .OrderBy(t => t.MinPoints)
            .ToListAsync(cancellationToken);
    }

    public Task<LoyaltyTier?> GetTierByIdAsync(
        int tierId,
        CancellationToken cancellationToken = default)
    {
        return _db.LoyaltyTiers
            .FirstOrDefaultAsync(t => t.TierID == tierId, cancellationToken);
    }

    public Task<bool> TierNameExistsAsync(
        string tierName,
        int? excludingTierId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = tierName.ToLower();

        return _db.LoyaltyTiers.AnyAsync(
            t => t.TierName.ToLower() == normalizedName
                && (!excludingTierId.HasValue || t.TierID != excludingTierId.Value),
            cancellationToken);
    }

    public Task<bool> MinPointsExistsAsync(
        int minPoints,
        int? excludingTierId = null,
        CancellationToken cancellationToken = default)
    {
        return _db.LoyaltyTiers.AnyAsync(
            t => t.MinPoints == minPoints
                && (!excludingTierId.HasValue || t.TierID != excludingTierId.Value),
            cancellationToken);
    }

    public async Task<LoyaltyTier> AddTierAsync(
        LoyaltyTier tier,
        CancellationToken cancellationToken = default)
    {
        await _db.LoyaltyTiers.AddAsync(tier, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return tier;
    }

    public async Task<LoyaltyTier?> UpdateTierAsync(
        int tierId,
        string tierName,
        int minPoints,
        decimal discountRate,
        int maxRefundPerMonth,
        CancellationToken cancellationToken = default)
    {
        var tier = await _db.LoyaltyTiers
            .FirstOrDefaultAsync(t => t.TierID == tierId, cancellationToken);

        if (tier is null)
        {
            return null;
        }

        tier.TierName = tierName;
        tier.MinPoints = minPoints;
        tier.DiscountRate = discountRate;
        tier.MaxRefundPerMonth = maxRefundPerMonth;
        await _db.SaveChangesAsync(cancellationToken);
        return tier;
    }

    public Task<bool> HasAssignedUsersAsync(
        int tierId,
        CancellationToken cancellationToken = default)
    {
        return _db.Users.AnyAsync(u => u.LoyaltyTierID == tierId, cancellationToken);
    }

    public async Task<bool> DeleteTierAsync(
        int tierId,
        CancellationToken cancellationToken = default)
    {
        var tier = await _db.LoyaltyTiers
            .FirstOrDefaultAsync(t => t.TierID == tierId, cancellationToken);

        if (tier is null)
        {
            return false;
        }

        _db.LoyaltyTiers.Remove(tier);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task AddLoyaltyPointAsync(
        LoyaltyPoints loyaltyPoint,
        CancellationToken cancellationToken = default)
    {
        await _db.LoyaltyPoints.AddAsync(loyaltyPoint, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<LoyaltyPoints>> GetPointHistoryAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.LoyaltyPoints
            .Where(lp => lp.UserID == userId)
            .OrderByDescending(lp => lp.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateUserTierAsync(
        int userId,
        int tierID,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);

        if (user is not null)
        {
            user.LoyaltyTierID = tierID;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateUserTotalPointsAsync(
        int userId,
        int totalPoints,
        CancellationToken cancellationToken = default)
    {
        var connection = _db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await _db.Database.ExecuteSqlRawAsync(
                "EXEC sys.sp_set_session_context @key=N'SkipLoyaltyPointTrigger', @value=1",
                cancellationToken);

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);

            if (user is not null)
            {
                user.TotalPoints = totalPoints;
                user.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        finally
        {
            await _db.Database.ExecuteSqlRawAsync(
                "EXEC sys.sp_set_session_context @key=N'SkipLoyaltyPointTrigger', @value=NULL",
                CancellationToken.None);
            if (shouldCloseConnection)
                await connection.CloseAsync();
        }
    }

    public async Task<bool> HasPointsForBookingAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await _db.LoyaltyPoints
            .AnyAsync(lp => lp.BookingID == bookingId
                         && lp.TransactionType == LoyaltyTransactionTypes.Earned,
                      cancellationToken);
    }

    public async Task<List<LoyaltyTier>> GetTiersByIdsAsync(List<int> tierIds, CancellationToken cancellationToken = default)
    {
        return await _db.LoyaltyTiers
            .AsNoTracking()
            .Where(t => tierIds.Contains(t.TierID))
            .ToListAsync(cancellationToken);
    }
}
