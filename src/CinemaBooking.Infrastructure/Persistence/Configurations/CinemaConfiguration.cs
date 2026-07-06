using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class CinemaConfiguration : IEntityTypeConfiguration<Cinema>
{
    public void Configure(EntityTypeBuilder<Cinema> builder)
    {
        builder.ToTable("Cinema");

        builder.HasKey(c => c.CinemaID);

        builder.Property(c => c.CinemaName).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Address).HasMaxLength(255).IsRequired();
        builder.Property(c => c.Latitude).HasColumnType("decimal(9,6)");
        builder.Property(c => c.Longitude).HasColumnType("decimal(10,6)");
        builder.Property(c => c.Status).HasMaxLength(20).IsRequired().HasDefaultValue("active");
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("GETDATE()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("GETDATE()");

        builder.ToTable(t => t.HasCheckConstraint("CK_Cinema_Status", "[Status] IN ('active', 'inactive', 'maintenance')"));
    }
}
