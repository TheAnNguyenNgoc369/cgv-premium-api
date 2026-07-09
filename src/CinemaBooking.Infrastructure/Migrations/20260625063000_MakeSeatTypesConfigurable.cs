using CinemaBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CinemaBookingDbContext))]
    [Migration("20260625063000_MakeSeatTypesConfigurable")]
    public partial class MakeSeatTypesConfigurable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SeatType_TypeName",
                table: "SeatType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_SeatType_TypeName",
                table: "SeatType",
                sql: "[TypeName] IN ('standard', 'vip', 'couple')");
        }
    }
}
