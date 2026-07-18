namespace CinemaBooking.Application.Reviews;

public sealed record ReviewListItem(
    int ReviewId,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    int UserId,
    string UserFullName,
    string? UserAvatarUrl);

public sealed record MovieReviewStats(
    double? AverageRating,
    int TotalReviews,
    IReadOnlyDictionary<int, int> RatingBreakdown);

public sealed record MovieReviewPage(
    int MovieId,
    double? AverageRating,
    int TotalReviews,
    IReadOnlyDictionary<int, int> RatingBreakdown,
    IReadOnlyList<ReviewListItem> Items,
    int Page,
    int PageSize);
