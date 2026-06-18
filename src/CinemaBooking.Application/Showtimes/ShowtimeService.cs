using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Showtimes;

public sealed class ShowtimeService : IShowtimeService
{
    private readonly IShowtimeRepository _showtimeRepository;

    public ShowtimeService(IShowtimeRepository showtimeRepository)
    {
        _showtimeRepository = showtimeRepository;
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
            bookedSeatIds.Contains(seat.SeatID) ? "booked"
                : heldSeatIds.Contains(seat.SeatID) ? "held"
                : "available"
        )).ToList();

        return new SeatMapResult(
            showtime.ShowtimeID,
            showtime.Room.RoomName,
            showtime.Room.RoomType,
            seatResults
        );
    }
}