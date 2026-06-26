using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatTypeCapacityAndDerivedRoomCapacity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Room_Capacity",
                table: "Room");

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "SeatType",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "Capacity",
                table: "Room",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.UpdateData(
                table: "SeatType",
                keyColumn: "SeatTypeID",
                keyValue: 1,
                column: "Capacity",
                value: 1);

            migrationBuilder.UpdateData(
                table: "SeatType",
                keyColumn: "SeatTypeID",
                keyValue: 2,
                column: "Capacity",
                value: 1);

            migrationBuilder.UpdateData(
                table: "SeatType",
                keyColumn: "SeatTypeID",
                keyValue: 3,
                column: "Capacity",
                value: 2);

            migrationBuilder.Sql(
                """
                UPDATE room
                SET Capacity = COALESCE(capacity.TotalCapacity, 0)
                FROM [Room] AS room
                OUTER APPLY (
                    SELECT SUM(seatType.[Capacity]) AS TotalCapacity
                    FROM [Seat] AS seat
                    INNER JOIN [SeatType] AS seatType
                        ON seat.[SeatTypeID] = seatType.[SeatTypeID]
                    WHERE seat.[RoomID] = room.[RoomID]
                ) AS capacity;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SeatType_Capacity",
                table: "SeatType",
                sql: "[Capacity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Room_Capacity",
                table: "Room",
                sql: "[Capacity] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SeatType_Capacity",
                table: "SeatType");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Room_Capacity",
                table: "Room");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "SeatType");

            migrationBuilder.AlterColumn<int>(
                name: "Capacity",
                table: "Room",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Room_Capacity",
                table: "Room",
                sql: "[Capacity] > 0");
        }
    }
}
