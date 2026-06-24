using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly CinemaBookingDbContext _db;

    public InvoiceRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Invoice> CreateInvoiceAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        await _db.Invoices.AddAsync(invoice, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return invoice;
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(
        int invoiceId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Invoices
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i => i.InvoiceID == invoiceId, cancellationToken);
    }

    public async Task<Invoice?> GetInvoiceByBookingIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Invoices
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i => i.BookingID == bookingId, cancellationToken);
    }

    public async Task<Invoice?> GetInvoiceByCodeAsync(
        string invoiceCode,
        CancellationToken cancellationToken = default)
    {
        return await _db.Invoices
            .Include(i => i.Booking)
            .FirstOrDefaultAsync(i => i.InvoiceCode == invoiceCode, cancellationToken);
    }
}
