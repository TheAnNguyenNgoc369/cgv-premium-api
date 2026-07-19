using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IReviewRewardSettingsRepository
{
    Task<ReviewRewardSettings?> GetAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(ReviewRewardSettings settings, CancellationToken cancellationToken = default);
}
