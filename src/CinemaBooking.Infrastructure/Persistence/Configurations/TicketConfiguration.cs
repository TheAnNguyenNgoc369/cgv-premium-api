using CinemaBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CinemaBooking.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Ticket");

        builder.HasKey(t => t.TicketID);

        builder.Property(t => t.QRCode).HasMaxLength(500).IsRequired();
        builder.Property(t => t.Status).HasMaxLength(20).IsRequired().HasDefaultValue("valid");

        builder.HasIndex(t => t.BookingSeatID).IsUnique().HasDatabaseName("UQ_Ticket_BookingSeatID");
        builder.HasIndex(t => t.QRCode).IsUnique().HasDatabaseName("UQ_Ticket_QRCode");

        builder.HasOne(t => t.BookingSeat)
            .WithOne(bs => bs.Ticket)
            .HasForeignKey<Ticket>(t => t.BookingSeatID)
            .HasConstraintName("FK_Ticket_BookingSeat");

        builder.HasOne(t => t.CheckedInBy)
            .WithMany(u => u.CheckedInTickets)
            .HasForeignKey(t => t.CheckedInByID)
            .HasConstraintName("FK_Ticket_CheckedInBy");

        builder.ToTable(t => t.HasCheckConstraint("CK_Ticket_Status", "[Status] IN ('valid', 'used', 'cancelled')"));
    }
}
