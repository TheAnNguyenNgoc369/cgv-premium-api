using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBarCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BarCode",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_BarCode",
                table: "Users",
                column: "BarCode",
                unique: true,
                filter: "[BarCode] IS NOT NULL");

            // Generate barcodes for existing users
            migrationBuilder.Sql(@"
                DECLARE @id INT, @barcode NVARCHAR(50);
                DECLARE user_cursor CURSOR FOR
                SELECT UserID FROM Users WHERE Role = 'customer' AND BarCode IS NULL;
                
                OPEN user_cursor;
                FETCH NEXT FROM user_cursor INTO @id;
                
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @barcode = 'CGV' + RIGHT('000000' + CAST(@id AS NVARCHAR(6)), 6);
                    
                    -- Ensure uniqueness
                    WHILE EXISTS (SELECT 1 FROM Users WHERE BarCode = @barcode)
                    BEGIN
                        SET @id = @id + 1;
                        SET @barcode = 'CGV' + RIGHT('000000' + CAST(@id AS NVARCHAR(6)), 6);
                    END
                    
                    UPDATE Users SET BarCode = @barcode WHERE UserID = @id;
                    
                    FETCH NEXT FROM user_cursor INTO @id;
                END
                
                CLOSE user_cursor;
                DEALLOCATE user_cursor;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Users_BarCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BarCode",
                table: "Users");
        }
    }
}
