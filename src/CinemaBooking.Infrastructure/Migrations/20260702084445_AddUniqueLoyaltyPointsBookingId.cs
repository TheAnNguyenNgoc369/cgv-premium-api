using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueLoyaltyPointsBookingId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoyaltyPoints_BookingID",
                table: "LoyaltyPoints");

            migrationBuilder.CreateIndex(
                name: "UQ_LoyaltyPoints_BookingID_Earned",
                table: "LoyaltyPoints",
                column: "BookingID",
                unique: true,
                filter: "[TransactionType] = 'earn' AND [BookingID] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_LoyaltyPoints_BookingID_Earned",
                table: "LoyaltyPoints");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPoints_BookingID",
                table: "LoyaltyPoints",
                column: "BookingID");
        }
    }
}
