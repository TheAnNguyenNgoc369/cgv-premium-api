using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.UserID);

        builder.Property(u => u.FullName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
        builder.Property(u => u.EmailVerifiedAt);
        builder.Property(u => u.Phone).HasMaxLength(15);
        builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
        builder.Property(u => u.AvatarURL).HasMaxLength(500);
        builder.Property(u => u.AvatarPublicId).HasMaxLength(255);
        builder.Property(u => u.Role).HasMaxLength(20).IsRequired();
        builder.Property(u => u.Status).HasMaxLength(20).IsRequired().HasDefaultValue("unverified");
        builder.Property(u => u.TokenVersion).HasDefaultValue(0);
        builder.Property(u => u.TotalPoints).HasDefaultValue(0);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
        builder.Property(u => u.UpdatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
        builder.HasIndex(u => new { u.Role, u.Status }).HasDatabaseName("IX_Users_Role_Status");

        builder.HasOne(u => u.Cinema)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CinemaID)
            .HasConstraintName("FK_Users_Cinema");

        builder.HasOne(u => u.LoyaltyTier)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.LoyaltyTierID)
            .HasConstraintName("FK_Users_LoyaltyTiers");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('customer', 'staff', 'admin', 'manager')");
            t.HasCheckConstraint("CK_Users_Status", "[Status] IN ('unverified', 'active', 'locked', 'inactive')");
            t.HasCheckConstraint("CK_Users_TotalPoints", "[TotalPoints] >= 0");
        });
    }
}
