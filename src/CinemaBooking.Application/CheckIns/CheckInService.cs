using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.CheckIns;

public sealed class CheckInService : ICheckInService
{
    private readonly IBookingRepository _bookingRepository;

    public CheckInService(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, CheckInLookupResult? Data)> LookupAsync(
        string qrCode,
        int staffId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetBookingByQRCodeAsync(qrCode, cancellationToken);

        if (booking is null)
            return (false, "Booking not found.", null);

        var staffCinemaId = await _bookingRepository.GetStaffCinemaIdAsync(staffId, cancellationToken);

        if (staffCinemaId is null)
            return (false, "Staff cinema assignment not found.", null);

        var bookingWithDetails = await _bookingRepository.GetBookingWithFullDetailsForCheckInAsync(
            booking.BookingID,
            cancellationToken);

        if (bookingWithDetails is null)
            return (false, "Booking not found.", null);

        var showtimeCinemaId = bookingWithDetails.Showtime.Room.Cinema.CinemaID;

        if (staffCinemaId != showtimeCinemaId)
            return (false, "You cannot check in tickets from another cinema.", null);

        var response = new CheckInLookupResult
        {
            BookingId = bookingWithDetails.BookingID,
            BookingCode = bookingWithDetails.BookingCode,
            CustomerName = bookingWithDetails.User?.FullName,
            Movie = new CheckInLookupResult.MovieInfo
            {
                Title = bookingWithDetails.Showtime.Movie.Title,
                Rating = bookingWithDetails.Showtime.Movie.AgeRating,
                Duration = bookingWithDetails.Showtime.Movie.DurationMin,
                PosterURL = bookingWithDetails.Showtime.Movie.PosterURL
            },
            Cinema = new CheckInLookupResult.CinemaInfo
            {
                Name = bookingWithDetails.Showtime.Room.Cinema.CinemaName,
                Address = bookingWithDetails.Showtime.Room.Cinema.Address
            },
            Room = new CheckInLookupResult.RoomInfo
            {
                Name = bookingWithDetails.Showtime.Room.RoomName,
                RoomType = bookingWithDetails.Showtime.Room.RoomType
            },
            Showtime = new CheckInLookupResult.ShowtimeInfo
            {
                StartTime = bookingWithDetails.Showtime.StartTime,
                EndTime = bookingWithDetails.Showtime.EndTime
            },
            PaymentStatus = bookingWithDetails.Payment?.Status ?? "pending",
            BookingStatus = bookingWithDetails.Status,
            CheckedIn = bookingWithDetails.CheckedInAt.HasValue,
            Seats = bookingWithDetails.BookingSeats.Select(bs => new CheckInLookupResult.SeatInfo
            {
                Row = bs.Seat.SeatRow,
                Column = bs.Seat.SeatCol,
                SeatType = bs.Seat.SeatType?.TypeName ?? "Standard",
                TicketPrice = bs.TicketPrice
            }).ToList(),
            Products = bookingWithDetails.BookingFnBs.Select(fnb => new CheckInLookupResult.ProductInfo
            {
                ProductName = fnb.Product.ItemName,
                Quantity = fnb.Quantity,
                UnitPrice = fnb.UnitPrice,
                Subtotal = fnb.SubTotal
            }).ToList()
        };

        return (true, null, response);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, string? BookingCode, DateTime? CheckedInAt)> CheckInAsync(
        int bookingId,
        int staffId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetBookingWithFullDetailsForCheckInAsync(
            bookingId,
            cancellationToken);

        if (booking is null)
            return (false, "Booking not found.", null, null);

        var staffCinemaId = await _bookingRepository.GetStaffCinemaIdAsync(staffId, cancellationToken);

        if (staffCinemaId is null)
            return (false, "Staff cinema assignment not found.", null, null);

        var showtimeCinemaId = booking.Showtime.Room.Cinema.CinemaID;

        if (staffCinemaId != showtimeCinemaId)
            return (false, "You cannot check in tickets from another cinema.", null, null);

        if (booking.Payment?.Status != PaymentStatus.Completed)
            return (false, "Booking has not been paid.", null, null);

        if (booking.Status == BookingStatus.Cancelled)
            return (false, "Booking has been cancelled.", null, null);

        var hasCompletedRefund = booking.Refunds.Any(r => r.Status == "completed");
        if (hasCompletedRefund)
            return (false, "Ticket has been refunded.", null, null);

        if (booking.CheckedInAt.HasValue)
            return (false, "Ticket has already been checked in.", null, null);

        var now = DateTime.UtcNow;
        var showtimeStart = booking.Showtime.StartTime;
        var earliestCheckIn = showtimeStart.AddMinutes(-30);
        var latestCheckIn = showtimeStart.AddMinutes(15);

        if (now < earliestCheckIn || now > latestCheckIn)
            return (false, "Check-in time has expired.", null, null);

        var success = await _bookingRepository.PerformCheckInAsync(
            bookingId,
            staffId,
            ipAddress,
            now,
            cancellationToken);

        if (!success)
            return (false, "Failed to perform check-in.", null, null);

        return (true, null, booking.BookingCode, now);
    }

    public async Task<CheckInHistoryResult> GetHistoryAsync(
        int? staffId,
        int? cinemaId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (bookings, totalCount) = await _bookingRepository.GetCheckInHistoryAsync(
            staffId,
            cinemaId,
            from,
            to,
            page,
            pageSize,
            cancellationToken);

        var records = bookings.Select(b => new CheckInHistoryResult.CheckInRecord
        {
            BookingId = b.BookingID,
            BookingCode = b.BookingCode,
            CustomerName = b.User?.FullName,
            MovieTitle = b.Showtime.Movie.Title,
            CinemaName = b.Showtime.Room.Cinema.CinemaName,
            RoomName = b.Showtime.Room.RoomName,
            ShowtimeStart = b.Showtime.StartTime,
            CheckedInAt = b.CheckedInAt!.Value,
            StaffName = b.CheckedInByUser?.FullName ?? "Unknown",
            SeatCount = b.BookingSeats.Count,
            TotalAmount = b.FinalAmount
        }).ToList();

        return new CheckInHistoryResult
        {
            Records = records,
            TotalCount = totalCount
        };
    }
}
