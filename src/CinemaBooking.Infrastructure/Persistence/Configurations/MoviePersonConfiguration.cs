using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class MoviePersonConfiguration : IEntityTypeConfiguration<MoviePerson>
{
    public void Configure(EntityTypeBuilder<MoviePerson> builder)
    {
        builder.ToTable("MoviePerson");

        builder.HasKey(mp => new { mp.MovieId, mp.PersonId, mp.Role });

        builder.Property(mp => mp.MovieId).HasColumnName("MovieId");
        builder.Property(mp => mp.PersonId).HasColumnName("PersonId");
        builder.Property(mp => mp.Role).HasMaxLength(50).IsRequired();
        builder.Property(mp => mp.DisplayOrder).HasDefaultValue(0);

        builder.HasIndex(mp => mp.PersonId).HasDatabaseName("IX_MoviePerson_PersonId");
        builder.HasIndex(mp => new { mp.MovieId, mp.Role, mp.DisplayOrder })
            .HasDatabaseName("IX_MoviePerson_MovieId_Role_DisplayOrder");

        builder.HasOne(mp => mp.Movie)
            .WithMany(m => m.MoviePersons)
            .HasForeignKey(mp => mp.MovieId)
            .HasConstraintName("FK_MoviePerson_Movie");

        builder.HasOne(mp => mp.Person)
            .WithMany(p => p.MoviePersons)
            .HasForeignKey(mp => mp.PersonId)
            .HasConstraintName("FK_MoviePerson_Person");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_MoviePerson_Role", "[Role] IN ('Director', 'Actor')");
        });
    }
}
