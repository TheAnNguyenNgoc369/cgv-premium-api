using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallet");

        builder.HasKey(w => w.WalletID);

        builder.Property(w => w.Balance).HasColumnType("decimal(18,2)").HasDefaultValue(0m);

        builder.HasIndex(w => w.UserID).IsUnique().HasDatabaseName("UQ_Wallet_UserID");

        builder.HasOne(w => w.User)
            .WithOne(u => u.Wallet)
            .HasForeignKey<Wallet>(w => w.UserID)
            .HasConstraintName("FK_Wallet_Users");

        builder.ToTable(t => t.HasCheckConstraint("CK_Wallet_Balance", "[Balance] >= 0"));
    }
}
