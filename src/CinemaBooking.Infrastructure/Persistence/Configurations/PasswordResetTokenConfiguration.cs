using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetToken");

        builder.HasKey(t => t.TokenID);

        builder.Property(t => t.Token).HasMaxLength(255).IsRequired();
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(t => t.Token).IsUnique().HasDatabaseName("UQ_PasswordResetToken_Token");

        builder.HasOne(t => t.User)
            .WithMany(u => u.PasswordResetTokens)
            .HasForeignKey(t => t.UserID)
            .HasConstraintName("FK_PasswordResetToken_Users");
    }
}
