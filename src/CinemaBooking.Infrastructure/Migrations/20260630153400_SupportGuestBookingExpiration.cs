using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SupportGuestBookingExpiration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookingID",
                table: "SeatHold",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "Booking",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_SeatHold_BookingID",
                table: "SeatHold",
                column: "BookingID");

            migrationBuilder.AddForeignKey(
                name: "FK_SeatHold_Booking",
                table: "SeatHold",
                column: "BookingID",
                principalTable: "Booking",
                principalColumn: "BookingID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeatHold_Booking",
                table: "SeatHold");

            migrationBuilder.DropIndex(
                name: "IX_SeatHold_BookingID",
                table: "SeatHold");

            migrationBuilder.DropColumn(
                name: "BookingID",
                table: "SeatHold");

            migrationBuilder.AlterColumn<int>(
                name: "UserID",
                table: "Booking",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
