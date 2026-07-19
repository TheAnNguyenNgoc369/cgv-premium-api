using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class ReviewRewardSettingsRepository : IReviewRewardSettingsRepository
{
    private readonly CinemaBookingDbContext _db;

    public ReviewRewardSettingsRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<ReviewRewardSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _db.ReviewRewardSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == 1, cancellationToken);
    }

    public async Task UpdateAsync(ReviewRewardSettings settings, CancellationToken cancellationToken = default)
    {
        _db.ReviewRewardSettings.Update(settings);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
