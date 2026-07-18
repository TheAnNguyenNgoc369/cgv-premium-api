using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNoShowBookingStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_Status",
                table: "Booking");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_Status",
                table: "Booking",
                sql: "[Status] IN ('pending', 'paid', 'cancelled', 'refunded', 'used', 'expired', 'payment_failed', 'partially_refunded', 'no_show')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Booking_Status",
                table: "Booking");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Booking_Status",
                table: "Booking",
                sql: "[Status] IN ('pending', 'paid', 'cancelled', 'refunded', 'used', 'expired', 'payment_failed', 'partially_refunded')");
        }
    }
}
