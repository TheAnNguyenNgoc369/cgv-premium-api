using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingFnBPickupFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PickedUp",
                table: "BookingFnB",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PickedUpAt",
                table: "BookingFnB",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PickedUpByStaffId",
                table: "BookingFnB",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingFnB_BookingId",
                table: "BookingFnB",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_BookingFnB_PickedUpByStaffId",
                table: "BookingFnB",
                column: "PickedUpByStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookingFnB_PickedUpByStaff",
                table: "BookingFnB",
                column: "PickedUpByStaffId",
                principalTable: "Users",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookingFnB_PickedUpByStaff",
                table: "BookingFnB");

            migrationBuilder.DropIndex(
                name: "IX_BookingFnB_BookingId",
                table: "BookingFnB");

            migrationBuilder.DropIndex(
                name: "IX_BookingFnB_PickedUpByStaffId",
                table: "BookingFnB");

            migrationBuilder.DropColumn(
                name: "PickedUp",
                table: "BookingFnB");

            migrationBuilder.DropColumn(
                name: "PickedUpAt",
                table: "BookingFnB");

            migrationBuilder.DropColumn(
                name: "PickedUpByStaffId",
                table: "BookingFnB");
        }
    }
}
