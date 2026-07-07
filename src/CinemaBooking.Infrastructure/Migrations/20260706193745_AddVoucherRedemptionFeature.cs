using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherRedemptionFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LoyaltyPoints_TransactionType",
                table: "LoyaltyPoints");

            migrationBuilder.AddColumn<int>(
                name: "ExchangeLimit",
                table: "Voucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRedeemable",
                table: "Voucher",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RemainingQuantity",
                table: "Voucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredPoints",
                table: "Voucher",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoucherID",
                table: "LoyaltyPoints",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserVoucher",
                columns: table => new
                {
                    UserVoucherID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    VoucherID = table.Column<int>(type: "int", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BookingID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVoucher", x => x.UserVoucherID);
                    table.CheckConstraint("CK_UserVoucher_Dates", "[ExpiredAt] >= [RedeemedAt]");
                    table.CheckConstraint("CK_UserVoucher_Status", "[Status] IN ('available', 'used', 'expired')");
                    table.ForeignKey(
                        name: "FK_UserVoucher_Booking",
                        column: x => x.BookingID,
                        principalTable: "Booking",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_UserVoucher_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_UserVoucher_Voucher",
                        column: x => x.VoucherID,
                        principalTable: "Voucher",
                        principalColumn: "VoucherID");
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_ExchangeLimit",
                table: "Voucher",
                sql: "[ExchangeLimit] IS NULL OR [ExchangeLimit] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_RemainingQuantity",
                table: "Voucher",
                sql: "[RemainingQuantity] IS NULL OR [RemainingQuantity] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Voucher_RequiredPoints",
                table: "Voucher",
                sql: "[RequiredPoints] IS NULL OR [RequiredPoints] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPoints_VoucherID",
                table: "LoyaltyPoints",
                column: "VoucherID");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoyaltyPoints_TransactionType",
                table: "LoyaltyPoints",
                sql: "[TransactionType] IN ('earn', 'redeem', 'expire', 'adjust', 'exchange')");

            migrationBuilder.CreateIndex(
                name: "IX_UserVoucher_BookingID",
                table: "UserVoucher",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_UserVoucher_Status",
                table: "UserVoucher",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserVoucher_UserID",
                table: "UserVoucher",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserVoucher_VoucherID",
                table: "UserVoucher",
                column: "VoucherID");

            migrationBuilder.AddForeignKey(
                name: "FK_LoyaltyPoints_Voucher",
                table: "LoyaltyPoints",
                column: "VoucherID",
                principalTable: "Voucher",
                principalColumn: "VoucherID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoyaltyPoints_Voucher",
                table: "LoyaltyPoints");

            migrationBuilder.DropTable(
                name: "UserVoucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_ExchangeLimit",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_RemainingQuantity",
                table: "Voucher");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Voucher_RequiredPoints",
                table: "Voucher");

            migrationBuilder.DropIndex(
                name: "IX_LoyaltyPoints_VoucherID",
                table: "LoyaltyPoints");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoyaltyPoints_TransactionType",
                table: "LoyaltyPoints");

            migrationBuilder.DropColumn(
                name: "ExchangeLimit",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "IsRedeemable",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "RemainingQuantity",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "RequiredPoints",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "VoucherID",
                table: "LoyaltyPoints");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoyaltyPoints_TransactionType",
                table: "LoyaltyPoints",
                sql: "[TransactionType] IN ('earn', 'redeem', 'expire', 'adjust')");
        }
    }
}
