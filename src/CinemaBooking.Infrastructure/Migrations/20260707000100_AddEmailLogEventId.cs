using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CinemaBookingDbContext))]
    [Migration("20260707000100_AddEmailLogEventId")]
    public partial class AddEmailLogEventId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventId",
                table: "EmailLog",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("UPDATE [EmailLog] SET [EventId] = CONCAT('legacy:', [EmailLogID]) WHERE [EventId] IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "EventId",
                table: "EmailLog",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailLog_EventId",
                table: "EmailLog",
                column: "EventId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailLog_EventId",
                table: "EmailLog");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "EmailLog");
        }
    }
}
