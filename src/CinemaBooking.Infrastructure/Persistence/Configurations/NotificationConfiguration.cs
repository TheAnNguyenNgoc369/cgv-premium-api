using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notification");

        builder.HasKey(n => n.NotificationID);

        builder.Property(n => n.Title).HasMaxLength(150).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(500).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
        builder.Property(n => n.EventId).HasMaxLength(100).IsRequired();
        builder.Property(n => n.EventType).HasMaxLength(50).IsRequired();
        builder.Property(n => n.ReferenceType).HasMaxLength(50);
        builder.Property(n => n.ActionUrl).HasMaxLength(500);
        builder.Property(n => n.IsRead).HasDefaultValue(false);
        builder.Property(n => n.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(n => new { n.UserID, n.IsRead }).HasDatabaseName("IX_Notification_UserID_IsRead");
        builder.HasIndex(n => new { n.UserID, n.CreatedAt }).HasDatabaseName("IX_Notification_UserID_CreatedAt");
        builder.HasIndex(n => new { n.EventId, n.UserID }).IsUnique();

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserID)
            .HasConstraintName("FK_Notification_Users");

        builder.ToTable(t => t.HasCheckConstraint("CK_Notification_Type", "[Type] IN ('booking', 'payment', 'refund', 'promotion', 'system', 'account', 'analytics', 'report', 'movie', 'showtime')"));
    }
}
