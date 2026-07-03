using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations;

[DbContext(typeof(CinemaBookingDbContext))]
[Migration("20260703120000_AddVoucherManagementAndReportAuditActions")]
public partial class AddVoucherManagementAndReportAuditActions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ImagePublicId", table: "Voucher",
            type: "nvarchar(255)", maxLength: 255, nullable: true);
        migrationBuilder.DropCheckConstraint(name: "CK_AdminActionLog_ActionType", table: "AdminActionLog");
        migrationBuilder.AddCheckConstraint(name: "CK_AdminActionLog_ActionType", table: "AdminActionLog",
            sql: "[ActionType] IN ('lock_user','unlock_user','change_role','cancel_booking','refund_processed','payment_viewed','booking_created','account_status_changed','create_user','update_user','delete_user','deactivate_user','create_voucher','update_voucher','delete_voucher','view_revenue_report','export_report')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ImagePublicId", table: "Voucher");
        migrationBuilder.DropCheckConstraint(name: "CK_AdminActionLog_ActionType", table: "AdminActionLog");
        migrationBuilder.AddCheckConstraint(name: "CK_AdminActionLog_ActionType", table: "AdminActionLog",
            sql: "[ActionType] IN ('lock_user','unlock_user','change_role','cancel_booking','refund_processed','payment_viewed','booking_created','account_status_changed','create_user','update_user','delete_user','deactivate_user')");
    }
}
