using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class EmailVerificationTokenConfiguration : IEntityTypeConfiguration<EmailVerificationToken>
{
    public void Configure(EntityTypeBuilder<EmailVerificationToken> builder)
    {
        builder.ToTable("EmailVerificationToken");

        builder.HasKey(t => t.TokenID);

        builder.Property(t => t.Token).HasMaxLength(255).IsRequired();
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(t => t.Token).IsUnique().HasDatabaseName("UQ_EmailVerificationToken_Token");

        builder.HasOne(t => t.User)
            .WithMany(u => u.EmailVerificationTokens)
            .HasForeignKey(t => t.UserID)
            .HasConstraintName("FK_EmailVerificationToken_Users");
    }
}
