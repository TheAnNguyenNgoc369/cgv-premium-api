using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IReviewRewardSettingsService
{
    Task<ReviewRewardSettings?> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateSettingsAsync(int firstReviewPoints, int nextReviewPoints, int? updatedBy, CancellationToken cancellationToken = default);
}
