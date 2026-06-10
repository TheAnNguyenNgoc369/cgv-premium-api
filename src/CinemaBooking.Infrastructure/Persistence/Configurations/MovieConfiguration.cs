using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.ToTable("Movie");

        builder.HasKey(m => m.MovieID);

        builder.Property(m => m.Title).HasMaxLength(200).IsRequired();
        builder.Property(m => m.AgeRating).HasMaxLength(10).IsRequired();
        builder.Property(m => m.Director).HasMaxLength(100);
        builder.Property(m => m.PosterURL).HasMaxLength(500);
        builder.Property(m => m.TrailerURL).HasMaxLength(500);
        builder.Property(m => m.Status).HasMaxLength(30).IsRequired().HasDefaultValue("coming_soon");
        builder.Property(m => m.CreatedAt).HasDefaultValueSql("GETDATE()");
        builder.Property(m => m.UpdatedAt).HasDefaultValueSql("GETDATE()");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Movie_AgeRating", "[AgeRating] IN ('P', 'C13', 'C16', 'C18')");
            t.HasCheckConstraint("CK_Movie_DurationMin", "[DurationMin] > 0");
            t.HasCheckConstraint("CK_Movie_Status", "[Status] IN ('coming_soon', 'now_showing', 'ended')");
            t.HasCheckConstraint("CK_Movie_ShowingDate", "[ShowingFrom] IS NULL OR [ShowingTo] IS NULL OR [ShowingFrom] <= [ShowingTo]");
        });
    }
}
