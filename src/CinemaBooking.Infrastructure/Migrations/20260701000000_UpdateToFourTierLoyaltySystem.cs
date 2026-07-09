using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateToFourTierLoyaltySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Delete all LoyaltyPoints history (reset loyalty transactions)
            migrationBuilder.Sql("DELETE FROM LoyaltyPoints;");

            // Step 2: Reset all users loyalty data
            migrationBuilder.Sql("UPDATE Users SET TotalPoints = 0;");
            migrationBuilder.Sql("UPDATE Users SET LoyaltyTierID = NULL;");

            // Step 3: Delete old loyalty tiers (Member, VIP)
            migrationBuilder.Sql("DELETE FROM LoyaltyTiers;");

            // Step 4: Insert new 4-tier system (Silver, Gold, Platinum, MegaVIP)
            migrationBuilder.Sql(@"
                INSERT INTO LoyaltyTiers (TierName, MinPoints, DiscountRate)
                VALUES
                    ('silver', 0, 0.00),
                    ('gold', 1000, 0.05),
                    ('platinum', 5000, 0.10),
                    ('megavip', 10000, 0.15);
            ");

            // Step 5: Set all users to Silver tier (default tier)
            migrationBuilder.Sql(@"
                UPDATE Users
                SET LoyaltyTierID = (SELECT TierID FROM LoyaltyTiers WHERE TierName = 'silver')
                WHERE LoyaltyTierID IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // WARNING: This rollback cannot restore deleted user points history or previous tier assignments
            // It only restores the 2-tier system structure (Member, VIP)

            // Step 1: Delete new 4 tiers
            migrationBuilder.Sql("DELETE FROM LoyaltyTiers;");

            // Step 2: Restore old 2-tier system (Member, VIP)
            migrationBuilder.Sql(@"
                INSERT INTO LoyaltyTiers (TierName, MinPoints, DiscountRate)
                VALUES
                    ('Member', 0, 0.00),
                    ('VIP', 200, 0.10);
            ");

            // Step 3: Set all users to Member tier (cannot restore previous tier assignments)
            migrationBuilder.Sql(@"
                UPDATE Users
                SET LoyaltyTierID = (SELECT TierID FROM LoyaltyTiers WHERE TierName = 'Member');
            ");
        }
    }
}
