using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Person");

        builder.HasKey(p => p.PersonId);

        builder.Property(p => p.PersonId).HasColumnName("PersonId");
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Biography).HasColumnType("nvarchar(max)");
        builder.Property(p => p.DateOfBirth).HasColumnType("date");
        builder.Property(p => p.Nationality).HasMaxLength(100);
        builder.Property(p => p.Gender).HasMaxLength(20);
        builder.Property(p => p.PhotoUrl).HasMaxLength(500);
        builder.Property(p => p.PhotoPublicId).HasMaxLength(255);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("GETDATE()");
    }
}
