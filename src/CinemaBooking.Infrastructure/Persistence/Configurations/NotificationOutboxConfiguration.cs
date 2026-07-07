using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public sealed class NotificationOutboxConfiguration : IEntityTypeConfiguration<NotificationOutbox>
{
    public void Configure(EntityTypeBuilder<NotificationOutbox> builder)
    {
        builder.ToTable("NotificationOutbox");
        builder.HasKey(x => x.NotificationOutboxID);
        builder.Property(x => x.EventId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired().HasDefaultValue("pending");
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(x => x.EventId).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
    }
}
