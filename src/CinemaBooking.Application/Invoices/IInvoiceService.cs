using CinemaBooking.Application.Contracts.Invoice;

namespace CinemaBooking.Application.Invoices;

public interface IInvoiceService
{
    Task<InvoiceResponse> CreateInvoiceAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponse?> GetInvoiceByIdAsync(
        int invoiceId,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponse?> GetInvoiceByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponse?> GetInvoiceByCodeAsync(
        string invoiceCode,
        CancellationToken cancellationToken = default);
}
