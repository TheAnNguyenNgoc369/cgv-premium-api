namespace CinemaBooking.Application.Contracts.Invoice;

public sealed record InvoiceResponse(
    int InvoiceId,
    int BookingId,
    string InvoiceCode,
    decimal TotalAmount,
    decimal TaxAmount,
    DateTime IssuedAt
);
