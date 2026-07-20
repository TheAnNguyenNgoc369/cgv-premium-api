using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Reviews;

public sealed class ReviewService : IReviewService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IMovieReviewRepository _reviewRepository;
    private readonly IReviewRewardSettingsRepository _rewardSettingsRepository;
    private readonly ILoyaltyRepository _loyaltyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReviewService(
        IBookingRepository bookingRepository,
        IMovieReviewRepository reviewRepository,
        IReviewRewardSettingsRepository rewardSettingsRepository,
        ILoyaltyRepository loyaltyRepository,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _reviewRepository = reviewRepository;
        _rewardSettingsRepository = rewardSettingsRepository;
        _loyaltyRepository = loyaltyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateReviewResult> CreateAsync(
        int userId,
        int bookingId,
        int rating,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        // 1. Booking exists.
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
        if (booking is null)
        {
            return new CreateReviewResult(false, "Booking not found.", null, "not_found");
        }

        // 2. Booking belongs to current user.
        if (booking.UserID != userId)
        {
            return new CreateReviewResult(false, "You do not own this booking.", null, "forbidden");
        }

        // 3. Booking.Status == Used.
        // 4. Booking is NOT paid/cancelled/refunded/partially_refunded/no_show.
        if (booking.Status != BookingStatus.Used)
        {
            return new CreateReviewResult(false, "You can only review a movie after attending the show.", null, "invalid_state");
        }

        // 5. Showtime has ended.
        if (booking.Showtime is null || DateTime.UtcNow <= booking.Showtime.EndTime)
        {
            return new CreateReviewResult(false, "Showtime has not ended.", null, "invalid_state");
        }

        // 6. Booking has not already been reviewed.
        if (await _reviewRepository.BookingHasReviewAsync(bookingId, cancellationToken))
        {
            return new CreateReviewResult(false, "You have already reviewed this booking.", null, "conflict");
        }

        var movieId = booking.Showtime.MovieID;

        // 7. Rating must be integer 1~5 only.
        if (rating < 1 || rating > 5)
        {
            return new CreateReviewResult(false, "Rating must be between 1 and 5.", null, "invalid_input");
        }

        // 8. Comment optional. Trim whitespace. Max 2000 chars.
        string? normalizedComment = null;
        if (!string.IsNullOrWhiteSpace(comment))
        {
            normalizedComment = comment.Trim();
            if (normalizedComment.Length > 2000)
            {
                return new CreateReviewResult(false, "Comment must not exceed 2000 characters.", null, "invalid_input");
            }
        }

        // Review + reward inside ONE transaction.
        var reviewId = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var review = new MovieReview
            {
                MovieId = movieId,
                UserId = userId,
                BookingId = bookingId,
                Rating = rating,
                Comment = normalizedComment,
                IsHidden = false,
                CreatedAt = DateTime.UtcNow,
                HiddenAt = null,
                HiddenBy = null
            };

            // Determine reward BEFORE inserting this review.
            var isFirstReview = !await _reviewRepository.UserHasAnyReviewAsync(userId, cancellationToken);

            await _reviewRepository.AddAsync(review, cancellationToken);

            var settings = await _rewardSettingsRepository.GetAsync(cancellationToken);
            var pointsToAward = isFirstReview
                ? (settings?.FirstReviewPoints ?? 0)
                : (settings?.NextReviewPoints ?? 0);

            if (pointsToAward > 0)
            {
                var loyaltyPoint = new LoyaltyPoints
                {
                    UserID = userId,
                    BookingID = null,
                    PointsDelta = pointsToAward,
                    TransactionType = LoyaltyTransactionTypes.Earned,
                    Description = isFirstReview
                        ? $"Earned {pointsToAward} points for first movie review."
                        : $"Earned {pointsToAward} points for movie review.",
                    CreatedAt = DateTime.UtcNow
                };

                await _loyaltyRepository.AddLoyaltyPointAsync(loyaltyPoint, cancellationToken);

                var currentTotal = await _loyaltyRepository.GetUserTotalPointsAsync(userId, cancellationToken);
                await _loyaltyRepository.UpdateUserTotalPointsAsync(
                    userId,
                    currentTotal + pointsToAward,
                    cancellationToken);
            }

            return review.ReviewId;
        }, cancellationToken);

        return new CreateReviewResult(true, null, reviewId);
    }

    public async Task<GetMovieReviewsResult> GetMovieReviewsAsync(
        int movieId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize switch
        {
            < 1 => 10,
            > 50 => 50,
            _ => pageSize
        };

        if (!await _reviewRepository.MovieExistsAsync(movieId, cancellationToken))
        {
            return new GetMovieReviewsResult(false, "Movie not found.", null, "not_found");
        }

        var stats = await _reviewRepository.GetVisibleStatsForMovieAsync(movieId, cancellationToken);
        var items = await _reviewRepository.GetVisibleReviewsForMovieAsync(
            movieId,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);

        var pageData = new MovieReviewPage(
            movieId,
            stats.AverageRating,
            stats.TotalReviews,
            stats.RatingBreakdown,
            items,
            normalizedPage,
            normalizedPageSize);

        return new GetMovieReviewsResult(true, null, pageData);
    }

    public async Task<HideReviewResult> HideAsync(
        int reviewId,
        int adminId,
        CancellationToken cancellationToken = default)
    {
        var review = await _reviewRepository.GetForUpdateAsync(reviewId, cancellationToken);
        if (review is null)
        {
            return new HideReviewResult(false, "Review not found.", "not_found");
        }

        if (review.IsHidden)
        {
            return new HideReviewResult(true, null);
        }

        review.IsHidden = true;
        review.HiddenAt = DateTime.UtcNow;
        review.HiddenBy = adminId;

        await _reviewRepository.UpdateAsync(review, cancellationToken);
        return new HideReviewResult(true, null);
    }

    public async Task<HideReviewResult> UnhideAsync(
        int reviewId,
        CancellationToken cancellationToken = default)
    {
        var review = await _reviewRepository.GetForUpdateAsync(reviewId, cancellationToken);
        if (review is null)
        {
            return new HideReviewResult(false, "Review not found.", "not_found");
        }

        if (!review.IsHidden)
        {
            return new HideReviewResult(true, null);
        }

        review.IsHidden = false;
        review.HiddenAt = null;
        review.HiddenBy = null;

        await _reviewRepository.UpdateAsync(review, cancellationToken);
        return new HideReviewResult(true, null);
    }

    public async Task<AdminReviewPage> SearchAdminReviewsAsync(
        string? keyword,
        int? movieId,
        AdminReviewStatusFilter status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize switch
        {
            < 1 => 10,
            > 50 => 50,
            _ => pageSize
        };

        var (items, total) = await _reviewRepository.SearchAdminReviewsAsync(
            keyword,
            movieId,
            status,
            normalizedPage,
            normalizedPageSize,
            cancellationToken);

        var totalPages = normalizedPageSize == 0
            ? 0
            : (int)Math.Ceiling(total / (double)normalizedPageSize);

        return new AdminReviewPage(items, normalizedPage, normalizedPageSize, total, totalPages);
    }
}
