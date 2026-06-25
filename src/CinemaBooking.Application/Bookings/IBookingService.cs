using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Bookings;

public interface IBookingService
{
    Task<(bool Succeeded, string? ErrorMessage, List<int>? HoldIds, DateTime? ExpiresAt)> HoldSeatsAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Booking? Booking)> CreateBookingAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        List<BookingFnBItemDto> fnbItems,
        string? voucherCode,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetBookingByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<List<Booking>> GetMyBookingsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}

public record BookingFnBItemDto(int ItemId, int Quantity);