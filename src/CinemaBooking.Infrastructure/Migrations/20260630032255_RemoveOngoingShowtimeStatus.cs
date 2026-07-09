using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOngoingShowtimeStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Showtime_Status",
                table: "Showtime");

            migrationBuilder.Sql(
                """
                UPDATE [Showtime]
                SET [Status] = CASE
                    WHEN [StartTime] > SYSUTCDATETIME() THEN 'scheduled'
                    ELSE 'completed'
                END
                WHERE [Status] = 'ongoing';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Showtime_Status",
                table: "Showtime",
                sql: "[Status] IN ('scheduled', 'completed', 'cancelled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Showtime_Status",
                table: "Showtime");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Showtime_Status",
                table: "Showtime",
                sql: "[Status] IN ('scheduled', 'ongoing', 'completed', 'cancelled')");
        }
    }
}
