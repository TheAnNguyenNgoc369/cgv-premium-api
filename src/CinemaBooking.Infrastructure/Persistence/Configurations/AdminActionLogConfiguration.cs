using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class AdminActionLogConfiguration : IEntityTypeConfiguration<AdminActionLog>
{
    public void Configure(EntityTypeBuilder<AdminActionLog> builder)
    {
        builder.ToTable("AdminActionLog");

        builder.HasKey(a => a.LogID);

        builder.Property(a => a.TargetTable).HasMaxLength(50);
        builder.Property(a => a.ActionType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.Property(a => a.IPAddress).HasMaxLength(45);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(a => a.AdminID).HasDatabaseName("IX_AdminActionLog_AdminID");

        builder.HasOne(a => a.Admin)
            .WithMany(u => u.AdminActions)
            .HasForeignKey(a => a.AdminID)
            .HasConstraintName("FK_AdminActionLog_Admin");

        builder.HasOne(a => a.TargetUser)
            .WithMany(u => u.TargetedAdminActions)
            .HasForeignKey(a => a.TargetUserID)
            .HasConstraintName("FK_AdminActionLog_TargetUser");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_AdminActionLog_ActionType",
            "[ActionType] IN ('lock_user','unlock_user','change_role','cancel_booking','refund_processed','payment_viewed','booking_created','account_status_changed','create_user','update_user','delete_user','deactivate_user','create_voucher','update_voucher','delete_voucher','view_revenue_report','export_report')"));
    }
}
