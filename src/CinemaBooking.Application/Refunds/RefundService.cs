using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Application.Notifications;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Application.Refunds;

public sealed class RefundService : IRefundService
{
    private const int MinimumMinutesBeforeShowtime = 30;

    private readonly IRefundRepository _refundRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationOutbox _notificationOutbox;

    public RefundService(
        IRefundRepository refundRepository,
        IWalletRepository walletRepository,
        IPaymentRepository paymentRepository,
        ITicketRepository ticketRepository,
        IBookingRepository bookingRepository,
        INotificationOutbox notificationOutbox,
        IUnitOfWork unitOfWork)
    {
        _refundRepository = refundRepository;
        _walletRepository = walletRepository;
        _paymentRepository = paymentRepository;
        _ticketRepository = ticketRepository;
        _bookingRepository = bookingRepository;
        _notificationOutbox = notificationOutbox;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<Refund>> GetRefundHistoryAsync(
        int userId,
        bool isStaffOrAdmin,
        CancellationToken cancellationToken = default)
    {
        if (isStaffOrAdmin)
            return await _refundRepository.GetAllRefundsAsync(cancellationToken);

        return await _refundRepository.GetRefundsByUserIdAsync(userId, cancellationToken);
    }

    public async Task<Refund?> GetRefundByIdAsync(
        int refundId,
        int userId,
        bool isStaffOrAdmin,
        CancellationToken cancellationToken = default)
    {
        var refund = await _refundRepository.GetRefundByIdAsync(refundId, cancellationToken);

        if (refund is null)
            return null;

        if (!isStaffOrAdmin && refund.Booking.UserID != userId)
            return null;

        return refund;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, RefundResult? Result)> ProcessRefundAsync(
        int bookingId,
        string reason,
        int requestedBy,
        bool isStaff,
        CancellationToken cancellationToken = default)
    {
        var booking = await _refundRepository.GetBookingForRefundAsync(bookingId, cancellationToken);
        if (booking is null)
            return (false, "Booking not found.", null);

        if (!isStaff && booking.UserID != requestedBy)
            return (false, "Forbidden.", null);

        if (booking.Payment is null)
            return (false, "Booking has no payment record.", null);

        if (booking.Payment.Status == PaymentStatus.Refunded)
            return (false, "Booking has already been refunded.", null);

        if (booking.Payment.Status != PaymentStatus.Completed)
            return (false, "Booking has not been paid.", null);

        var hasUsedTicket = booking.BookingSeats
            .Any(bs => bs.Ticket is not null && bs.Ticket.Status == "used");
        if (hasUsedTicket)
            return (false, "Ticket has already been used.", null);

        if (booking.Status == BookingStatus.Cancelled)
            return (false, "Booking has been cancelled.", null);

        var now = DateTime.UtcNow;
        var minutesUntilShowtime = (booking.Showtime.StartTime - now).TotalMinutes;
        if (minutesUntilShowtime <= MinimumMinutesBeforeShowtime)
            return (false, "Refund period has expired.", null);

        if (booking.User?.LoyaltyTier is not null)
        {
            var refundCount = await _refundRepository.CountCompletedRefundsInCurrentMonthAsync(
                booking.UserID!.Value, cancellationToken);

            if (refundCount >= booking.User.LoyaltyTier.MaxRefundPerMonth)
                return (false, "You have reached your monthly refund limit.", null);
        }

        var wallet = await _walletRepository.GetWalletByUserIdAsync(
            booking.UserID!.Value, cancellationToken);
        if (wallet is null)
            return (false, "Wallet not found.", null);

        var result = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!await _paymentRepository.TryMarkCompletedPaymentAsRefundedAsync(
                    booking.Payment.PaymentID,
                    booking.FinalAmount,
                    reason,
                    requestedBy,
                    now,
                    cancellationToken))
                return null;

            var refund = new Refund
            {
                BookingID = bookingId,
                PaymentID = booking.Payment.PaymentID,
                WalletID = wallet.WalletID,
                Amount = booking.FinalAmount,
                Reason = reason,
                Status = "pending",
                RequestedAt = now
            };
            await _refundRepository.CreateRefundAsync(refund, cancellationToken);

            await _walletRepository.AddBalanceAsync(
                booking.UserID.Value,
                refund.Amount,
                cancellationToken);

            var walletAfterRefund = await _walletRepository.GetWalletByUserIdAsync(
                booking.UserID.Value, cancellationToken);

            var transaction = new WalletTransaction
            {
                WalletID = wallet.WalletID,
                Amount = refund.Amount,
                BalanceAfter = walletAfterRefund!.Balance,
                TransactionType = WalletTransactionType.Refund,
                BookingID = bookingId,
                RefundID = refund.RefundID,
                Description = $"Refund booking {booking.BookingCode}",
                CreatedAt = now
            };
            await _walletRepository.CreateTransactionAsync(transaction, cancellationToken);

            await _bookingRepository.UpdateBookingStatusAsync(
                bookingId,
                BookingStatus.Refunded,
                cancellationToken);

            await _ticketRepository.UpdateTicketsStatusByBookingAsync(
                bookingId,
                "cancelled",
                cancellationToken);

            await _refundRepository.UpdateRefundStatusAsync(
                refund.RefundID,
                "completed",
                now,
                requestedBy,
                cancellationToken);

            return new RefundResult
            {
                RefundId = refund.RefundID,
                RefundAmount = refund.Amount,
                WalletBalance = walletAfterRefund.Balance,
                Status = BookingStatus.Refunded
            };
        }, cancellationToken);

        if (result is null)
            return (false, "Booking has already been refunded.", null);

        await _notificationOutbox.EnqueueRefundCompletedAsync(
            bookingId, result.RefundAmount, now, cancellationToken);

        return (true, null, result);
    }
}
