using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public sealed class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> builder)
    {
        builder.ToTable("RoomType", table =>
            table.HasCheckConstraint("CK_RoomType_ExtraPrice", "[ExtraPrice] >= 0"));
        builder.HasKey(x => x.RoomTypeID);
        builder.Property(x => x.TypeName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ExtraPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.HasIndex(x => x.TypeName).IsUnique().HasDatabaseName("UQ_RoomType_TypeName");
    }
}
