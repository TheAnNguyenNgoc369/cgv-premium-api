using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payment");

        builder.HasKey(p => p.PaymentID);

        builder.Property(p => p.PaymentMethod).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.TransactionCode).HasMaxLength(200);
        builder.Property(p => p.Status).HasMaxLength(30).IsRequired().HasDefaultValue("pending");
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(p => p.BookingID).IsUnique().HasDatabaseName("UQ_Payment_BookingID");
        builder.HasIndex(p => p.TransactionCode).IsUnique().HasDatabaseName("UQ_Payment_TransactionCode");
        builder.HasIndex(p => p.Status).HasDatabaseName("IX_Payment_Status");

        builder.HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingID)
            .HasConstraintName("FK_Payment_Booking");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Payment_Method", "[PaymentMethod] IN ('vnpay', 'payos', 'momo', 'credit_card', 'banking', 'cash', 'wallet')");
            t.HasCheckConstraint("CK_Payment_Amount", "[Amount] >= 0");
            t.HasCheckConstraint("CK_Payment_Status", "[Status] IN ('pending', 'success', 'failed', 'refunded', 'cancelled', 'expired')");
        });
    }
}
