using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMovieReviewUserMovieUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_MovieReviews_UserId_MovieId",
                table: "MovieReviews");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UQ_MovieReviews_UserId_MovieId",
                table: "MovieReviews",
                columns: new[] { "UserId", "MovieId" },
                unique: true);
        }
    }
}
