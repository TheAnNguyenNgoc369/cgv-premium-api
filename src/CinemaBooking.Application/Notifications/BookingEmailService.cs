using System.Text;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Shared.Time;
using Microsoft.Extensions.Logging;
using QRCoder;

namespace CinemaBooking.Application.Notifications;

public sealed class BookingEmailService : IBookingEmailService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEmailQueue _emailQueue;
    private readonly ILogger<BookingEmailService> _logger;

    public BookingEmailService(
        IBookingRepository bookingRepository,
        IEmailQueue emailQueue,
        ILogger<BookingEmailService> logger)
    {
        _bookingRepository = bookingRepository;
        _emailQueue = emailQueue;
        _logger = logger;
    }

    public async Task QueueBookingConfirmedAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
            if (booking?.User is null || string.IsNullOrWhiteSpace(booking.User.Email))
                return;

            var ticketSeats = booking.BookingSeats.Where(bs => bs.Ticket is not null).ToList();
            if (ticketSeats.Count == 0)
                return;

            var images = new List<EmailInlineImage>(ticketSeats.Count);
            var ticketsHtml = new StringBuilder();
            foreach (var bookingSeat in ticketSeats)
            {
                var ticket = bookingSeat.Ticket!;
                var contentId = $"ticket-{ticket.TicketID}";
                images.Add(new EmailInlineImage(
                    contentId,
                    PngByteQRCodeHelper.GetQRCode(ticket.QRCode, QRCodeGenerator.ECCLevel.Q, 10),
                    "image/png",
                    $"ticket-{booking.BookingCode}-{bookingSeat.Seat.SeatRow}{bookingSeat.Seat.SeatCol}.png"));
                ticketsHtml.Append(BookingEmailTemplate.BuildTicket(
                    $"{bookingSeat.Seat.SeatRow}{bookingSeat.Seat.SeatCol}",
                    bookingSeat.Seat.SeatType?.TypeName.ToUpperInvariant() ?? "N/A",
                    ticket.QRCode,
                    contentId,
                    $"ticket-{booking.BookingCode}-{bookingSeat.Seat.SeatRow}{bookingSeat.Seat.SeatCol}.png"));
            }

            var showtime = booking.Showtime;
            var localStart = VietnamTime.FromUtc(showtime.StartTime);
            var paymentMethod = booking.Payment?.PaymentMethod ?? "N/A";
            var html = BookingEmailTemplate.BuildBookingConfirmed(
                booking.User.FullName,
                booking.BookingCode,
                showtime.Movie.Title,
                showtime.Movie.DurationMin,
                showtime.Room.RoomType?.TypeName ?? "N/A",
                $"{localStart:dd/MM/yyyy HH:mm}",
                showtime.Room.Cinema.CinemaName,
                showtime.Room.RoomName,
                $"{booking.FinalAmount:N0}",
                paymentMethod,
                ticketsHtml.ToString());

            await _emailQueue.EnqueueAsync(
                booking.UserID, booking.User.Email, $"booking_confirmed:{booking.BookingID}", "booking_confirmed",
                $"[CGV Premium] Xác nhận đặt vé thành công - {booking.BookingCode}",
                html, images, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to queue booking confirmation email for booking {BookingId}", bookingId);
        }
    }

    public async Task QueueRefundProcessedAsync(
        int bookingId,
        decimal refundAmount,
        DateTime completedAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
            if (booking?.User is null || string.IsNullOrWhiteSpace(booking.User.Email))
                return;

            var localStart = VietnamTime.FromUtc(booking.Showtime.StartTime);
            var localCompletedAt = VietnamTime.FromUtc(completedAt);
            var html = BookingEmailTemplate.BuildRefundProcessed(
                booking.User.FullName,
                booking.BookingCode,
                booking.Showtime.Movie.Title,
                $"{localStart:dd/MM/yyyy HH:mm}",
                $"{refundAmount:N0}",
                $"{localCompletedAt:dd/MM/yyyy HH:mm}");

            await _emailQueue.EnqueueAsync(
                booking.UserID, booking.User.Email, $"refund_processed:{booking.BookingID}", "refund_processed",
                $"[CGV Premium] Xác nhận hoàn tiền thành công - Mã đơn hàng {booking.BookingCode}",
                html, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to queue refund email for booking {BookingId}", bookingId);
        }
    }
}
