using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
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
            .Where(b => b.UserID == userId && b.Status == BookingStatus.Paid)
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
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserID == userId, cancellationToken);

        if (user is not null)
        {
            user.TotalPoints = totalPoints;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
