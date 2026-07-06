using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IRefundRepository
{
    Task<Refund> CreateRefundAsync(
        Refund refund,
        CancellationToken cancellationToken = default);

    Task<Refund?> GetRefundByIdAsync(
        int refundId,
        CancellationToken cancellationToken = default);

    Task<List<Refund>> GetRefundsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<List<Refund>> GetAllRefundsAsync(
        CancellationToken cancellationToken = default);

    Task UpdateRefundStatusAsync(
        int refundId,
        string status,
        DateTime processedAt,
        int processedBy,
        CancellationToken cancellationToken = default);

    Task<int> CountCompletedRefundsInCurrentMonthAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<Booking?> GetBookingForRefundAsync(
        int bookingId,
        CancellationToken cancellationToken = default);
}
