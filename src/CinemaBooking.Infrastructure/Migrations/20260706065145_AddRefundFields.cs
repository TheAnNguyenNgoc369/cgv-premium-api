using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WalletID",
                table: "Refund",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "Payment",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReason",
                table: "Payment",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "Payment",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundedBy",
                table: "Payment",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRefundPerMonth",
                table: "LoyaltyTiers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "LoyaltyTiers",
                keyColumn: "TierID",
                keyValue: 1,
                column: "MaxRefundPerMonth",
                value: 1);

            migrationBuilder.UpdateData(
                table: "LoyaltyTiers",
                keyColumn: "TierID",
                keyValue: 2,
                column: "MaxRefundPerMonth",
                value: 3);

            migrationBuilder.UpdateData(
                table: "LoyaltyTiers",
                keyColumn: "TierID",
                keyValue: 3,
                column: "MaxRefundPerMonth",
                value: 5);

            migrationBuilder.UpdateData(
                table: "LoyaltyTiers",
                keyColumn: "TierID",
                keyValue: 4,
                column: "MaxRefundPerMonth",
                value: 7);

            migrationBuilder.CreateIndex(
                name: "IX_Refund_WalletID",
                table: "Refund",
                column: "WalletID");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_RefundedBy",
                table: "Payment",
                column: "RefundedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_RefundedBy",
                table: "Payment",
                column: "RefundedBy",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Refund_Wallet",
                table: "Refund",
                column: "WalletID",
                principalTable: "Wallet",
                principalColumn: "WalletID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payment_RefundedBy",
                table: "Payment");

            migrationBuilder.DropForeignKey(
                name: "FK_Refund_Wallet",
                table: "Refund");

            migrationBuilder.DropIndex(
                name: "IX_Refund_WalletID",
                table: "Refund");

            migrationBuilder.DropIndex(
                name: "IX_Payment_RefundedBy",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "WalletID",
                table: "Refund");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "RefundedBy",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "MaxRefundPerMonth",
                table: "LoyaltyTiers");
        }
    }
}
