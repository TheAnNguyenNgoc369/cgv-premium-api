using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refund");

        builder.HasKey(r => r.RefundID);

        builder.Property(r => r.Amount).HasColumnType("decimal(18,2)");
        builder.Property(r => r.Reason).HasMaxLength(255);
        builder.Property(r => r.Status).HasMaxLength(30).IsRequired().HasDefaultValue("pending");
        builder.Property(r => r.RequestedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(r => r.BookingID).HasDatabaseName("IX_Refund_BookingID");

        builder.HasOne(r => r.Booking)
            .WithMany(b => b.Refunds)
            .HasForeignKey(r => r.BookingID)
            .HasConstraintName("FK_Refund_Booking");

        builder.HasOne(r => r.Payment)
            .WithMany(p => p.Refunds)
            .HasForeignKey(r => r.PaymentID)
            .HasConstraintName("FK_Refund_Payment");

        builder.HasOne(r => r.ProcessedByUser)
            .WithMany(u => u.ProcessedRefunds)
            .HasForeignKey(r => r.ProcessedBy)
            .HasConstraintName("FK_Refund_ProcessedBy");

        builder.HasOne(r => r.Wallet)
            .WithMany()
            .HasForeignKey(r => r.WalletID)
            .HasConstraintName("FK_Refund_Wallet");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Refund_Amount", "[Amount] >= 0");
            t.HasCheckConstraint("CK_Refund_Status", "[Status] IN ('pending', 'approved', 'rejected', 'processing', 'completed', 'failed')");
        });
    }
}
