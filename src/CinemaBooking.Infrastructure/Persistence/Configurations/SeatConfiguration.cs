using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("Seat");

        builder.HasKey(s => s.SeatID);

        builder.Property(s => s.SeatRow).HasMaxLength(5).IsRequired();
        builder.Property(s => s.Status).HasMaxLength(20).IsRequired().HasDefaultValue("active");

        builder.HasIndex(s => s.RoomID).HasDatabaseName("IX_Seat_RoomID");
        builder.HasIndex(s => new { s.RoomID, s.SeatRow, s.SeatCol })
            .IsUnique()
            .HasDatabaseName("UQ_Seat_RoomID_SeatRow_SeatCol");

        builder.HasOne(s => s.Room)
            .WithMany(r => r.Seats)
            .HasForeignKey(s => s.RoomID)
            .HasConstraintName("FK_Seat_Room");

        builder.HasOne(s => s.SeatType)
            .WithMany(st => st.Seats)
            .HasForeignKey(s => s.SeatTypeID)
            .HasConstraintName("FK_Seat_SeatType");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Seat_SeatCol", "[SeatCol] > 0");
            t.HasCheckConstraint("CK_Seat_Status", "[Status] IN ('active', 'inactive')");
        });
    }
}
