using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class SeatHoldConfiguration : IEntityTypeConfiguration<SeatHold>
{
    public void Configure(EntityTypeBuilder<SeatHold> builder)
    {
        builder.ToTable("SeatHold");

        builder.HasKey(h => h.HoldID);

        builder.Property(h => h.Status).HasMaxLength(20).IsRequired().HasDefaultValue("holding");
        builder.Property(h => h.HeldAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(h => new { h.SeatID, h.ShowtimeID, h.Status })
            .HasDatabaseName("IX_SeatHold_Status_Lookup");

        builder.HasIndex(h => new { h.SeatID, h.ShowtimeID })
            .IsUnique()
            .HasFilter("[Status] = 'holding'")
            .HasDatabaseName("UQ_SeatHold_ActiveHolding");

        builder.HasOne(h => h.Seat)
            .WithMany(s => s.SeatHolds)
            .HasForeignKey(h => h.SeatID)
            .HasConstraintName("FK_SeatHold_Seat");

        builder.HasOne(h => h.Showtime)
            .WithMany(st => st.SeatHolds)
            .HasForeignKey(h => h.ShowtimeID)
            .HasConstraintName("FK_SeatHold_Showtime");

        builder.HasOne(h => h.User)
            .WithMany(u => u.SeatHolds)
            .HasForeignKey(h => h.UserID)
            .HasConstraintName("FK_SeatHold_Users");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SeatHold_Status", "[Status] IN ('holding', 'confirmed', 'released', 'expired')");
            t.HasCheckConstraint("CK_SeatHold_ExpiresAt", "[ExpiresAt] > [HeldAt]");
        });
    }
}
