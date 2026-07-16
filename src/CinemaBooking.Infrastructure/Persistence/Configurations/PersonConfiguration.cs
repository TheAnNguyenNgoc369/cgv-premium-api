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
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(p => p.Name).IsUnique().HasDatabaseName("UQ_Person_Name");
    }
}
