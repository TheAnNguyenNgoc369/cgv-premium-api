using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeProductsGlobalWithoutStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_Status",
                table: "Product");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_StockQuantity",
                table: "Product");

            migrationBuilder.Sql(
                "UPDATE [Product] SET [Status] = 'active' WHERE [Status] <> 'inactive'");

            migrationBuilder.DropColumn(
                name: "CinemaID",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "IsOnMenu",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "Product");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Product",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "active",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "in_stock");

            migrationBuilder.CreateIndex(
                name: "UQ_Product_ItemName",
                table: "Product",
                column: "ItemName",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_Status",
                table: "Product",
                sql: "[Status] IN ('active', 'inactive')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Product_ItemName",
                table: "Product");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Product_Status",
                table: "Product");

            migrationBuilder.Sql(
                "UPDATE [Product] SET [Status] = 'in_stock' WHERE [Status] = 'active'");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Product",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "in_stock",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "active");

            migrationBuilder.AddColumn<int>(
                name: "CinemaID",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnMenu",
                table: "Product",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "Product",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Product_CinemaID",
                table: "Product",
                column: "CinemaID");

            migrationBuilder.CreateIndex(
                name: "UQ_Product_CinemaID_ItemName",
                table: "Product",
                columns: new[] { "CinemaID", "ItemName" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_Status",
                table: "Product",
                sql: "[Status] IN ('in_stock', 'low_stock', 'out_of_stock', 'inactive')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Product_StockQuantity",
                table: "Product",
                sql: "[StockQuantity] >= 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Product_Cinema",
                table: "Product",
                column: "CinemaID",
                principalTable: "Cinema",
                principalColumn: "CinemaID");
        }
    }
}
