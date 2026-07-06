using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations;

public partial class CompleteEmailLogFlow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_EmailLog_DeliveryStatus", "EmailLog");
        migrationBuilder.DropCheckConstraint("CK_EmailLog_RetryCount", "EmailLog");

        migrationBuilder.AlterColumn<string>(
            name: "DeliveryStatus",
            table: "EmailLog",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "pending",
            oldClrType: typeof(string),
            oldType: "nvarchar(20)",
            oldMaxLength: 20,
            oldDefaultValue: "sent");

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "EmailLog",
            type: "datetime2",
            nullable: false,
            defaultValueSql: "SYSUTCDATETIME()",
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldDefaultValueSql: "GETDATE()");

        migrationBuilder.AddColumn<string>(
            name: "LastError",
            table: "EmailLog",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_EmailLog_DeliveryStatus",
            table: "EmailLog",
            sql: "[DeliveryStatus] IN ('pending', 'processing', 'sent', 'failed', 'retrying')");

        migrationBuilder.AddCheckConstraint(
            name: "CK_EmailLog_RetryCount",
            table: "EmailLog",
            sql: "[RetryCount] BETWEEN 0 AND 3");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_EmailLog_DeliveryStatus", "EmailLog");
        migrationBuilder.DropCheckConstraint("CK_EmailLog_RetryCount", "EmailLog");
        migrationBuilder.DropColumn("LastError", "EmailLog");

        migrationBuilder.AlterColumn<string>(
            name: "DeliveryStatus",
            table: "EmailLog",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "sent",
            oldClrType: typeof(string),
            oldType: "nvarchar(20)",
            oldMaxLength: 20,
            oldDefaultValue: "pending");

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreatedAt",
            table: "EmailLog",
            type: "datetime2",
            nullable: false,
            defaultValueSql: "GETDATE()",
            oldClrType: typeof(DateTime),
            oldType: "datetime2",
            oldDefaultValueSql: "SYSUTCDATETIME()");

        migrationBuilder.AddCheckConstraint(
            name: "CK_EmailLog_DeliveryStatus",
            table: "EmailLog",
            sql: "[DeliveryStatus] IN ('sent', 'failed', 'retrying')");

        migrationBuilder.AddCheckConstraint(
            name: "CK_EmailLog_RetryCount",
            table: "EmailLog",
            sql: "[RetryCount] >= 0");
    }
}
