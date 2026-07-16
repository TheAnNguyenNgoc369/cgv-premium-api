using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class LoyaltyTierConfiguration : IEntityTypeConfiguration<LoyaltyTier>
{
    public void Configure(EntityTypeBuilder<LoyaltyTier> builder)
    {
        builder.ToTable("LoyaltyTiers");

        builder.HasKey(t => t.TierID);

        builder.Property(t => t.TierName).HasMaxLength(20).IsRequired();
        builder.Property(t => t.DiscountRate).HasColumnType("decimal(4,2)");
        builder.Property(t => t.MaxRefundPerMonth).IsRequired();

        builder.HasIndex(t => t.TierName).IsUnique().HasDatabaseName("UQ_LoyaltyTiers_TierName");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_LoyaltyTiers_MinPoints", "[MinPoints] >= 0");
            t.HasCheckConstraint("CK_LoyaltyTiers_DiscountRate", "[DiscountRate] >= 0 AND [DiscountRate] <= 1");
        });

        builder.HasData(
            new LoyaltyTier { TierID = 1, TierName = "silver", MinPoints = 0, DiscountRate = 0.00m, MaxRefundPerMonth = 1 },
            new LoyaltyTier { TierID = 2, TierName = "gold", MinPoints = 1000, DiscountRate = 0.05m, MaxRefundPerMonth = 3 },
            new LoyaltyTier { TierID = 3, TierName = "platinum", MinPoints = 5000, DiscountRate = 0.10m, MaxRefundPerMonth = 5 },
            new LoyaltyTier { TierID = 4, TierName = "megavip", MinPoints = 10000, DiscountRate = 0.15m, MaxRefundPerMonth = 7 });
    }
}
