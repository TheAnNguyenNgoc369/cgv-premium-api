using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public sealed class ShowtimeTypeSlotConfiguration : IEntityTypeConfiguration<ShowtimeTypeSlot>
{
    public void Configure(EntityTypeBuilder<ShowtimeTypeSlot> builder)
    {
        builder.ToTable("ShowtimeTypeSlot");
        builder.HasKey(x => x.SlotID);
        builder.Property(x => x.StartTime).HasColumnType("time");
        builder.HasIndex(x => new { x.ShowtimeTypeID, x.StartTime }).IsUnique()
            .HasDatabaseName("UX_ShowtimeTypeSlot_ShowtimeTypeID_StartTime");
        builder.HasOne(x => x.ShowtimeType).WithMany(x => x.Slots)
            .HasForeignKey(x => x.ShowtimeTypeID).OnDelete(DeleteBehavior.Cascade);
    }
}
