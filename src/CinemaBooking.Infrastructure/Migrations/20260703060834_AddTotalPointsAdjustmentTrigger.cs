using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalPointsAdjustmentTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [dbo].[TR_Users_TotalPoints_Adjustment]
                ON [dbo].[Users]
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF NOT UPDATE([TotalPoints])
                        RETURN;

                    IF TRY_CAST(SESSION_CONTEXT(N'SkipLoyaltyPointTrigger') AS bit) = 1
                        RETURN;

                    INSERT INTO [dbo].[LoyaltyPoints]
                        ([UserID], [BookingID], [PointsDelta], [TransactionType], [Description], [CreatedAt])
                    SELECT
                        inserted.[UserID],
                        NULL,
                        inserted.[TotalPoints] - deleted.[TotalPoints],
                        'adjust',
                        CONCAT(
                            'TotalPoints adjusted directly from ',
                            deleted.[TotalPoints],
                            ' to ',
                            inserted.[TotalPoints]),
                        GETUTCDATE()
                    FROM inserted
                    INNER JOIN deleted ON deleted.[UserID] = inserted.[UserID]
                    WHERE inserted.[TotalPoints] <> deleted.[TotalPoints];
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS [dbo].[TR_Users_TotalPoints_Adjustment];");
        }
    }
}
