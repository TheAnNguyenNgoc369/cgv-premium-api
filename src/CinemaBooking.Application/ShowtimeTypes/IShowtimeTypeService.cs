namespace CinemaBooking.Application.ShowtimeTypes;

public interface IShowtimeTypeService
{
    Task<(bool Succeeded, string? Error, ShowtimeTypePage? Page)> ListAsync(int? cinemaId, bool? active, int page, int pageSize, int? scope, CancellationToken ct);
    Task<ShowtimeTypeWriteResult> GetAsync(int id, int? scope, CancellationToken ct);
    Task<ShowtimeTypeWriteResult> CreateAsync(int cinemaId, string name, IReadOnlyList<TimeSpan> slots, int? scope, CancellationToken ct);
    Task<ShowtimeTypeWriteResult> UpdateAsync(int id, string name, bool active, IReadOnlyList<TimeSpan> slots, int? scope, CancellationToken ct);
    Task<ShowtimeTypeWriteResult> DeleteAsync(int id, int? scope, CancellationToken ct);
    Task<ShowtimeTypeBatchResult> PreviewAsync(int movieId, int roomId, DateOnly start, DateOnly end, int typeId, decimal price, int? scope, CancellationToken ct);
    Task<ShowtimeTypeBatchResult> GenerateAsync(int movieId, int roomId, DateOnly start, DateOnly end, int typeId, decimal price, int? scope, CancellationToken ct);
}
