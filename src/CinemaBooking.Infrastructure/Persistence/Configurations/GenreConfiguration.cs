using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.ToTable("Genre");

        builder.HasKey(g => g.GenreID);

        builder.Property(g => g.GenreName).HasMaxLength(100).IsRequired();

        builder.HasIndex(g => g.GenreName).IsUnique().HasDatabaseName("UQ_Genre_GenreName");
    }
}
