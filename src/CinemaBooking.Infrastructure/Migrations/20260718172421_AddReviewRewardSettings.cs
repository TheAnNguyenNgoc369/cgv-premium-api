using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewRewardSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReviewRewardSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstReviewPoints = table.Column<int>(type: "int", nullable: false),
                    NextReviewPoints = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewRewardSettings", x => x.Id);
                    table.CheckConstraint("CK_ReviewRewardSettings_FirstReviewPoints", "[FirstReviewPoints] >= 0");
                    table.CheckConstraint("CK_ReviewRewardSettings_NextReviewPoints", "[NextReviewPoints] >= 0");
                    table.ForeignKey(
                        name: "FK_ReviewRewardSettings_Users",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.InsertData(
                table: "ReviewRewardSettings",
                columns: new[] { "Id", "FirstReviewPoints", "NextReviewPoints", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 1, 50, 10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewRewardSettings_UpdatedBy",
                table: "ReviewRewardSettings",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewRewardSettings");
        }
    }
}
