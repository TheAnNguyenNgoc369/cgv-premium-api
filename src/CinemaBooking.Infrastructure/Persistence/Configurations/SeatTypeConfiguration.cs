using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class SeatTypeConfiguration : IEntityTypeConfiguration<SeatType>
{
    public void Configure(EntityTypeBuilder<SeatType> builder)
    {
        builder.ToTable("SeatType");

        builder.HasKey(s => s.SeatTypeID);

        builder.Property(s => s.TypeName).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Capacity).HasDefaultValue(1);
        builder.Property(s => s.ExtraPrice).HasColumnType("decimal(18,2)").HasDefaultValue(0m);

        builder.HasIndex(s => s.TypeName).IsUnique().HasDatabaseName("UQ_SeatType_TypeName");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SeatType_Capacity", "[Capacity] > 0");
            t.HasCheckConstraint("CK_SeatType_ExtraPrice", "[ExtraPrice] >= 0");
        });

        builder.HasData(
            new SeatType { SeatTypeID = 1, TypeName = "standard", Capacity = 1, ExtraPrice = 0 },
            new SeatType { SeatTypeID = 2, TypeName = "vip", Capacity = 1, ExtraPrice = 20000 },
            new SeatType { SeatTypeID = 3, TypeName = "couple", Capacity = 2, ExtraPrice = 50000 });
    }
}
