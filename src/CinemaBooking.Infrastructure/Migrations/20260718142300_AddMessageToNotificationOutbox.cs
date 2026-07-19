using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
        /// <inheritdoc />
        public partial class AddMessageToNotificationOutbox : Migration
        {
            /// <inheritdoc />
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                // No-op: all changes already applied in 20260718141736_AddNotificationOutboxMessageAndTypes
            }

            /// <inheritdoc />
            protected override void Down(MigrationBuilder migrationBuilder)
            {
                // No-op
            }
        }
}
