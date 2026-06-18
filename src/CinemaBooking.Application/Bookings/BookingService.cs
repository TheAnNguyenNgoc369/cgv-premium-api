using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Bookings;

public sealed class BookingService : IBookingService
{
    private const int HoldDurationMinutes = 10;

    private readonly IBookingRepository _bookingRepository;

    public BookingService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, List<int>? HoldIds, DateTime? ExpiresAt)> HoldSeatsAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default)
    {
        if (seatIds.Count == 0)
            return (false, "Vui lòng chọn ít nhất 1 ghế", null, null);

        var showtime = await _bookingRepository.GetShowtimeAsync(showtimeId, cancellationToken);
        if (showtime is null)
            return (false, "Không tìm thấy suất chiếu", null, null);

        var unavailableSeatIds = await _bookingRepository.GetUnavailableSeatIdsAsync(
            showtimeId, seatIds, userId, cancellationToken);

        if (unavailableSeatIds.Count > 0)
            return (false, $"Ghế ID {string.Join(", ", unavailableSeatIds)} đã được đặt hoặc đang được giữ bởi người khác", null, null);

        var now = DateTime.Now;
        var expiresAt = now.AddMinutes(HoldDurationMinutes);

        var holds = seatIds.Select(seatId => new SeatHold
        {
            SeatID = seatId,
            ShowtimeID = showtimeId,
            UserID = userId,
            HeldAt = now,
            ExpiresAt = expiresAt,
            Status = "holding"
        }).ToList();

        await _bookingRepository.AddSeatHoldsAsync(holds, cancellationToken);

        return (true, null, holds.Select(h => h.HoldID).ToList(), expiresAt);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Booking? Booking)> CreateBookingAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default)
    {
        if (seatIds.Count == 0)
            return (false, "Vui lòng chọn ít nhất 1 ghế", null);

        var showtime = await _bookingRepository.GetShowtimeAsync(showtimeId, cancellationToken);
        if (showtime is null)
            return (false, "Không tìm thấy suất chiếu", null);

        var myHolds = await _bookingRepository.GetMyActiveHoldsAsync(
            userId, showtimeId, seatIds, cancellationToken);

        if (myHolds.Count != seatIds.Count)
            return (false, "Một số ghế chưa được giữ hoặc đã hết hạn, vui lòng chọn lại", null);

        var seats = await _bookingRepository.GetSeatsByIdsAsync(seatIds, cancellationToken);

        var bookingSeats = seats.Select(seat => new BookingSeat
        {
            SeatID = seat.SeatID,
            TicketPrice = showtime.BasePrice + seat.SeatType.ExtraPrice
        }).ToList();

        var subTotal = bookingSeats.Sum(bs => bs.TicketPrice);

        var booking = new Booking
        {
            BookingCode = GenerateBookingCode(),
            UserID = userId,
            ShowtimeID = showtimeId,
            SubTotal = subTotal,
            DiscountAmount = 0,
            FinalAmount = subTotal,
            Status = "pending",
            BookingDate = DateTime.Now,
            UpdatedAt = DateTime.Now,
            BookingSeats = bookingSeats
        };

        await _bookingRepository.AddBookingAsync(booking, cancellationToken);
        await _bookingRepository.MarkHoldsAsConfirmedAsync(myHolds, cancellationToken);

        var savedBooking = await _bookingRepository.GetBookingByIdAsync(booking.BookingID, cancellationToken);

        return (true, null, savedBooking);
    }

    public async Task<Booking?> GetBookingByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
    }

    public async Task<List<Booking>> GetMyBookingsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _bookingRepository.GetBookingsByUserAsync(userId, cancellationToken);
    }

    private static string GenerateBookingCode()
    {
        return $"BK{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }
}