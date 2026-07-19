using CinemaBooking.Application.Features.AI.DTOs;

namespace CinemaBooking.Application.Features.AI;

public interface IRecommendationEngine
{
    Task<IReadOnlyList<MovieRecommendation>> GetMovieRecommendationsAsync(
        IntentResult intent,
        IReadOnlyList<int> preferredGenreIds,
        IReadOnlyList<string> recentlyWatchedTitles,
        int maxResults = 5,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FnBRecommendation>> GetFnBRecommendationsAsync(
        IntentResult intent,
        IReadOnlyList<string> userVoucherCodes,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}
