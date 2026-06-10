using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> builder)
    {
        builder.ToTable("Voucher");

        builder.HasKey(v => v.VoucherID);

        builder.Property(v => v.VoucherCode).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Category).HasMaxLength(50);
        builder.Property(v => v.DiscountType).HasMaxLength(20).IsRequired();
        builder.Property(v => v.DiscountValue).HasColumnType("decimal(18,2)");
        builder.Property(v => v.MinOrderValue).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(v => v.UsedCount).HasDefaultValue(0);
        builder.Property(v => v.ImageURL).HasMaxLength(500);
        builder.Property(v => v.Description).HasMaxLength(500);
        builder.Property(v => v.IsActive).HasDefaultValue(true);
        builder.Property(v => v.CreatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(v => v.VoucherCode).IsUnique().HasDatabaseName("UQ_Voucher_VoucherCode");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Voucher_Category", "[Category] IS NULL OR [Category] IN ('Discount', 'Combo', 'Cashback')");
            t.HasCheckConstraint("CK_Voucher_DiscountType", "[DiscountType] IN ('percent', 'fixed')");
            t.HasCheckConstraint("CK_Voucher_DiscountValue", "[DiscountValue] >= 0");
            t.HasCheckConstraint("CK_Voucher_MinOrderValue", "[MinOrderValue] IS NULL OR [MinOrderValue] >= 0");
            t.HasCheckConstraint("CK_Voucher_MaxUses", "[MaxUses] IS NULL OR [MaxUses] > 0");
            t.HasCheckConstraint("CK_Voucher_UsedCount", "[UsedCount] >= 0");
            t.HasCheckConstraint("CK_Voucher_ValidDate", "[ValidUntil] > [ValidFrom]");
        });
    }
}
