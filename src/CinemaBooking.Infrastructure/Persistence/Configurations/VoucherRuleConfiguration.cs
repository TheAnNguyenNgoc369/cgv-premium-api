using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public sealed class VoucherRuleConfiguration : IEntityTypeConfiguration<VoucherRule>
{
    public void Configure(EntityTypeBuilder<VoucherRule> builder)
    {
        builder.ToTable("VoucherRules");
        builder.HasKey(vr => vr.RuleID);

        builder.Property(vr => vr.RuleType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(vr => vr.RuleValue)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(vr => vr.CreatedAt)
            .IsRequired();

        builder.HasOne(vr => vr.Voucher)
            .WithMany(v => v.VoucherRules)
            .HasForeignKey(vr => vr.VoucherID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(vr => vr.VoucherID);
        builder.HasIndex(vr => new { vr.VoucherID, vr.RuleType });
    }
}
