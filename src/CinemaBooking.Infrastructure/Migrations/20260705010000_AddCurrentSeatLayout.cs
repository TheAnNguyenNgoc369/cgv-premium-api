using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations;

[DbContext(typeof(CinemaBookingDbContext))]
[Migration("20260705010000_AddCurrentSeatLayout")]
public partial class AddCurrentSeatLayout : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UQ_Seat_RoomID_SeatRow_SeatCol",
            table: "Seat");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Seat_Status",
            table: "Seat");

        migrationBuilder.Sql(
            "UPDATE [Seat] SET [Status] = 'inactive' WHERE [Status] NOT IN ('active', 'inactive');");

        migrationBuilder.AddColumn<bool>(
            name: "IsCurrentLayout",
            table: "Seat",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.CreateIndex(
            name: "UQ_Seat_RoomID_SeatRow_SeatCol",
            table: "Seat",
            columns: new[] { "RoomID", "SeatRow", "SeatCol" },
            unique: true,
            filter: "[IsCurrentLayout] = 1");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Seat_Status",
            table: "Seat",
            sql: "[Status] IN ('active', 'inactive')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UQ_Seat_RoomID_SeatRow_SeatCol",
            table: "Seat");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Seat_Status",
            table: "Seat");

        migrationBuilder.DropColumn(
            name: "IsCurrentLayout",
            table: "Seat");

        migrationBuilder.CreateIndex(
            name: "UQ_Seat_RoomID_SeatRow_SeatCol",
            table: "Seat",
            columns: new[] { "RoomID", "SeatRow", "SeatCol" });

        migrationBuilder.AddCheckConstraint(
            name: "CK_Seat_Status",
            table: "Seat",
            sql: "[Status] IN ('active', 'inactive', 'maintenance')");
    }
}
