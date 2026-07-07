using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CinemaBooking.Infrastructure.Persistence;

public class CinemaBookingDbContext : DbContext
{
    public CinemaBookingDbContext(DbContextOptions<CinemaBookingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cinema> Cinemas => Set<Cinema>();
    public DbSet<LoyaltyTier> LoyaltyTiers => Set<LoyaltyTier>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Movie> Movie => Set<Movie>();
    public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<SeatType> SeatTypes => Set<SeatType>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<ShowtimeType> ShowtimeTypes => Set<ShowtimeType>();
    public DbSet<ShowtimeTypeSlot> ShowtimeTypeSlots => Set<ShowtimeTypeSlot>();
    public DbSet<SeatHold> SeatHolds => Set<SeatHold>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSeat> BookingSeats => Set<BookingSeat>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<BookingFnB> BookingFnBs => Set<BookingFnB>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentSession> PaymentSessions => Set<PaymentSession>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<BookingVoucher> BookingVouchers => Set<BookingVoucher>();
    public DbSet<LoyaltyPoints> LoyaltyPoints => Set<LoyaltyPoints>();
    public DbSet<UserVoucher> UserVouchers => Set<UserVoucher>();
    public DbSet<AdminActionLog> AdminActionLogs => Set<AdminActionLog>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CinemaBookingDbContext).Assembly);

        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
        }
    }
}
