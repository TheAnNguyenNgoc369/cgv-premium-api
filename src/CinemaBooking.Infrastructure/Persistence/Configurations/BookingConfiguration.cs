using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Booking");

        builder.HasKey(b => b.BookingID);

        builder.Property(b => b.BookingCode).HasMaxLength(50).IsRequired();
        builder.Property(b => b.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(b => b.DiscountAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(b => b.FinalAmount).HasColumnType("decimal(18,2)");
        builder.Property(b => b.PointsEarned).HasDefaultValue(0);
        builder.Property(b => b.PointsRedeemed).HasDefaultValue(0);
        builder.Property(b => b.Status).HasMaxLength(30).IsRequired().HasDefaultValue("pending");
        builder.Property(b => b.QRCode).HasMaxLength(100);
        builder.Property(b => b.BookingDate).HasDefaultValueSql("GETDATE()");
        builder.Property(b => b.UpdatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(b => b.BookingCode).IsUnique().HasDatabaseName("UQ_Booking_BookingCode");
        builder.HasIndex(b => b.QRCode).IsUnique().HasDatabaseName("UQ_Booking_QRCode");
        builder.HasIndex(b => b.UserID).HasDatabaseName("IX_Booking_UserID");
        builder.HasIndex(b => b.ShowtimeID).HasDatabaseName("IX_Booking_ShowtimeID");
        builder.HasIndex(b => b.Status).HasDatabaseName("IX_Booking_Status");

        builder.HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserID)
            .IsRequired(false)
            .HasConstraintName("FK_Booking_Users");

        builder.HasOne(b => b.Showtime)
            .WithMany(st => st.Bookings)
            .HasForeignKey(b => b.ShowtimeID)
            .HasConstraintName("FK_Booking_Showtime");

        builder.HasOne(b => b.CreatedByStaff)
            .WithMany(u => u.StaffBookings)
            .HasForeignKey(b => b.CreatedByStaffID)
            .HasConstraintName("FK_Booking_CreatedByStaff");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Booking_Amounts", "[SubTotal] >= 0 AND [DiscountAmount] >= 0 AND [FinalAmount] >= 0");
            t.HasCheckConstraint("CK_Booking_Points", "ISNULL([PointsEarned],0) >= 0 AND ISNULL([PointsRedeemed],0) >= 0");
            t.HasCheckConstraint("CK_Booking_Status", "[Status] IN ('pending', 'paid', 'cancelled', 'refunded', 'used', 'expired', 'payment_failed', 'partially_refunded', 'no_show')");
        });
    }
}
