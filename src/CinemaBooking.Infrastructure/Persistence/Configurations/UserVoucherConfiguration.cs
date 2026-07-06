using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class UserVoucherConfiguration : IEntityTypeConfiguration<UserVoucher>
{
    public void Configure(EntityTypeBuilder<UserVoucher> builder)
    {
        builder.ToTable("UserVoucher");

        builder.HasKey(uv => uv.UserVoucherID);

        builder.Property(uv => uv.Status).HasMaxLength(20).IsRequired();
        builder.Property(uv => uv.RedeemedAt).IsRequired();
        builder.Property(uv => uv.ExpiredAt).IsRequired();

        builder.HasIndex(uv => uv.UserID).HasDatabaseName("IX_UserVoucher_UserID");
        builder.HasIndex(uv => uv.VoucherID).HasDatabaseName("IX_UserVoucher_VoucherID");
        builder.HasIndex(uv => uv.Status).HasDatabaseName("IX_UserVoucher_Status");

        builder.HasOne(uv => uv.User)
            .WithMany(u => u.UserVouchers)
            .HasForeignKey(uv => uv.UserID)
            .HasConstraintName("FK_UserVoucher_Users")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uv => uv.Voucher)
            .WithMany(v => v.UserVouchers)
            .HasForeignKey(uv => uv.VoucherID)
            .HasConstraintName("FK_UserVoucher_Voucher")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uv => uv.Booking)
            .WithMany()
            .HasForeignKey(uv => uv.BookingID)
            .HasConstraintName("FK_UserVoucher_Booking")
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_UserVoucher_Status", "[Status] IN ('available', 'used', 'expired')");
            t.HasCheckConstraint("CK_UserVoucher_Dates", "[ExpiredAt] >= [RedeemedAt]");
        });
    }
}
