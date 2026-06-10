using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product");

        builder.HasKey(p => p.ItemID);

        builder.Property(p => p.ItemName).HasMaxLength(150).IsRequired();
        builder.Property(p => p.ItemType).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.StockQuantity).HasDefaultValue(0);
        builder.Property(p => p.ImageURL).HasMaxLength(500);
        builder.Property(p => p.IsOnMenu).HasDefaultValue(true);
        builder.Property(p => p.IsLoyaltyEligible).HasDefaultValue(false);
        builder.Property(p => p.Status).HasMaxLength(20).IsRequired().HasDefaultValue("in_stock");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("GETDATE()");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Product_ItemType", "[ItemType] IN ('combo', 'snack', 'beverage', 'dessert')");
            t.HasCheckConstraint("CK_Product_Price", "[Price] >= 0");
            t.HasCheckConstraint("CK_Product_StockQuantity", "[StockQuantity] >= 0");
            t.HasCheckConstraint("CK_Product_Status", "[Status] IN ('in_stock', 'low_stock', 'out_of_stock', 'inactive')");
        });
    }
}
