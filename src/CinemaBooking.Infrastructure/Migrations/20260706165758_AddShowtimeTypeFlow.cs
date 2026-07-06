using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShowtimeTypeFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint("CK_AdminActionLog_ActionType", "AdminActionLog");
            migrationBuilder.AddColumn<int>("ShowtimeTypeID", "Showtime", nullable: true);
            migrationBuilder.CreateTable("ShowtimeType", table => new { ShowtimeTypeID = table.Column<int>().Annotation("SqlServer:Identity", "1, 1"), CinemaID = table.Column<int>(), Name = table.Column<string>(maxLength: 100), IsActive = table.Column<bool>(defaultValue: true), CreatedAt = table.Column<DateTime>(defaultValueSql: "SYSUTCDATETIME()"), UpdatedAt = table.Column<DateTime>(defaultValueSql: "SYSUTCDATETIME()") }, constraints: table => { table.PrimaryKey("PK_ShowtimeType", x => x.ShowtimeTypeID); table.ForeignKey("FK_ShowtimeType_Cinema_CinemaID", x => x.CinemaID, "Cinema", "CinemaID"); });
            migrationBuilder.CreateTable("ShowtimeTypeSlot", table => new { SlotID = table.Column<int>().Annotation("SqlServer:Identity", "1, 1"), ShowtimeTypeID = table.Column<int>(), StartTime = table.Column<TimeSpan>(type: "time") }, constraints: table => { table.PrimaryKey("PK_ShowtimeTypeSlot", x => x.SlotID); table.ForeignKey("FK_ShowtimeTypeSlot_ShowtimeType_ShowtimeTypeID", x => x.ShowtimeTypeID, "ShowtimeType", "ShowtimeTypeID"); });
            migrationBuilder.CreateIndex("IX_Showtime_ShowtimeTypeID", "Showtime", "ShowtimeTypeID");
            migrationBuilder.CreateIndex("IX_ShowtimeType_CinemaID", "ShowtimeType", "CinemaID");
            migrationBuilder.CreateIndex("UQ_ShowtimeType_CinemaID_Name", "ShowtimeType", new[] { "CinemaID", "Name" }, unique: true);
            migrationBuilder.CreateIndex("UX_ShowtimeTypeSlot_ShowtimeTypeID_StartTime", "ShowtimeTypeSlot", new[] { "ShowtimeTypeID", "StartTime" }, unique: true);
            migrationBuilder.AddForeignKey(
                name: "FK_Showtime_ShowtimeType",
                table: "Showtime",
                column: "ShowtimeTypeID",
                principalTable: "ShowtimeType",
                principalColumn: "ShowtimeTypeID");
            migrationBuilder.AddCheckConstraint("CK_AdminActionLog_ActionType", "AdminActionLog", "[ActionType] IN ('create_showtime_type','update_showtime_type','delete_showtime_type','generate_showtime_by_type','create_user','update_user','lock_user','unlock_user','change_role','delete_user','create_voucher','update_voucher','delete_voucher','create_showtime','update_showtime','delete_showtime','update_ticket_price','generate_seat','update_seat','delete_seat','create_cinema','update_cinema','delete_cinema','create_genre','update_genre','delete_genre','create_movie','update_movie','delete_movie','create_room_type','update_room_type','delete_room_type','export_report')");
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint("CK_AdminActionLog_ActionType", "AdminActionLog");
            migrationBuilder.DropForeignKey("FK_Showtime_ShowtimeType", "Showtime");
            migrationBuilder.DropTable("ShowtimeTypeSlot");
            migrationBuilder.DropTable("ShowtimeType");
            migrationBuilder.DropIndex("IX_Showtime_ShowtimeTypeID", "Showtime");
            migrationBuilder.DropColumn("ShowtimeTypeID", "Showtime");
            migrationBuilder.AddCheckConstraint("CK_AdminActionLog_ActionType", "AdminActionLog", "[ActionType] IN ('create_user','update_user','lock_user','unlock_user','change_role','delete_user','create_voucher','update_voucher','delete_voucher','create_showtime','update_showtime','delete_showtime','update_ticket_price','generate_seat','update_seat','delete_seat','create_cinema','update_cinema','delete_cinema','create_genre','update_genre','delete_genre','create_movie','update_movie','delete_movie','create_room_type','update_room_type','delete_room_type','export_report')");
        }
    }
}
