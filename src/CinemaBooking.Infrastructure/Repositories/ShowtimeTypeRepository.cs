using CinemaBooking.Application.ShowtimeTypes;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class ShowtimeTypeRepository(CinemaBookingDbContext db) : IShowtimeTypeRepository
{
    public async Task<ShowtimeTypePage> ListAsync(int? cinemaId, bool? isActive, int page, int pageSize, CancellationToken ct)
    {
        var q = db.ShowtimeTypes.AsNoTracking().Include(x => x.Slots).AsQueryable();
        if (cinemaId.HasValue) q = q.Where(x => x.CinemaID == cinemaId);
        if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive);
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new(items, total);
    }
    public Task<ShowtimeType?> GetAsync(int id, bool tracking, CancellationToken ct)
    {
        var q = db.ShowtimeTypes.Include(x => x.Slots).AsQueryable();
        if (!tracking) q = q.AsNoTracking();
        return q.FirstOrDefaultAsync(x => x.ShowtimeTypeID == id, ct);
    }
    public Task<bool> CinemaExistsAsync(int id, CancellationToken ct) => db.Cinemas.AnyAsync(x => x.CinemaID == id, ct);
    public Task<bool> NameExistsAsync(int cinemaId, string name, int? exceptId, CancellationToken ct) =>
        db.ShowtimeTypes.AnyAsync(x => x.CinemaID == cinemaId && x.Name == name
            && (!exceptId.HasValue || x.ShowtimeTypeID != exceptId), ct);
    public Task<Movie?> GetMovieAsync(int id, CancellationToken ct) => db.Movie.AsNoTracking().FirstOrDefaultAsync(x => x.MovieID == id, ct);
    public Task<Room?> GetRoomAsync(int id, CancellationToken ct) => db.Rooms.AsNoTracking().Include(x => x.Cinema).FirstOrDefaultAsync(x => x.RoomID == id, ct);
    public Task<bool> HasConflictAsync(int roomId, DateTime start, DateTime end, CancellationToken ct) =>
        db.Showtimes.AnyAsync(x => x.RoomID == roomId && x.Status != "cancelled" && x.StartTime < end && x.EndTime > start, ct);
    public async Task AddAsync(ShowtimeType value, CancellationToken ct) { db.ShowtimeTypes.Add(value); await db.SaveChangesAsync(ct); }
    public void ReplaceSlots(ShowtimeType value, IEnumerable<TimeSpan> slots)
    {
        db.ShowtimeTypeSlots.RemoveRange(value.Slots);
        value.Slots.Clear();
        foreach (var slot in slots)
            value.Slots.Add(new ShowtimeTypeSlot { StartTime = slot });
    }
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public async Task AddShowtimesAsync(IEnumerable<Showtime> values, CancellationToken ct) { db.Showtimes.AddRange(values); await db.SaveChangesAsync(ct); }
    public async Task<T> TransactionAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var result = await action();
            await tx.CommitAsync(ct);
            return result;
        });
    }
    public async Task LockScheduleAsync(int roomId, int cinemaId, CancellationToken ct)
    {
        await db.Rooms.FromSqlInterpolated($"SELECT * FROM [Room] WITH (UPDLOCK, HOLDLOCK) WHERE RoomID = {roomId}").LoadAsync(ct);
        await db.Cinemas.FromSqlInterpolated($"SELECT * FROM [Cinema] WITH (UPDLOCK, HOLDLOCK) WHERE CinemaID = {cinemaId}").LoadAsync(ct);
    }
}
