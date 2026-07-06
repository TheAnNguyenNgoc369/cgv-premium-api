using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationOutboxFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Notification_Type",
                table: "Notification");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Notification",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "ActionUrl",
                table: "Notification",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Notification",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventId",
                table: "Notification",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Notification",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.Sql("UPDATE [Notification] SET [EventId] = CONCAT('legacy:', [NotificationID]), [EventType] = 'Legacy' WHERE [EventId] IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "EventId",
                table: "Notification",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "Notification",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "Notification",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReferenceId",
                table: "Notification",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceType",
                table: "Notification",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HtmlBody",
                table: "EmailLog",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InlineImagesJson",
                table: "EmailLog",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                table: "EmailLog",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "EmailLog",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "NotificationOutbox",
                columns: table => new
                {
                    NotificationOutboxID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationOutbox", x => x.NotificationOutboxID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_EventId_UserID",
                table: "Notification",
                columns: new[] { "EventId", "UserID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserID_CreatedAt",
                table: "Notification",
                columns: new[] { "UserID", "CreatedAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notification_Type",
                table: "Notification",
                sql: "[Type] IN ('booking', 'payment', 'refund', 'promotion', 'system', 'account')");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLog_DeliveryStatus_NextAttemptAt",
                table: "EmailLog",
                columns: new[] { "DeliveryStatus", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutbox_EventId",
                table: "NotificationOutbox",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationOutbox_Status_NextAttemptAt",
                table: "NotificationOutbox",
                columns: new[] { "Status", "NextAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationOutbox");

            migrationBuilder.DropIndex(
                name: "IX_Notification_EventId_UserID",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IX_Notification_UserID_CreatedAt",
                table: "Notification");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Notification_Type",
                table: "Notification");

            migrationBuilder.DropIndex(
                name: "IX_EmailLog_DeliveryStatus_NextAttemptAt",
                table: "EmailLog");

            migrationBuilder.DropColumn(
                name: "ActionUrl",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "ReferenceType",
                table: "Notification");

            migrationBuilder.DropColumn(
                name: "HtmlBody",
                table: "EmailLog");

            migrationBuilder.DropColumn(
                name: "InlineImagesJson",
                table: "EmailLog");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "EmailLog");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "EmailLog");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Notification",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Notification_Type",
                table: "Notification",
                sql: "[Type] IN ('booking', 'payment', 'refund', 'promotion', 'system')");
        }
    }
}
