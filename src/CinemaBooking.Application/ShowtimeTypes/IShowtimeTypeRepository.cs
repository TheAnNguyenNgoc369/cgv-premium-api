using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.ShowtimeTypes;

public interface IShowtimeTypeRepository
{
    Task<ShowtimeTypePage> ListAsync(int? cinemaId, bool? isActive, int page, int pageSize, CancellationToken ct);
    Task<ShowtimeType?> GetAsync(int id, bool tracking, CancellationToken ct);
    Task<bool> CinemaExistsAsync(int id, CancellationToken ct);
    Task<bool> NameExistsAsync(int cinemaId, string name, int? exceptId, CancellationToken ct);
    Task<CinemaBooking.Domain.Entities.Movie?> GetMovieAsync(int id, CancellationToken ct);
    Task<Room?> GetRoomAsync(int id, CancellationToken ct);
    Task<bool> HasValidSeatAsync(int roomId, CancellationToken ct);
    Task<bool> HasConflictAsync(int roomId, DateTime start, DateTime end, CancellationToken ct);
    Task AddAsync(ShowtimeType value, CancellationToken ct);
    void ReplaceSlots(ShowtimeType value, IEnumerable<TimeSpan> slots);
    Task SaveAsync(CancellationToken ct);
    Task AddShowtimesAsync(IEnumerable<Showtime> values, CancellationToken ct);
    Task<T> TransactionAsync<T>(Func<Task<T>> action, CancellationToken ct);
    Task LockScheduleAsync(int roomId, int cinemaId, CancellationToken ct);
}
