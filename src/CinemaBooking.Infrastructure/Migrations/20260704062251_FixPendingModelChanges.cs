using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Seat_Status",
                table: "Seat");

            migrationBuilder.AlterColumn<int>(
                name: "SeatTypeID",
                table: "Seat",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<bool>(
                name: "IsGap",
                table: "Seat",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Seat_Status",
                table: "Seat",
                sql: "[Status] IN ('active', 'inactive', 'maintenance')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Seat_Status",
                table: "Seat");

            migrationBuilder.DropColumn(
                name: "IsGap",
                table: "Seat");

            migrationBuilder.AlterColumn<int>(
                name: "SeatTypeID",
                table: "Seat",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Seat_Status",
                table: "Seat",
                sql: "[Status] IN ('active', 'inactive')");
        }
    }
}
