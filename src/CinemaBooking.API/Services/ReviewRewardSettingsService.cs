using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.API.Services;

public sealed class ReviewRewardSettingsService : IReviewRewardSettingsService
{
    private readonly IReviewRewardSettingsRepository _repository;

    public ReviewRewardSettingsService(IReviewRewardSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReviewRewardSettings?> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAsync(cancellationToken);
    }

    public async Task UpdateSettingsAsync(
        int firstReviewPoints,
        int nextReviewPoints,
        int? updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (firstReviewPoints < 0 || nextReviewPoints < 0)
        {
            throw new ArgumentException("Review points must be greater than or equal to 0");
        }

        var settings = await _repository.GetAsync(cancellationToken);
        if (settings is null)
        {
            throw new InvalidOperationException("ReviewRewardSettings not found");
        }

        settings.FirstReviewPoints = firstReviewPoints;
        settings.NextReviewPoints = nextReviewPoints;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedBy = updatedBy;

        await _repository.UpdateAsync(settings, cancellationToken);
    }
}
