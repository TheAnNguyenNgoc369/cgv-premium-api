using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class ReviewRewardSettingsConfiguration : IEntityTypeConfiguration<ReviewRewardSettings>
{
    public void Configure(EntityTypeBuilder<ReviewRewardSettings> builder)
    {
        builder.ToTable("ReviewRewardSettings");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.FirstReviewPoints).IsRequired();
        builder.Property(r => r.NextReviewPoints).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();
        builder.Property(r => r.UpdatedBy);

        builder.HasOne(r => r.UpdatedByUser)
            .WithMany()
            .HasForeignKey(r => r.UpdatedBy)
            .HasConstraintName("FK_ReviewRewardSettings_Users");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_ReviewRewardSettings_FirstReviewPoints", "[FirstReviewPoints] >= 0");
            t.HasCheckConstraint("CK_ReviewRewardSettings_NextReviewPoints", "[NextReviewPoints] >= 0");
        });

        builder.HasData(
            new ReviewRewardSettings
            {
                Id = 1,
                FirstReviewPoints = 50,
                NextReviewPoints = 10,
                UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedBy = null
            });
    }
}
