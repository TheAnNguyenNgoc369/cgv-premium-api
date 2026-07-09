using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CinemaBookingDbContext))]
    [Migration("20260629110620_ExtendAdminActionLog")]
    public partial class ExtendAdminActionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AdminActionLog_ActionType",
                table: "AdminActionLog");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AdminActionLog",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AdminActionLog_ActionType",
                table: "AdminActionLog",
                sql: "[ActionType] IN ('lock_user','unlock_user','change_role','cancel_booking','refund_processed','payment_viewed','booking_created','account_status_changed','create_user','update_user','delete_user','deactivate_user')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AdminActionLog_ActionType",
                table: "AdminActionLog");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "AdminActionLog");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AdminActionLog_ActionType",
                table: "AdminActionLog",
                sql: "[ActionType] IN ('lock_user','unlock_user','change_role','cancel_booking','refund_processed','payment_viewed','booking_created','account_status_changed')");
        }
    }
}
