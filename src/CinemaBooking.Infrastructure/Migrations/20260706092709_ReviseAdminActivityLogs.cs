using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations;

public partial class ReviseAdminActivityLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_AdminActionLog_ActionType", "AdminActionLog");
        migrationBuilder.Sql("UPDATE [AdminActionLog] SET [ActionType] = 'lock_user' WHERE [ActionType] IN ('account_status_changed','deactivate_user'); DELETE FROM [AdminActionLog] WHERE [ActionType] IN ('view_revenue_report','cancel_booking','refund_processed','payment_viewed','booking_created');");
        migrationBuilder.Sql("UPDATE [AdminActionLog] SET [IPAddress] = 'unknown' WHERE [IPAddress] IS NULL; UPDATE [AdminActionLog] SET [Description] = 'Legacy activity' WHERE [Description] IS NULL;");
        migrationBuilder.AlterColumn<string>("IPAddress", "AdminActionLog", "nvarchar(45)", maxLength: 45, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(45)", oldMaxLength: 45, oldNullable: true);
        migrationBuilder.AlterColumn<string>("Description", "AdminActionLog", "nvarchar(1000)", maxLength: 1000, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(1000)", oldMaxLength: 1000, oldNullable: true);
        migrationBuilder.AlterColumn<DateTime>("CreatedAt", "AdminActionLog", "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()", oldClrType: typeof(DateTime), oldType: "datetime2", oldDefaultValueSql: "GETDATE()");
        migrationBuilder.CreateIndex("IX_AdminActionLog_ActionType", "AdminActionLog", "ActionType");
        migrationBuilder.CreateIndex("IX_AdminActionLog_CreatedAt", "AdminActionLog", "CreatedAt");
        migrationBuilder.CreateIndex("IX_AdminActionLog_TargetTable_TargetID", "AdminActionLog", new[] { "TargetTable", "TargetID" });
        migrationBuilder.AddCheckConstraint("CK_AdminActionLog_ActionType", "AdminActionLog", "[ActionType] IN ('create_user','update_user','lock_user','unlock_user','change_role','delete_user','create_voucher','update_voucher','delete_voucher','create_showtime','update_showtime','delete_showtime','update_ticket_price','generate_seat','update_seat','delete_seat','create_cinema','update_cinema','delete_cinema','create_genre','update_genre','delete_genre','create_movie','update_movie','delete_movie','export_report')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_AdminActionLog_ActionType", "AdminActionLog");
        migrationBuilder.DropIndex("IX_AdminActionLog_CreatedAt", "AdminActionLog");
        migrationBuilder.DropIndex("IX_AdminActionLog_TargetTable_TargetID", "AdminActionLog");
        migrationBuilder.DropCheckConstraint("CK_AdminActionLog_ActionType", "AdminActionLog");
        migrationBuilder.AlterColumn<string>("IPAddress", "AdminActionLog", "nvarchar(45)", maxLength: 45, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(45)", oldMaxLength: 45);
        migrationBuilder.AlterColumn<string>("Description", "AdminActionLog", "nvarchar(1000)", maxLength: 1000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(1000)", oldMaxLength: 1000);
        migrationBuilder.AlterColumn<DateTime>("CreatedAt", "AdminActionLog", "datetime2", nullable: false, defaultValueSql: "GETDATE()", oldClrType: typeof(DateTime), oldType: "datetime2", oldDefaultValueSql: "SYSUTCDATETIME()");
        migrationBuilder.AddCheckConstraint("CK_AdminActionLog_ActionType", "AdminActionLog", "[ActionType] IN ('lock_user','unlock_user','change_role','cancel_booking','refund_processed','payment_viewed','booking_created','account_status_changed','create_user','update_user','delete_user','deactivate_user','create_voucher','update_voucher','delete_voucher','view_revenue_report','export_report')");
    }
}
