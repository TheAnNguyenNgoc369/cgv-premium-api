using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.Enums;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Showtimes;

public sealed class ShowtimeService : IShowtimeService
{
    private const string InvalidStatusMessage =
        "Invalid showtime status. (scheduled, ongoing, completed, cancelled)";

    private readonly IShowtimeRepository _showtimeRepository;

    public ShowtimeService(IShowtimeRepository showtimeRepository)
    {
        _showtimeRepository = showtimeRepository;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, ShowtimePageResult? Page)> GetShowtimesAsync(
        string? movieName, string? roomName, DateOnly? date, string? status,
        int page, int pageSize, string? sortBy, string? sortDir,
        CancellationToken cancellationToken = default)
    {
        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusResult = EnumValueMapper.Validate(
                status, "Status", DatabaseEnumMappings.ShowtimeStatuses);
            if (!statusResult.Succeeded) return (false, InvalidStatusMessage, null);
            normalizedStatus = statusResult.DatabaseValue;
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;
        var normalizedSort = sortBy?.Trim().ToLowerInvariant();
        normalizedSort = normalizedSort is "endtime" or "baseprice" or "status" ? normalizedSort : "starttime";
        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var result = await _showtimeRepository.GetShowtimesAsync(
            movieName?.Trim(), roomName?.Trim(), date, normalizedStatus,
            page, pageSize, normalizedSort, descending, cancellationToken);
        return (true, null, new ShowtimePageResult(result.Items, page, pageSize, result.TotalItems));
    }

    public Task<(bool Succeeded, string? ErrorMessage, Showtime? Showtime)> CreateShowtimeAsync(
        int movieId, int roomId, DateTime startTime, decimal basePrice, string status,
        CancellationToken cancellationToken = default) =>
        SaveAsync(null, movieId, roomId, startTime, basePrice, status, cancellationToken);

    public async Task<(bool Succeeded, string? ErrorMessage, Showtime? Showtime)> UpdateShowtimeAsync(
        int id, int movieId, int roomId, DateTime startTime, decimal basePrice, string status,
        CancellationToken cancellationToken = default)
    {
        var existing = await _showtimeRepository.GetShowtimeByIdAsync(id, cancellationToken);
        if (existing is null) return (false, "Showtime not found", null);
        if (await _showtimeRepository.HasActiveBookingOrHoldAsync(id, DateTime.UtcNow, cancellationToken))
            return (false, "Showtime has active bookings or seat holds", null);
        return await SaveAsync(existing, movieId, roomId, startTime, basePrice, status, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteShowtimeAsync(
        int id, CancellationToken cancellationToken = default)
    {
        if (await _showtimeRepository.GetShowtimeByIdAsync(id, cancellationToken) is null)
            return (false, "Showtime not found");
        if (await _showtimeRepository.HasActiveBookingOrHoldAsync(id, DateTime.UtcNow, cancellationToken))
            return (false, "Showtime has active bookings or seat holds");
        return await _showtimeRepository.DeleteAsync(id, cancellationToken)
            ? (true, null) : (false, "Showtime not found");
    }

    public Task<bool> IsSoldOutAsync(Showtime showtime, CancellationToken cancellationToken = default) =>
        _showtimeRepository.IsSoldOutAsync(
            showtime.ShowtimeID, showtime.Room.Capacity, DateTime.UtcNow, cancellationToken);

    private async Task<(bool Succeeded, string? ErrorMessage, Showtime? Showtime)> SaveAsync(
        Showtime? existing, int movieId, int roomId, DateTime startTime,
        decimal basePrice, string status, CancellationToken cancellationToken)
    {
        var statusResult = EnumValueMapper.Validate(
            status, "Status", DatabaseEnumMappings.ShowtimeStatuses);
        var normalizedStatus = statusResult.DatabaseValue;
        if (!statusResult.Succeeded)
            return (false, InvalidStatusMessage, null);
        if (basePrice < 0) return (false, "Base price must be greater than or equal to 0", null);
        var movie = await _showtimeRepository.GetMovieAsync(movieId, cancellationToken);
        if (movie is null) return (false, "Movie not found", null);
        if (movie.Status != "now_showing")
            return (false,
                "The movie must be 'now_showing' to update or create showtimes.",
                null);
        var room = await _showtimeRepository.GetRoomAsync(roomId, cancellationToken);
        if (room is null) return (false, "Room not found", null);
        if (room.Status != "active") return (false, "Room must be active", null);
        var endTime = startTime.AddMinutes(movie.DurationMin + 30);
        if (normalizedStatus != "cancelled" &&
            await _showtimeRepository.HasConflictAsync(roomId, startTime, endTime,
                existing?.ShowtimeID, cancellationToken))
            return (false, "Showtime conflicts with another showtime in the same room", null);

        var showtime = existing ?? new Showtime { CreatedAt = DateTime.UtcNow };
        showtime.MovieID = movieId; showtime.RoomID = roomId;
        showtime.StartTime = startTime; showtime.EndTime = endTime;
        showtime.BasePrice = basePrice; showtime.Status = normalizedStatus!;
        var saved = existing is null
            ? await _showtimeRepository.AddAsync(showtime, cancellationToken)
            : await _showtimeRepository.UpdateAsync(showtime, cancellationToken);
        return saved is null ? (false, "Showtime not found", null) : (true, null, saved);
    }

    public async Task<List<Showtime>> GetShowtimesByMovieAsync(
        int movieId,
        DateOnly? date,
        int? cinemaId,
        CancellationToken cancellationToken = default)
    {
        return await _showtimeRepository.GetShowtimesByMovieAsync(
            movieId, date, cinemaId, cancellationToken);
    }

    public async Task<Showtime?> GetShowtimeByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _showtimeRepository.GetShowtimeByIdAsync(id, cancellationToken);
    }

    public async Task<SeatMapResult?> GetSeatMapAsync(
        int showtimeId,
        CancellationToken cancellationToken = default)
    {
        var showtime = await _showtimeRepository.GetShowtimeByIdAsync(showtimeId, cancellationToken);
        if (showtime is null)
            return null;

        var seats = await _showtimeRepository.GetSeatsByRoomAsync(showtime.RoomID, cancellationToken);
        var bookedSeatIds = await _showtimeRepository.GetBookedSeatIdsAsync(showtimeId, cancellationToken);
        var heldSeatIds = await _showtimeRepository.GetHeldSeatIdsAsync(showtimeId, cancellationToken);

        var seatResults = seats.Select(seat => new SeatMapSeatResult(
            seat.SeatID,
            seat.SeatRow,
            seat.SeatCol,
            seat.SeatType.TypeName,
            seat.SeatType.ExtraPrice,
            showtime.BasePrice + seat.SeatType.ExtraPrice,
            bookedSeatIds.Contains(seat.SeatID) ? SeatStatus.Booked
                : heldSeatIds.Contains(seat.SeatID) ? SeatStatus.Held
                : SeatStatus.Available
        )).ToList();

        return new SeatMapResult(
            showtime.ShowtimeID,
            showtime.Room.RoomName,
            showtime.Room.RoomType,
            seatResults
        );
    }
}
