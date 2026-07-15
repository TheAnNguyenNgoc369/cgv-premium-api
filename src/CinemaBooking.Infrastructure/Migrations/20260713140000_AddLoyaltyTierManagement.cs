using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    [Migration("20260713140000_AddLoyaltyTierManagement")]
    public partial class AddLoyaltyTierManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LoyaltyTiers_TierName",
                table: "LoyaltyTiers");

            migrationBuilder.CreateIndex(
                name: "UQ_LoyaltyTiers_MinPoints",
                table: "LoyaltyTiers",
                column: "MinPoints",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_LoyaltyTiers_MinPoints",
                table: "LoyaltyTiers");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoyaltyTiers_TierName",
                table: "LoyaltyTiers",
                sql: "[TierName] IN ('silver', 'gold', 'platinum', 'megavip')");
        }
    }
}
