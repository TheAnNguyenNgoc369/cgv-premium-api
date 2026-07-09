using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckInFieldsToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "Booking",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckedInByUserId",
                table: "Booking",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QRCode",
                table: "Booking",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Booking_CheckedInByUserId",
                table: "Booking",
                column: "CheckedInByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Booking_QRCode",
                table: "Booking",
                column: "QRCode",
                unique: true,
                filter: "[QRCode] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Booking_CheckedInByUser",
                table: "Booking",
                column: "CheckedInByUserId",
                principalTable: "Users",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Booking_CheckedInByUser",
                table: "Booking");

            migrationBuilder.DropIndex(
                name: "IX_Booking_CheckedInByUserId",
                table: "Booking");

            migrationBuilder.DropIndex(
                name: "UQ_Booking_QRCode",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "CheckedInByUserId",
                table: "Booking");

            migrationBuilder.DropColumn(
                name: "QRCode",
                table: "Booking");
        }
    }
}
