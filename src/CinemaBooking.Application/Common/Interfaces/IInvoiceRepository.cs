using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice> CreateInvoiceAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default);

    Task<Invoice?> GetInvoiceByIdAsync(
        int invoiceId,
        CancellationToken cancellationToken = default);

    Task<Invoice?> GetInvoiceByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<Invoice?> GetInvoiceByCodeAsync(
        string invoiceCode,
        CancellationToken cancellationToken = default);
}
