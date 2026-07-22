using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Bookings;

public interface IBookingService
{
    Task<(bool Succeeded, string? ErrorMessage, List<int>? HoldIds, DateTime? ExpiresAt, SeatValidationErrors? SeatErrors)> HoldSeatsAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> ReleaseSeatHoldsAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Booking? Booking, SeatValidationErrors? SeatErrors)> CreateBookingAsync(
        int actorUserId,
        int? customerId,
        bool isStaff,
        int? showtimeId,
        List<int> seatIds,
        List<BookingFnBItemDto> fnbItems,
        string? voucherCode,
        int? staffCinemaId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, PricingCalculationResult? Result)> CalculatePricingAsync(
        int? userId,
        int? showtimeId,
        List<int> seatIds,
        List<BookingFnBItemDto> fnbItems,
        string? voucherCode,
        int? staffCinemaId = null,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetBookingByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<List<Booking>> GetMyBookingsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, LookupBookingFnbResult? Result)> LookupBookingFnbAsync(
        string bookingCode,
        int staffId,
        CancellationToken cancellationToken = default);
}

public record BookingFnBItemDto(int ItemId, int Quantity);

public sealed class LookupBookingFnbResult
{
    public int BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerAvatarURL { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<LookupBookingFnbItem> FnbItems { get; set; } = [];

    public sealed class LookupBookingFnbItem
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ImageURL { get; set; }
        public int Quantity { get; set; }
        public bool PickedUp { get; set; }
    }
}
