using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IBookingRepository
{
    Task<Showtime?> GetShowtimeAsync(
        int showtimeId,
        CancellationToken cancellationToken = default);

    Task<List<Seat>> GetSeatsByIdsAsync(
        List<int> seatIds,
        CancellationToken cancellationToken = default);

    Task<List<int>> GetUnavailableSeatIdsAsync(
        int showtimeId,
        List<int> seatIds,
        int currentUserId,
        CancellationToken cancellationToken = default);

    Task ExpireStaleHoldsAsync(
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default);

    Task<bool> TryAddSeatHoldsAsync(
        IEnumerable<SeatHold> seatHolds,
        CancellationToken cancellationToken = default);

    Task<List<SeatHold>> GetMyActiveHoldsAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default);

    Task CreateBookingAndConfirmHoldsAsync(
        Booking booking,
        IEnumerable<SeatHold> holdsToConfirm,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetBookingByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<List<Booking>> GetBookingsByUserAsync(
        int userId,
        CancellationToken cancellationToken = default);
}