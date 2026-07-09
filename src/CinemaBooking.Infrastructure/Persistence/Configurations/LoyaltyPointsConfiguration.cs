using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class LoyaltyPointsConfiguration : IEntityTypeConfiguration<LoyaltyPoints>
{
    public void Configure(EntityTypeBuilder<LoyaltyPoints> builder)
    {
        builder.ToTable("LoyaltyPoints");

        builder.HasKey(p => p.PointID);

        builder.Property(p => p.TransactionType).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(255);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(p => p.UserID).HasDatabaseName("IX_LoyaltyPoints_UserID");

        builder.HasIndex(p => p.BookingID)
            .IsUnique()
            .HasDatabaseName("UQ_LoyaltyPoints_BookingID_Earned")
            .HasFilter("[TransactionType] = 'earn' AND [BookingID] IS NOT NULL");

        builder.HasOne(p => p.User)
            .WithMany(u => u.LoyaltyPoints)
            .HasForeignKey(p => p.UserID)
            .HasConstraintName("FK_LoyaltyPoints_Users");

        builder.HasOne(p => p.Booking)
            .WithMany(b => b.LoyaltyPoints)
            .HasForeignKey(p => p.BookingID)
            .HasConstraintName("FK_LoyaltyPoints_Booking");

        builder.HasOne(p => p.Voucher)
            .WithMany(v => v.LoyaltyPointsTransactions)
            .HasForeignKey(p => p.VoucherID)
            .HasConstraintName("FK_LoyaltyPoints_Voucher")
            .OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t => t.HasCheckConstraint("CK_LoyaltyPoints_TransactionType", "[TransactionType] IN ('earn', 'redeem', 'expire', 'adjust', 'exchange')"));
    }
}
