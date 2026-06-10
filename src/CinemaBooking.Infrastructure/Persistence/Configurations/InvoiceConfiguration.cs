using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoice");

        builder.HasKey(i => i.InvoiceID);

        builder.Property(i => i.InvoiceCode).HasMaxLength(50).IsRequired();
        builder.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(i => i.TaxAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(i => i.IssuedAt).HasDefaultValueSql("GETDATE()");

        builder.HasIndex(i => i.BookingID).IsUnique().HasDatabaseName("UQ_Invoice_BookingID");
        builder.HasIndex(i => i.InvoiceCode).IsUnique().HasDatabaseName("UQ_Invoice_InvoiceCode");

        builder.HasOne(i => i.Booking)
            .WithOne(b => b.Invoice)
            .HasForeignKey<Invoice>(i => i.BookingID)
            .HasConstraintName("FK_Invoice_Booking");

        builder.ToTable(t => t.HasCheckConstraint("CK_Invoice_Amount", "[TotalAmount] >= 0 AND [TaxAmount] >= 0"));
    }
}
