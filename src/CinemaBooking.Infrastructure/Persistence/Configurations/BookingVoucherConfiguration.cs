using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class BookingVoucherConfiguration : IEntityTypeConfiguration<BookingVoucher>
{
    public void Configure(EntityTypeBuilder<BookingVoucher> builder)
    {
        builder.ToTable("BookingVoucher");

        builder.HasKey(bv => bv.BookingVoucherID);

        builder.Property(bv => bv.DiscountApplied).HasColumnType("decimal(18,2)");
        builder.Property(bv => bv.UsedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(bv => bv.BookingID).IsUnique().HasDatabaseName("UQ_BookingVoucher_BookingID");

        builder.HasOne(bv => bv.Booking)
            .WithOne(b => b.BookingVoucher)
            .HasForeignKey<BookingVoucher>(bv => bv.BookingID)
            .HasConstraintName("FK_BookingVoucher_Booking");

        builder.HasOne(bv => bv.Voucher)
            .WithMany(v => v.BookingVouchers)
            .HasForeignKey(bv => bv.VoucherID)
            .HasConstraintName("FK_BookingVoucher_Voucher");

        builder.ToTable(t => t.HasCheckConstraint("CK_BookingVoucher_DiscountApplied", "[DiscountApplied] >= 0"));
    }
}
