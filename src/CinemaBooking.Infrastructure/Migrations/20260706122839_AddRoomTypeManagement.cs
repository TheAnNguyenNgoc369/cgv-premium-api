using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations;

public partial class AddRoomTypeManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_AdminActionLog_ActionType", table: "AdminActionLog");
        migrationBuilder.AddCheckConstraint(name: "CK_AdminActionLog_ActionType", table: "AdminActionLog", sql: "[ActionType] IN ('create_user','update_user','lock_user','unlock_user','change_role','delete_user','create_voucher','update_voucher','delete_voucher','create_showtime','update_showtime','delete_showtime','update_ticket_price','generate_seat','update_seat','delete_seat','create_cinema','update_cinema','delete_cinema','create_genre','update_genre','delete_genre','create_movie','update_movie','delete_movie','create_room_type','update_room_type','delete_room_type','export_report')");
        migrationBuilder.CreateTable(
            name: "RoomType",
            columns: table => new
            {
                RoomTypeID = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                TypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ExtraPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoomType", x => x.RoomTypeID);
                table.CheckConstraint("CK_RoomType_ExtraPrice", "[ExtraPrice] >= 0");
            });

        migrationBuilder.CreateIndex(name: "UQ_RoomType_TypeName", table: "RoomType", column: "TypeName", unique: true);
        migrationBuilder.Sql("""
            INSERT INTO [RoomType] ([TypeName], [ExtraPrice], [Description], [CreatedAt], [UpdatedAt])
            SELECT DISTINCT LTRIM(RTRIM([RoomType])),
                   0, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()
            FROM [Room]
            WHERE LTRIM(RTRIM([RoomType])) IN ('Standard', 'VIP', 'IMAX', '3D');
            IF NOT EXISTS (SELECT 1 FROM [RoomType] WHERE [TypeName] = 'Standard')
                INSERT INTO [RoomType] ([TypeName], [ExtraPrice], [Description], [CreatedAt], [UpdatedAt])
                VALUES ('Standard', 0, N'Phòng tiêu chuẩn', SYSUTCDATETIME(), SYSUTCDATETIME());
            """);

        migrationBuilder.AddColumn<int>(name: "RoomTypeID", table: "Room", type: "int", nullable: true);
        migrationBuilder.Sql("""
            UPDATE r SET [RoomTypeID] = COALESCE(rt.[RoomTypeID], standard.[RoomTypeID])
            FROM [Room] r
            LEFT JOIN [RoomType] rt ON rt.[TypeName] = LTRIM(RTRIM(r.[RoomType]))
                AND LTRIM(RTRIM(r.[RoomType])) IN ('Standard', 'VIP', 'IMAX', '3D')
            CROSS JOIN (SELECT TOP 1 [RoomTypeID] FROM [RoomType] WHERE [TypeName] = 'Standard') standard;
            """);
        migrationBuilder.AlterColumn<int>(name: "RoomTypeID", table: "Room", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);
        migrationBuilder.CreateIndex(name: "IX_Room_RoomTypeID", table: "Room", column: "RoomTypeID");
        migrationBuilder.AddForeignKey(name: "FK_Room_RoomType", table: "Room", column: "RoomTypeID", principalTable: "RoomType", principalColumn: "RoomTypeID");
        migrationBuilder.DropCheckConstraint(name: "CK_Room_RoomType", table: "Room");
        migrationBuilder.DropColumn(name: "RoomType", table: "Room");
        migrationBuilder.AddColumn<decimal>(name: "RoomExtraPrice", table: "Showtime", type: "decimal(18,2)", nullable: false, defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_AdminActionLog_ActionType", table: "AdminActionLog");
        migrationBuilder.AddCheckConstraint(name: "CK_AdminActionLog_ActionType", table: "AdminActionLog", sql: "[ActionType] IN ('create_user','update_user','lock_user','unlock_user','change_role','delete_user','create_voucher','update_voucher','delete_voucher','create_showtime','update_showtime','delete_showtime','update_ticket_price','generate_seat','update_seat','delete_seat','create_cinema','update_cinema','delete_cinema','create_genre','update_genre','delete_genre','create_movie','update_movie','delete_movie','export_report')");
        migrationBuilder.AddColumn<string>(name: "RoomType", table: "Room", type: "nvarchar(20)", maxLength: 20, nullable: true);
        migrationBuilder.Sql("UPDATE r SET [RoomType] = rt.[TypeName] FROM [Room] r JOIN [RoomType] rt ON rt.[RoomTypeID] = r.[RoomTypeID];");
        migrationBuilder.AlterColumn<string>(name: "RoomType", table: "Room", type: "nvarchar(20)", maxLength: 20, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(20)", oldMaxLength: 20, oldNullable: true);
        migrationBuilder.DropForeignKey(name: "FK_Room_RoomType", table: "Room");
        migrationBuilder.DropIndex(name: "IX_Room_RoomTypeID", table: "Room");
        migrationBuilder.DropColumn(name: "RoomTypeID", table: "Room");
        migrationBuilder.DropColumn(name: "RoomExtraPrice", table: "Showtime");
        migrationBuilder.DropTable(name: "RoomType");
        migrationBuilder.AddCheckConstraint(name: "CK_Room_RoomType", table: "Room", sql: "[RoomType] IN ('Standard', 'VIP', 'IMAX', '3D')");
    }
}
