using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class BookingFnBConfiguration : IEntityTypeConfiguration<BookingFnB>
{
    public void Configure(EntityTypeBuilder<BookingFnB> builder)
    {
        builder.ToTable("BookingFnB");

        builder.HasKey(b => b.BookingFnBID);

        builder.Property(b => b.Quantity).HasDefaultValue(1);
        builder.Property(b => b.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Property(b => b.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(b => b.PickedUp).HasDefaultValue(false);
        builder.Property(b => b.PickedUpAt);
        builder.Property(b => b.PickedUpByStaffId);

        builder.HasIndex(b => new { b.BookingID, b.ItemID })
            .IsUnique()
            .HasDatabaseName("UQ_BookingFnB_BookingID_ItemID");

        builder.HasIndex(b => b.BookingID)
            .HasDatabaseName("IX_BookingFnB_BookingId");

        builder.HasOne(b => b.Booking)
            .WithMany(bk => bk.BookingFnBs)
            .HasForeignKey(b => b.BookingID)
            .HasConstraintName("FK_BookingFnB_Booking");

        builder.HasOne(b => b.Product)
            .WithMany(p => p.BookingFnBs)
            .HasForeignKey(b => b.ItemID)
            .HasConstraintName("FK_BookingFnB_Product");

        builder.HasOne(b => b.PickedUpByStaff)
            .WithMany()
            .HasForeignKey(b => b.PickedUpByStaffId)
            .HasConstraintName("FK_BookingFnB_PickedUpByStaff");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_BookingFnB_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_BookingFnB_Price", "[UnitPrice] >= 0 AND [SubTotal] >= 0");
        });
    }
}
