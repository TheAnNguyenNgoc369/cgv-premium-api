namespace CinemaBooking.Application.Reviews;

public interface IReviewService
{
    Task<CreateReviewResult> CreateAsync(
        int userId,
        int bookingId,
        int rating,
        string? comment,
        CancellationToken cancellationToken = default);

    Task<GetMovieReviewsResult> GetMovieReviewsAsync(
        int movieId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<HideReviewResult> HideAsync(
        int reviewId,
        int adminId,
        CancellationToken cancellationToken = default);

    Task<HideReviewResult> UnhideAsync(
        int reviewId,
        CancellationToken cancellationToken = default);

    Task<AdminReviewPage> SearchAdminReviewsAsync(
        string? keyword,
        int? movieId,
        AdminReviewStatusFilter status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed record CreateReviewResult(
    bool Succeeded,
    string? ErrorMessage,
    int? ReviewId,
    string? ErrorCode = null);

public sealed record GetMovieReviewsResult(
    bool Succeeded,
    string? ErrorMessage,
    MovieReviewPage? Data,
    string? ErrorCode = null);

public sealed record HideReviewResult(
    bool Succeeded,
    string? ErrorMessage,
    string? ErrorCode = null);
