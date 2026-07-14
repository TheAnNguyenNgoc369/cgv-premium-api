using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentSessionReturnFrontendUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnFrontendUrl",
                table: "PaymentSession",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnFrontendUrl",
                table: "PaymentSession");
        }
    }
}
