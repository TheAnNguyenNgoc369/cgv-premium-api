using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ScopeProductsByCinema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CinemaID",
                table: "Product",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Product] SET [CinemaID] = 1 WHERE [CinemaID] IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "CinemaID",
                table: "Product",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_CinemaID",
                table: "Product",
                column: "CinemaID");

            migrationBuilder.CreateIndex(
                name: "UQ_Product_CinemaID_ItemName",
                table: "Product",
                columns: new[] { "CinemaID", "ItemName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Cinema",
                table: "Product",
                column: "CinemaID",
                principalTable: "Cinema",
                principalColumn: "CinemaID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Product_Cinema",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "IX_Product_CinemaID",
                table: "Product");

            migrationBuilder.DropIndex(
                name: "UQ_Product_CinemaID_ItemName",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "CinemaID",
                table: "Product");
        }
    }
}
