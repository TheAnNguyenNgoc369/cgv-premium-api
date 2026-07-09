using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCinemaCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Cinema",
                type: "decimal(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Cinema",
                type: "decimal(10,6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Cinema");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Cinema");
        }
    }
}
