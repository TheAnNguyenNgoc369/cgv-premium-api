using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Room");

        builder.HasKey(r => r.RoomID);

        builder.Property(r => r.RoomName).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Capacity).HasDefaultValue(0);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.Status).HasMaxLength(20).IsRequired().HasDefaultValue("active");
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(r => r.CinemaID).HasDatabaseName("IX_Room_CinemaID");
        builder.HasIndex(r => new { r.CinemaID, r.RoomName })
            .IsUnique()
            .HasDatabaseName("UQ_Room_CinemaID_RoomName");

        builder.HasOne(r => r.Cinema)
            .WithMany(c => c.Rooms)
            .HasForeignKey(r => r.CinemaID)
            .HasConstraintName("FK_Room_Cinema");

        builder.HasOne(r => r.RoomType)
            .WithMany(rt => rt.Rooms)
            .HasForeignKey(r => r.RoomTypeID)
            .HasConstraintName("FK_Room_RoomType");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Room_Capacity", "[Capacity] >= 0");
            t.HasCheckConstraint("CK_Room_Status", "[Status] IN ('active', 'maintenance', 'inactive')");
        });
    }
}
