using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Persistence;
using CinemaBooking.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly CinemaBookingDbContext _db;

    public BookingRepository(CinemaBookingDbContext db)
    {
        _db = db;
    }

    public async Task<Showtime?> GetShowtimeAsync(
        int showtimeId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Showtimes
            .Include(s => s.Room)
                .ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(s => s.ShowtimeID == showtimeId, cancellationToken);
    }

    public async Task<List<Seat>> GetSeatsByIdsAsync(
        List<int> seatIds,
        CancellationToken cancellationToken = default)
    {
        return await _db.Seats
            .Include(s => s.SeatType)
            .Where(s => seatIds.Contains(s.SeatID) && s.IsCurrentLayout)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetUnavailableSeatIdsAsync(
        int showtimeId,
        List<int> seatIds,
        int currentUserId,
        CancellationToken cancellationToken = default)
    {
        var bookedSeatIds = await _db.BookingSeats
            .Where(bs => bs.Booking.ShowtimeID == showtimeId
                      && seatIds.Contains(bs.SeatID)
                      && (bs.Booking.Status == "pending"
                          || bs.Booking.Status == "paid"
                          || bs.Booking.Status == "used"))
            .Select(bs => bs.SeatID)
            .ToListAsync(cancellationToken);

        var heldByOthersSeatIds = await _db.SeatHolds
            .Where(h => h.ShowtimeID == showtimeId
                      && seatIds.Contains(h.SeatID)
                      && h.Status == "holding"
                      && h.ExpiresAt > DateTime.UtcNow
                      && h.UserID != currentUserId)
            .Select(h => h.SeatID)
            .ToListAsync(cancellationToken);

        return bookedSeatIds.Union(heldByOthersSeatIds).ToList();
    }

    public async Task<bool> TryAddSeatHoldsAsync(
        IEnumerable<SeatHold> seatHolds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SeatHolds.AddRangeAsync(seatHolds, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<List<SeatHold>> GetMyActiveHoldsAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default)
    {
        return await _db.SeatHolds
            .Where(h => h.UserID == userId
                      && h.ShowtimeID == showtimeId
                      && seatIds.Contains(h.SeatID)
                      && h.Status == "holding"
                      && h.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public Task<List<SeatHold>> GetMyActiveHoldsForUpdateAsync(
        int userId,
        int showtimeId,
        DateTime now,
        CancellationToken cancellationToken = default) =>
        _db.SeatHolds
            .FromSqlInterpolated($"""
                SELECT * FROM [SeatHold] WITH (UPDLOCK, HOLDLOCK)
                WHERE [UserID] = {userId}
                  AND [ShowtimeID] = {showtimeId}
                  AND [Status] = {SeatHoldStatus.Holding}
                  AND [ExpiresAt] > {now}
                """)
            .ToListAsync(cancellationToken);

    public async Task ReleaseSeatHoldsAsync(
        IEnumerable<SeatHold> seatHolds,
        CancellationToken cancellationToken = default)
    {
        foreach (var seatHold in seatHolds)
            seatHold.Status = SeatHoldStatus.Released;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddBookingAsync(
        Booking booking,
        CancellationToken cancellationToken = default)
    {
        await _db.Bookings.AddAsync(booking, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkHoldsAsConfirmedAsync(
        IEnumerable<SeatHold> seatHolds,
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        foreach (var hold in seatHolds)
        {
            hold.Status = "confirmed";
            hold.BookingID = bookingId;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Booking?> GetBookingByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .Include(b => b.BookingFnBs).ThenInclude(fnb => fnb.Product)
            .Include(b => b.BookingVoucher!).ThenInclude(bv => bv.Voucher)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.BookingID == bookingId, cancellationToken);
    }

    public async Task<Booking?> GetBookingByQRCodeAsync(
        string qrCode,
        CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .FirstOrDefaultAsync(b => b.QRCode == qrCode, cancellationToken);
    }

    public async Task<Booking?> GetBookingWithFullDetailsForCheckInAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .Include(b => b.User)
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.RoomType)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat).ThenInclude(s => s.SeatType)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Ticket)
            .Include(b => b.BookingFnBs).ThenInclude(fnb => fnb.Product)
            .Include(b => b.Payment)
            .Include(b => b.Refunds)
            .AsSplitQuery()
            .FirstOrDefaultAsync(b => b.BookingID == bookingId, cancellationToken);
    }

    public async Task<int?> GetStaffCinemaIdAsync(
        int staffId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Where(u => u.UserID == staffId)
            .Select(u => u.CinemaID)
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task<bool> PerformCheckInAsync(
        int bookingId,
        int staffId,
        string? ipAddress,
        DateTime checkedInAt,
        CancellationToken cancellationToken = default)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var booking = await _db.Bookings
                .Include(b => b.BookingSeats)
                .FirstOrDefaultAsync(b => b.BookingID == bookingId, cancellationToken);

            if (booking is null)
                return false;

            var tickets = await _db.Tickets
                .Where(t => booking.BookingSeats.Select(bs => bs.BookingSeatID).Contains(t.BookingSeatID))
                .ToListAsync(cancellationToken);

            foreach (var ticket in tickets)
            {
                ticket.Status = TicketStatus.Used;
                ticket.CheckedInAt = checkedInAt;
                ticket.CheckedInByID = staffId;
            }

            booking.Status = BookingStatus.Used;
            booking.CheckedInAt = checkedInAt;
            booking.CheckedInByUserId = staffId;
            booking.UpdatedAt = checkedInAt;

            var auditLog = new AdminActionLog
            {
                AdminID = staffId,
                TargetTable = "Booking",
                TargetID = booking.BookingID,
                ActionType = AdminActionTypes.CheckIn,
                Description = $"Staff checked in booking {booking.BookingCode}",
                IPAddress = ipAddress ?? "unknown",
                CreatedAt = checkedInAt
            };

            await _db.AdminActionLogs.AddAsync(auditLog, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }
    }

    public async Task<(List<Booking> Bookings, int TotalCount)> GetCheckInHistoryAsync(
        int? staffId,
        int? cinemaId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Bookings
            .Include(b => b.User)
            .Include(b => b.CheckedInByUser)
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .Include(b => b.BookingSeats)
            .Where(b => b.CheckedInAt != null);

        if (staffId.HasValue)
            query = query.Where(b => b.CheckedInByUserId == staffId.Value);

        if (cinemaId.HasValue)
            query = query.Where(b => b.Showtime.Room.CinemaID == cinemaId.Value);

        if (from.HasValue)
            query = query.Where(b => b.CheckedInAt >= from.Value);

        if (to.HasValue)
            query = query.Where(b => b.CheckedInAt <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var bookings = await query
            .OrderByDescending(b => b.CheckedInAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (bookings, totalCount);
    }

    public async Task UpdateBookingQRCodeAsync(
        int bookingId,
        string qrCode,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings.FindAsync(new object[] { bookingId }, cancellationToken);
        if (booking is not null)
        {
            booking.QRCode = qrCode;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<Booking>> GetBookingsByUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .Include(b => b.BookingSeats).ThenInclude(bs => bs.Seat)
            .Include(b => b.BookingFnBs).ThenInclude(fnb => fnb.Product)
            .Include(b => b.BookingVoucher!).ThenInclude(bv => bv.Voucher)
            .AsSplitQuery()
            .Where(b => b.UserID == userId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> GetProductsByIdsAsync(
        List<int> productIds,
        CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .Where(p => productIds.Contains(p.ItemID))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> GetAvailableProductsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Products
            .Where(p => p.Status == "active")
            .OrderBy(p => p.ItemType)
            .ThenBy(p => p.ItemName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Voucher?> GetVoucherByCodeAsync(
        string voucherCode,
        CancellationToken cancellationToken = default)
    {
        return await _db.Vouchers
            .FirstOrDefaultAsync(v => v.VoucherCode == voucherCode, cancellationToken);
    }

    public async Task IncrementVoucherUsageAsync(
        int voucherId,
        CancellationToken cancellationToken = default)
    {
        var voucher = await _db.Vouchers.FindAsync(new object[] { voucherId }, cancellationToken);
        if (voucher is not null)
        {
            voucher.UsedCount++;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ExtendBookingHoldsAsync(
        int bookingId,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        await _db.SeatHolds
            .Where(hold => hold.BookingID == bookingId && hold.Status == "confirmed")
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(hold => hold.ExpiresAt, expiresAt),
                cancellationToken);
    }

    public Task<bool> HasActiveBookingHoldsAsync(
        int bookingId,
        DateTime now,
        CancellationToken cancellationToken = default) =>
        _db.SeatHolds.AnyAsync(
            hold => hold.BookingID == bookingId
                && hold.Status == "confirmed"
                && hold.ExpiresAt > now,
            cancellationToken);

    public async Task UpdateBookingStatusAsync(
        int bookingId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.BookingID == bookingId, cancellationToken);

        if (booking is null)
            throw new InvalidOperationException($"Booking {bookingId} not found");

        booking.Status = status;
        booking.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }


    public async Task<Voucher?> GetVoucherByCodeWithLockAsync(
        string voucherCode,
        CancellationToken cancellationToken = default)
    {
        return await _db.Vouchers
            .FromSqlRaw("SELECT * FROM Voucher WITH (UPDLOCK, ROWLOCK) WHERE VoucherCode = {0}", voucherCode)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Database.BeginTransactionAsync(cancellationToken);
    }
}
