using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class MovieReviewConfiguration : IEntityTypeConfiguration<MovieReview>
{
    public void Configure(EntityTypeBuilder<MovieReview> builder)
    {
        builder.ToTable("MovieReviews");

        builder.HasKey(r => r.ReviewId);

        builder.Property(r => r.MovieId).IsRequired();
        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.BookingId).IsRequired();
        builder.Property(r => r.Rating).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(2000);
        builder.Property(r => r.IsHidden).IsRequired().HasDefaultValue(false);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.HiddenAt);
        builder.Property(r => r.HiddenBy);

        builder.HasOne(r => r.Movie)
            .WithMany(m => m.Reviews)
            .HasForeignKey(r => r.MovieId)
            .HasConstraintName("FK_MovieReviews_Movies");

        builder.HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .HasConstraintName("FK_MovieReviews_Users");

        builder.HasOne(r => r.Booking)
            .WithOne(b => b.Review)
            .HasForeignKey<MovieReview>(r => r.BookingId)
            .HasConstraintName("FK_MovieReviews_Bookings");

        builder.HasOne(r => r.HiddenByUser)
            .WithMany()
            .HasForeignKey(r => r.HiddenBy)
            .HasConstraintName("FK_MovieReviews_HiddenByUsers");

        builder.HasIndex(r => r.MovieId).HasDatabaseName("IX_MovieReviews_MovieId");
        builder.HasIndex(r => r.UserId).HasDatabaseName("IX_MovieReviews_UserId");
        builder.HasIndex(r => r.BookingId).IsUnique().HasDatabaseName("UQ_MovieReviews_BookingId");
        builder.HasIndex(r => r.IsHidden).HasDatabaseName("IX_MovieReviews_IsHidden");

        builder.HasIndex(r => new { r.UserId, r.MovieId })
            .IsUnique()
            .HasDatabaseName("UQ_MovieReviews_UserId_MovieId");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_MovieReviews_Rating", "[Rating] BETWEEN 1 AND 5");
        });
    }
}
