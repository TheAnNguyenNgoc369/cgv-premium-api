using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransaction");

        builder.HasKey(w => w.TransactionID);

        builder.Property(w => w.Amount).HasColumnType("decimal(18,2)");
        builder.Property(w => w.BalanceAfter).HasColumnType("decimal(18,2)");
        builder.Property(w => w.TransactionType).HasMaxLength(30).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(255);
        builder.Property(w => w.CreatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(w => w.WalletID).HasDatabaseName("IX_WalletTransaction_WalletID");

        builder.HasOne(w => w.Wallet)
            .WithMany(wl => wl.Transactions)
            .HasForeignKey(w => w.WalletID)
            .HasConstraintName("FK_WalletTransaction_Wallet");

        builder.HasOne(w => w.Booking)
            .WithMany(b => b.WalletTransactions)
            .HasForeignKey(w => w.BookingID)
            .HasConstraintName("FK_WalletTransaction_Booking");

        builder.HasOne(w => w.Refund)
            .WithMany(r => r.WalletTransactions)
            .HasForeignKey(w => w.RefundID)
            .HasConstraintName("FK_WalletTransaction_Refund");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_WalletTransaction_Type", "[TransactionType] IN ('top_up', 'payment', 'refund')");
            t.HasCheckConstraint("CK_WalletTransaction_BalanceAfter", "[BalanceAfter] >= 0");
        });
    }
}
