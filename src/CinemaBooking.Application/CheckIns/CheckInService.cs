using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.CheckIns;

public sealed class CheckInService : ICheckInService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IBookingRepository _bookingRepository;

    public CheckInService(
        ITicketRepository ticketRepository,
        IBookingRepository bookingRepository)
    {
        _ticketRepository = ticketRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, CheckInLookupResult? Data)> LookupAsync(
        string qrCode,
        int staffId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetTicketWithFullDetailsForCheckInAsync(qrCode, cancellationToken);

        if (ticket is null)
            return (false, "Ticket not found.", null);

        if (ticket.BookingSeat?.Booking is null)
            return (false, "Ticket data is incomplete (missing booking).", null);

        var booking = ticket.BookingSeat.Booking;

        var staffCinemaId = await _bookingRepository.GetStaffCinemaIdAsync(staffId, cancellationToken);

        if (staffCinemaId is null)
            return (false, "Staff cinema assignment not found.", null);

        if (booking.Showtime is null ||
            booking.Showtime.Room is null ||
            booking.Showtime.Room.Cinema is null)
            return (false, "Booking data is incomplete (missing showtime/room/cinema).", null);

        var showtimeCinemaId = booking.Showtime.Room.Cinema.CinemaID;

        if (staffCinemaId != showtimeCinemaId)
            return (false, "You cannot check in tickets from another cinema.", null);

        var response = new CheckInLookupResult
        {
            BookingId = booking.BookingID,
            BookingCode = booking.BookingCode,
            CustomerName = booking.User?.FullName,
            Movie = new CheckInLookupResult.MovieInfo
            {
                Title = booking.Showtime.Movie.Title,
                Rating = booking.Showtime.Movie.AgeRating,
                Duration = booking.Showtime.Movie.DurationMin,
                PosterURL = booking.Showtime.Movie.PosterURL
            },
            Cinema = new CheckInLookupResult.CinemaInfo
            {
                Name = booking.Showtime.Room.Cinema.CinemaName,
                Address = booking.Showtime.Room.Cinema.Address
            },
            Room = new CheckInLookupResult.RoomInfo
            {
                Name = booking.Showtime.Room.RoomName,
                RoomType = booking.Showtime.Room.RoomType.TypeName
            },
            Showtime = new CheckInLookupResult.ShowtimeInfo
            {
                StartTime = booking.Showtime.StartTime,
                EndTime = booking.Showtime.EndTime
            },
            PaymentStatus = booking.Payment?.Status ?? "pending",
            BookingStatus = booking.Status,
            CheckedIn = booking.BookingSeats.Any(bs => bs.Ticket?.Status == TicketStatus.Used),
            Seats = booking.BookingSeats.Select(bs => new CheckInLookupResult.SeatInfo
            {
                Row = bs.Seat.SeatRow,
                Column = bs.Seat.SeatCol,
                SeatType = bs.Seat.SeatType?.TypeName ?? "Standard",
                TicketPrice = bs.TicketPrice,
                IsCheckedIn = bs.Ticket?.Status == TicketStatus.Used,
                CheckedInAt = bs.Ticket?.CheckedInAt
            }).ToList(),
            Products = booking.BookingFnBs.Select(fnb => new CheckInLookupResult.ProductInfo
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
        string qrCode,
        int staffId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetTicketWithFullDetailsForCheckInAsync(qrCode, cancellationToken);

        if (ticket is null)
            return (false, "Ticket not found.", null, null);

        if (ticket.BookingSeat?.Booking is null)
            return (false, "Ticket data is incomplete (missing booking).", null, null);

        var booking = ticket.BookingSeat.Booking;

        var staffCinemaId = await _bookingRepository.GetStaffCinemaIdAsync(staffId, cancellationToken);

        if (staffCinemaId is null)
            return (false, "Staff cinema assignment not found.", null, null);

        if (booking.Showtime?.Room?.Cinema is null)
            return (false, "Booking data is incomplete (missing showtime/room/cinema).", null, null);

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

        if (ticket.Status == TicketStatus.Used)
            return (false, "This ticket has already been checked in.", null, null);

        if (ticket.Status == TicketStatus.Cancelled)
            return (false, "This ticket has been cancelled.", null, null);

        if (ticket.Status == TicketStatus.Refunded)
            return (false, "This ticket has been refunded.", null, null);

        var now = DateTime.UtcNow;
        var showtimeStart = booking.Showtime.StartTime;
        var earliestCheckIn = showtimeStart.AddMinutes(-30);
        var latestCheckIn = booking.Showtime.EndTime;

        if (now < earliestCheckIn || now > latestCheckIn)
            return (false, "Check-in time has expired.", null, null);

        var success = await _ticketRepository.PerformTicketCheckInAsync(
            ticket.TicketID,
            booking.BookingID,
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
        int currentUserId,
        bool isAdmin,
        bool isManager,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        if (isStaff && !isManager && !isAdmin)
        {
            staffId = currentUserId;
            cinemaId = await _bookingRepository.GetStaffCinemaIdAsync(currentUserId, cancellationToken)
                ?? -1;
        }
        else if (isManager && !isAdmin)
        {
            cinemaId = await _bookingRepository.GetStaffCinemaIdAsync(currentUserId, cancellationToken)
                ?? -1;
        }

        var (bookings, totalCount) = await _bookingRepository.GetCheckInHistoryAsync(
            staffId,
            cinemaId,
            from,
            to,
            page,
            pageSize,
            cancellationToken);

        var records = bookings.Select(b =>
        {
            var mostRecentTicket = b.BookingSeats
                .Where(bs => bs.Ticket != null && bs.Ticket.CheckedInAt != null)
                .OrderByDescending(bs => bs.Ticket!.CheckedInAt)
                .Select(bs => bs.Ticket)
                .FirstOrDefault();

            var checkedInSeats = b.BookingSeats
                .Where(bs => bs.Ticket != null && bs.Ticket.Status == TicketStatus.Used)
                .OrderBy(bs => bs.Seat.SeatRow)
                .ThenBy(bs => bs.Seat.SeatCol)
                .Select(bs => new CheckInHistoryResult.CheckedInSeatRecord
                {
                    SeatCode = $"{bs.Seat.SeatRow}{bs.Seat.SeatCol}",
                    SeatType = bs.Seat.SeatType?.TypeName ?? "Standard",
                    TicketPrice = bs.TicketPrice,
                    CheckedInAt = bs.Ticket!.CheckedInAt ?? DateTime.MinValue
                })
                .ToList();

            return new CheckInHistoryResult.CheckInRecord
            {
                BookingId = b.BookingID,
                BookingCode = b.BookingCode,
                CustomerName = b.User?.FullName,
                MovieTitle = b.Showtime.Movie.Title,
                CinemaName = b.Showtime.Room.Cinema.CinemaName,
                RoomName = b.Showtime.Room.RoomName,
                ShowtimeStart = b.Showtime.StartTime,
                CheckedInAt = mostRecentTicket?.CheckedInAt ?? DateTime.MinValue,
                StaffName = mostRecentTicket?.CheckedInBy?.FullName ?? "Unknown",
                SeatCount = b.BookingSeats.Count,
                TotalAmount = b.FinalAmount,
                CheckedInSeats = checkedInSeats
            };
        }).ToList();

        return new CheckInHistoryResult
        {
            Records = records,
            TotalCount = totalCount
        };
    }

    public Task<CheckInHistoryResult> GetHistoryAsync(
        int? staffId,
        int? cinemaId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        GetHistoryAsync(
            staffId,
            cinemaId,
            from,
            to,
            page,
            pageSize,
            currentUserId: 0,
            isAdmin: true,
            isManager: false,
            isStaff: false,
            cancellationToken);
}
