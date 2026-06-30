using CinemaBooking.Application.Contracts.Invoice;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Invoices;

public sealed class InvoiceService : IInvoiceService
{
    private const decimal TaxRate = 0.10m;

    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IBookingRepository _bookingRepository;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IBookingRepository bookingRepository)
    {
        _invoiceRepository = invoiceRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<InvoiceResponse> CreateInvoiceAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
        if (booking is null)
            throw new InvalidOperationException($"Booking {bookingId} not found");

        var existingInvoice = await _invoiceRepository.GetInvoiceByBookingIdAsync(bookingId, cancellationToken);
        if (existingInvoice is not null)
            throw new InvalidOperationException($"Invoice already exists for booking {bookingId}");

        var totalAmount = booking.FinalAmount;
        var taxAmount = CalculateTaxAmount(totalAmount);
        var invoiceCode = GenerateInvoiceCode(bookingId);

        var invoice = await _invoiceRepository.CreateInvoiceAsync(
            new Invoice
            {
                BookingID = bookingId,
                InvoiceCode = invoiceCode,
                TotalAmount = totalAmount,
                TaxAmount = taxAmount,
                IssuedAt = DateTime.UtcNow
            },
            cancellationToken);

        return MapToInvoiceResponse(invoice);
    }

    public async Task<InvoiceResponse?> GetInvoiceByIdAsync(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetInvoiceByIdAsync(invoiceId, cancellationToken);
        return invoice is not null ? MapToInvoiceResponse(invoice) : null;
    }

    public async Task<InvoiceResponse?> GetInvoiceByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetInvoiceByBookingIdAsync(bookingId, cancellationToken);
        return invoice is not null ? MapToInvoiceResponse(invoice) : null;
    }

    public async Task<InvoiceResponse?> GetInvoiceByCodeAsync(
        string invoiceCode,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetInvoiceByCodeAsync(invoiceCode, cancellationToken);
        return invoice is not null ? MapToInvoiceResponse(invoice) : null;
    }

    private static InvoiceResponse MapToInvoiceResponse(Invoice invoice)
    {
        return new InvoiceResponse(
            InvoiceId: invoice.InvoiceID,
            BookingId: invoice.BookingID,
            InvoiceCode: invoice.InvoiceCode,
            TotalAmount: invoice.TotalAmount,
            TaxAmount: invoice.TaxAmount,
            IssuedAt: invoice.IssuedAt
        );
    }

    private static decimal CalculateTaxAmount(decimal totalAmount)
    {
        return Math.Round(totalAmount * (TaxRate / (1 + TaxRate)), 2);
    }

    private static string GenerateInvoiceCode(int bookingId)
    {
        return $"INV{bookingId:D8}{DateTime.UtcNow:yyMMddHHmmss}";
    }
}
