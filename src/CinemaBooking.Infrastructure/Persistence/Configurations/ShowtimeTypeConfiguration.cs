using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public sealed class ShowtimeTypeConfiguration : IEntityTypeConfiguration<ShowtimeType>
{
    public void Configure(EntityTypeBuilder<ShowtimeType> builder)
    {
        builder.ToTable("ShowtimeType");
        builder.HasKey(x => x.ShowtimeTypeID);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.CinemaID).HasDatabaseName("IX_ShowtimeType_CinemaID");
        builder.HasIndex(x => new { x.CinemaID, x.Name }).IsUnique()
            .HasDatabaseName("UQ_ShowtimeType_CinemaID_Name");
        builder.HasOne(x => x.Cinema).WithMany().HasForeignKey(x => x.CinemaID);
    }
}
