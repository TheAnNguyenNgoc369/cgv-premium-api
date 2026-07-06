using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CinemaBooking.Shared.Constants;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly CinemaBookingDbContext _db;

    public PaymentRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Payment> CreatePaymentAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        await _db.Payments.AddAsync(payment, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment?> GetPaymentByIdAsync(
        int paymentId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.PaymentID == paymentId, cancellationToken);
    }

    public async Task<Payment?> GetPaymentByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.BookingID == bookingId, cancellationToken);
    }

    public async Task UpdatePaymentStatusAsync(
        int paymentId,
        string status,
        DateTime? paidAt,
        string? transactionCode = null,
        CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.PaymentID == paymentId, cancellationToken);

        if (payment is null)
            throw new InvalidOperationException($"Payment {paymentId} not found");

        payment.Status = status;
        if (paidAt.HasValue)
            payment.PaidAt = paidAt.Value;
        if (!string.IsNullOrEmpty(transactionCode))
            payment.TransactionCode = transactionCode;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryCompletePendingPaymentAsync(
        int paymentId,
        DateTime paidAt,
        string transactionCode,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _db.Payments
            .Where(payment => payment.PaymentID == paymentId
                && payment.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payment => payment.Status, PaymentStatus.Completed)
                .SetProperty(payment => payment.PaidAt, paidAt)
                .SetProperty(payment => payment.TransactionCode, transactionCode),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<PaymentSession> CreatePaymentSessionAsync(
        PaymentSession session,
        CancellationToken cancellationToken = default)
    {
        await _db.PaymentSessions.AddAsync(session, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<PaymentSession?> GetPaymentSessionByIdAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _db.PaymentSessions
            .Include(ps => ps.Payment)
            .FirstOrDefaultAsync(ps => ps.SessionID == sessionId, cancellationToken);
    }

    public async Task<PaymentSession?> GetPaymentSessionByOrderNoAsync(
        string orderNo,
        CancellationToken cancellationToken = default)
    {
        return await _db.PaymentSessions
            .Include(ps => ps.Payment)
            .FirstOrDefaultAsync(ps => ps.GatewayOrderNo == orderNo, cancellationToken);
    }

    public async Task UpdatePaymentSessionStatusAsync(
        int sessionId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var session = await _db.PaymentSessions
            .FirstOrDefaultAsync(ps => ps.SessionID == sessionId, cancellationToken);

        if (session is null)
            throw new InvalidOperationException($"PaymentSession {sessionId} not found");

        session.Status = status;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePaymentSessionsForPaymentAsync(
        int paymentId,
        string status,
        CancellationToken cancellationToken = default)
    {
        await _db.PaymentSessions
            .Where(session => session.PaymentID == paymentId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(session => session.Status, status),
                cancellationToken);
    }

    public async Task ResetPaymentForRetryAsync(
        int paymentId,
        string paymentMethod,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments.FindAsync(new object[] { paymentId }, cancellationToken)
            ?? throw new InvalidOperationException($"Payment {paymentId} not found");
        payment.PaymentMethod = paymentMethod;
        payment.Amount = amount;
        payment.Status = PaymentStatus.Pending;
        payment.PaidAt = null;
        payment.TransactionCode = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePaymentForRefundAsync(
        int paymentId,
        decimal refundAmount,
        string refundReason,
        int refundedBy,
        CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments.FindAsync(new object[] { paymentId }, cancellationToken)
            ?? throw new InvalidOperationException($"Payment {paymentId} not found");

        payment.Status = PaymentStatus.Refunded;
        payment.RefundReason = refundReason;
        payment.RefundAmount = refundAmount;
        payment.RefundedAt = DateTime.UtcNow;
        payment.RefundedBy = refundedBy;

        await _db.SaveChangesAsync(cancellationToken);
    }
}
