using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class ShowtimeConfiguration : IEntityTypeConfiguration<Showtime>
{
    public void Configure(EntityTypeBuilder<Showtime> builder)
    {
        builder.ToTable("Showtime");

        builder.HasKey(s => s.ShowtimeID);

        builder.Property(s => s.BasePrice).HasColumnType("decimal(18,2)");
        builder.Property(s => s.Status).HasMaxLength(30).IsRequired().HasDefaultValue("scheduled");
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(s => s.MovieID).HasDatabaseName("IX_Showtime_MovieID");
        builder.HasIndex(s => new { s.RoomID, s.StartTime }).HasDatabaseName("IX_Showtime_RoomID_StartTime");
        builder.HasIndex(s => new { s.RoomID, s.StartTime, s.EndTime }).HasDatabaseName("IX_Showtime_Conflict_Check");

        builder.HasOne(s => s.Movie)
            .WithMany(m => m.Showtimes)
            .HasForeignKey(s => s.MovieID)
            .HasConstraintName("FK_Showtime_Movie");

        builder.HasOne(s => s.Room)
            .WithMany(r => r.Showtimes)
            .HasForeignKey(s => s.RoomID)
            .HasConstraintName("FK_Showtime_Room");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Showtime_Time", "[EndTime] > [StartTime]");
            t.HasCheckConstraint("CK_Showtime_BasePrice", "[BasePrice] >= 0");
            t.HasCheckConstraint("CK_Showtime_Status", "[Status] IN ('scheduled', 'ongoing', 'completed', 'cancelled')");
        });
    }
}
