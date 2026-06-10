using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class MovieGenreConfiguration : IEntityTypeConfiguration<MovieGenre>
{
    public void Configure(EntityTypeBuilder<MovieGenre> builder)
    {
        builder.ToTable("MovieGenre");

        builder.HasKey(mg => mg.MovieGenreID);

        builder.HasIndex(mg => new { mg.MovieID, mg.GenreID })
            .IsUnique()
            .HasDatabaseName("UQ_MovieGenre_MovieID_GenreID");

        builder.HasOne(mg => mg.Movie)
            .WithMany(m => m.MovieGenres)
            .HasForeignKey(mg => mg.MovieID)
            .HasConstraintName("FK_MovieGenre_Movie");

        builder.HasOne(mg => mg.Genre)
            .WithMany(g => g.MovieGenres)
            .HasForeignKey(mg => mg.GenreID)
            .HasConstraintName("FK_MovieGenre_Genre");
    }
}
