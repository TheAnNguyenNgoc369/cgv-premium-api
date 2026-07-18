using CinemaBooking.Application.Reviews;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IMovieReviewRepository
{
    Task<MovieReview> AddAsync(MovieReview review, CancellationToken cancellationToken = default);
    Task<MovieReview?> GetByIdAsync(int reviewId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int reviewId, CancellationToken cancellationToken = default);

    Task<bool> BookingHasReviewAsync(int bookingId, CancellationToken cancellationToken = default);
    Task<bool> UserHasReviewedMovieAsync(int userId, int movieId, CancellationToken cancellationToken = default);
    Task<bool> UserHasAnyReviewAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> MovieExistsAsync(int movieId, CancellationToken cancellationToken = default);

    Task<MovieReview?> GetForUpdateAsync(int reviewId, CancellationToken cancellationToken = default);

    Task UpdateAsync(MovieReview review, CancellationToken cancellationToken = default);

    Task<MovieReviewStats> GetVisibleStatsForMovieAsync(
        int movieId,
        CancellationToken cancellationToken = default);

    Task<List<ReviewListItem>> GetVisibleReviewsForMovieAsync(
        int movieId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
