using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeLoyaltyTiersDynamic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.check_constraints
                    WHERE name = 'CK_LoyaltyTiers_TierName'
                      AND parent_object_id = OBJECT_ID('dbo.LoyaltyTiers')
                )
                BEGIN
                    ALTER TABLE [LoyaltyTiers] DROP CONSTRAINT [CK_LoyaltyTiers_TierName];
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'UQ_LoyaltyTiers_TierName'
                      AND object_id = OBJECT_ID('dbo.LoyaltyTiers')
                )
                BEGIN
                    CREATE UNIQUE INDEX [UQ_LoyaltyTiers_TierName]
                        ON [LoyaltyTiers] ([TierName]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.check_constraints
                    WHERE name = 'CK_LoyaltyTiers_TierName'
                      AND parent_object_id = OBJECT_ID('dbo.LoyaltyTiers')
                )
                BEGIN
                    ALTER TABLE [LoyaltyTiers] ADD CONSTRAINT [CK_LoyaltyTiers_TierName]
                        CHECK ([TierName] IN ('silver', 'gold', 'platinum', 'megavip'));
                END
            ");
        }
    }
}
