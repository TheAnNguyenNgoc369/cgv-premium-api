using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBarcodePrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing barcodes from CGV prefix to CV prefix
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET BarCode = 'CV' + SUBSTRING(BarCode, 4, LEN(BarCode) - 3)
                WHERE BarCode LIKE 'CGV%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert barcodes from CV prefix back to CGV prefix
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET BarCode = 'CGV' + SUBSTRING(BarCode, 3, LEN(BarCode) - 2)
                WHERE BarCode LIKE 'CV%';
            ");
        }
    }
}
