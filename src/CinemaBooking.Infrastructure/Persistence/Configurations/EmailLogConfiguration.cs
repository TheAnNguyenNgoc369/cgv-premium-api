using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("EmailLog");

        builder.HasKey(e => e.EmailLogID);

        builder.Property(e => e.ToEmail).HasMaxLength(150).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.DeliveryStatus).HasMaxLength(20).IsRequired().HasDefaultValue("pending");
        builder.Property(e => e.RetryCount).HasDefaultValue(0);
        builder.Property(e => e.LastError).HasMaxLength(2000);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(e => e.UserID).HasDatabaseName("IX_EmailLog_UserID");

        builder.HasOne(e => e.User)
            .WithMany(u => u.EmailLogs)
            .HasForeignKey(e => e.UserID)
            .HasConstraintName("FK_EmailLog_Users");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_EmailLog_EventType", "[EventType] IN ('register','booking_confirmed','booking_cancelled','forgot_password','refund_processed','points_earned','reward_redeemed')");
            t.HasCheckConstraint("CK_EmailLog_DeliveryStatus", "[DeliveryStatus] IN ('pending', 'processing', 'sent', 'failed', 'retrying')");
            t.HasCheckConstraint("CK_EmailLog_RetryCount", "[RetryCount] BETWEEN 0 AND 3");
        });
    }
}
