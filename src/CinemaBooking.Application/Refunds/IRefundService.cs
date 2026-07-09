using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Refunds;

public interface IRefundService
{
    Task<(bool Succeeded, string? ErrorMessage, RefundResult? Result)> ProcessRefundAsync(
        int bookingId,
        string reason,
        int requestedBy,
        bool isStaff,
        CancellationToken cancellationToken = default);

    Task<List<Refund>> GetRefundHistoryAsync(
        int userId,
        bool isStaffOrAdmin,
        CancellationToken cancellationToken = default);

    Task<Refund?> GetRefundByIdAsync(
        int refundId,
        int userId,
        bool isStaffOrAdmin,
        CancellationToken cancellationToken = default);
}
