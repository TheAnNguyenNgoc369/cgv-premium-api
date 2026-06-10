using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class BookingSeatConfiguration : IEntityTypeConfiguration<BookingSeat>
{
    public void Configure(EntityTypeBuilder<BookingSeat> builder)
    {
        builder.ToTable("BookingSeat");

        builder.HasKey(bs => bs.BookingSeatID);

        builder.Property(bs => bs.TicketPrice).HasColumnType("decimal(18,2)");

        builder.HasIndex(bs => new { bs.BookingID, bs.SeatID })
            .IsUnique()
            .HasDatabaseName("UQ_BookingSeat_BookingID_SeatID");

        builder.HasOne(bs => bs.Booking)
            .WithMany(b => b.BookingSeats)
            .HasForeignKey(bs => bs.BookingID)
            .HasConstraintName("FK_BookingSeat_Booking");

        builder.HasOne(bs => bs.Seat)
            .WithMany(s => s.BookingSeats)
            .HasForeignKey(bs => bs.SeatID)
            .HasConstraintName("FK_BookingSeat_Seat");

        builder.ToTable(t => t.HasCheckConstraint("CK_BookingSeat_TicketPrice", "[TicketPrice] >= 0"));
    }
}
