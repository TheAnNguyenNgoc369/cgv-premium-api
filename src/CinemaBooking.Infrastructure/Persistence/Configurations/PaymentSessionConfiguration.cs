using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class PaymentSessionConfiguration : IEntityTypeConfiguration<PaymentSession>
{
    public void Configure(EntityTypeBuilder<PaymentSession> builder)
    {
        builder.ToTable("PaymentSession");

        builder.HasKey(p => p.SessionID);

        builder.Property(p => p.GatewayName).HasMaxLength(50).IsRequired();
        builder.Property(p => p.GatewayOrderNo).HasMaxLength(200);
        builder.Property(p => p.QRCodeURL).HasMaxLength(500);
        builder.Property(p => p.Status).HasMaxLength(30).IsRequired().HasDefaultValue("waiting");
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(p => p.GatewayOrderNo).IsUnique().HasDatabaseName("UQ_PaymentSession_GatewayOrderNo");
        builder.HasIndex(p => p.PaymentID).HasDatabaseName("IX_PaymentSession_PaymentID");

        builder.HasOne(p => p.Payment)
            .WithMany(pay => pay.PaymentSessions)
            .HasForeignKey(p => p.PaymentID)
            .HasConstraintName("FK_PaymentSession_Payment");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_PaymentSession_GatewayName", "[GatewayName] IN ('payos', 'momo')");
            t.HasCheckConstraint("CK_PaymentSession_Status", "[Status] IN ('waiting', 'processing', 'completed', 'expired', 'cancelled')");
        });
    }
}
