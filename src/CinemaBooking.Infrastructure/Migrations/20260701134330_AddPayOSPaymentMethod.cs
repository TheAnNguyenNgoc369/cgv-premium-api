using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayOSPaymentMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentSession_GatewayName",
                table: "PaymentSession");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentSession_GatewayName",
                table: "PaymentSession",
                sql: "[GatewayName] IN ('vnpay', 'payos', 'momo')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment",
                sql: "[PaymentMethod] IN ('vnpay', 'payos', 'momo', 'credit_card', 'banking', 'cash', 'wallet')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentSession_GatewayName",
                table: "PaymentSession");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentSession_GatewayName",
                table: "PaymentSession",
                sql: "[GatewayName] IN ('vnpay', 'momo')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payment_Method",
                table: "Payment",
                sql: "[PaymentMethod] IN ('vnpay', 'momo', 'credit_card', 'banking', 'cash', 'wallet')");
        }
    }
}
