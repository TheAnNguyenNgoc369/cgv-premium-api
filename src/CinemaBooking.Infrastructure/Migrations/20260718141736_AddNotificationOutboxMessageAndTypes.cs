using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationOutboxMessageAndTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "NotificationOutbox",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notification_Type",
                table: "Notification");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notification",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notification_Type",
                table: "Notification",
                sql: "[Type] IN ('booking', 'payment', 'refund', 'promotion', 'system', 'account', 'analytics', 'report', 'movie', 'showtime')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Notification_Type",
                table: "Notification");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notification",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notification_Type",
                table: "Notification",
                sql: "[Type] IN ('booking', 'payment', 'refund', 'promotion', 'system', 'account')");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "NotificationOutbox");
        }
    }
}
