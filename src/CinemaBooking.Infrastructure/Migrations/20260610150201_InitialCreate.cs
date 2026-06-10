using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cinema",
                columns: table => new
                {
                    CinemaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CinemaName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "active"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cinema", x => x.CinemaID);
                    table.CheckConstraint("CK_Cinema_Status", "[Status] IN ('active', 'inactive', 'maintenance')");
                });

            migrationBuilder.CreateTable(
                name: "Genre",
                columns: table => new
                {
                    GenreID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GenreName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genre", x => x.GenreID);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTiers",
                columns: table => new
                {
                    TierID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TierName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MinPoints = table.Column<int>(type: "int", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "decimal(4,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTiers", x => x.TierID);
                    table.CheckConstraint("CK_LoyaltyTiers_DiscountRate", "[DiscountRate] >= 0 AND [DiscountRate] <= 1");
                    table.CheckConstraint("CK_LoyaltyTiers_MinPoints", "[MinPoints] >= 0");
                    table.CheckConstraint("CK_LoyaltyTiers_TierName", "[TierName] IN ('silver', 'gold', 'platinum', 'megavip')");
                });

            migrationBuilder.CreateTable(
                name: "Movie",
                columns: table => new
                {
                    MovieID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AgeRating = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Director = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Cast = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PosterURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrailerURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DurationMin = table.Column<int>(type: "int", nullable: false),
                    ShowingFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ShowingTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "coming_soon"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movie", x => x.MovieID);
                    table.CheckConstraint("CK_Movie_AgeRating", "[AgeRating] IN ('P', 'C13', 'C16', 'C18')");
                    table.CheckConstraint("CK_Movie_DurationMin", "[DurationMin] > 0");
                    table.CheckConstraint("CK_Movie_ShowingDate", "[ShowingFrom] IS NULL OR [ShowingTo] IS NULL OR [ShowingFrom] <= [ShowingTo]");
                    table.CheckConstraint("CK_Movie_Status", "[Status] IN ('coming_soon', 'now_showing', 'ended')");
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    ItemID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ImageURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsOnMenu = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsLoyaltyEligible = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "in_stock"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.ItemID);
                    table.CheckConstraint("CK_Product_ItemType", "[ItemType] IN ('combo', 'snack', 'beverage', 'dessert')");
                    table.CheckConstraint("CK_Product_Price", "[Price] >= 0");
                    table.CheckConstraint("CK_Product_Status", "[Status] IN ('in_stock', 'low_stock', 'out_of_stock', 'inactive')");
                    table.CheckConstraint("CK_Product_StockQuantity", "[StockQuantity] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "SeatType",
                columns: table => new
                {
                    SeatTypeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExtraPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatType", x => x.SeatTypeID);
                    table.CheckConstraint("CK_SeatType_ExtraPrice", "[ExtraPrice] >= 0");
                    table.CheckConstraint("CK_SeatType_TypeName", "[TypeName] IN ('standard', 'vip', 'couple')");
                });

            migrationBuilder.CreateTable(
                name: "Voucher",
                columns: table => new
                {
                    VoucherID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VoucherCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DiscountType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinOrderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    MaxUses = table.Column<int>(type: "int", nullable: true),
                    UsedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImageURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voucher", x => x.VoucherID);
                    table.CheckConstraint("CK_Voucher_Category", "[Category] IS NULL OR [Category] IN ('Discount', 'Combo', 'Cashback')");
                    table.CheckConstraint("CK_Voucher_DiscountType", "[DiscountType] IN ('percent', 'fixed')");
                    table.CheckConstraint("CK_Voucher_DiscountValue", "[DiscountValue] >= 0");
                    table.CheckConstraint("CK_Voucher_MaxUses", "[MaxUses] IS NULL OR [MaxUses] > 0");
                    table.CheckConstraint("CK_Voucher_MinOrderValue", "[MinOrderValue] IS NULL OR [MinOrderValue] >= 0");
                    table.CheckConstraint("CK_Voucher_UsedCount", "[UsedCount] >= 0");
                    table.CheckConstraint("CK_Voucher_ValidDate", "[ValidUntil] > [ValidFrom]");
                });

            migrationBuilder.CreateTable(
                name: "Room",
                columns: table => new
                {
                    RoomID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CinemaID = table.Column<int>(type: "int", nullable: false),
                    RoomName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoomType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "active"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Room", x => x.RoomID);
                    table.CheckConstraint("CK_Room_Capacity", "[Capacity] > 0");
                    table.CheckConstraint("CK_Room_RoomType", "[RoomType] IN ('Standard', 'VIP', 'IMAX', '3D')");
                    table.CheckConstraint("CK_Room_Status", "[Status] IN ('active', 'maintenance', 'inactive')");
                    table.ForeignKey(
                        name: "FK_Room_Cinema",
                        column: x => x.CinemaID,
                        principalTable: "Cinema",
                        principalColumn: "CinemaID");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AvatarURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CinemaID = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "unverified"),
                    LoyaltyTierID = table.Column<int>(type: "int", nullable: true),
                    TotalPoints = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.CheckConstraint("CK_Users_Role", "[Role] IN ('customer', 'staff', 'admin', 'manager')");
                    table.CheckConstraint("CK_Users_Status", "[Status] IN ('unverified', 'active', 'locked', 'inactive')");
                    table.CheckConstraint("CK_Users_TotalPoints", "[TotalPoints] >= 0");
                    table.ForeignKey(
                        name: "FK_Users_Cinema",
                        column: x => x.CinemaID,
                        principalTable: "Cinema",
                        principalColumn: "CinemaID");
                    table.ForeignKey(
                        name: "FK_Users_LoyaltyTiers",
                        column: x => x.LoyaltyTierID,
                        principalTable: "LoyaltyTiers",
                        principalColumn: "TierID");
                });

            migrationBuilder.CreateTable(
                name: "MovieGenre",
                columns: table => new
                {
                    MovieGenreID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MovieID = table.Column<int>(type: "int", nullable: false),
                    GenreID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieGenre", x => x.MovieGenreID);
                    table.ForeignKey(
                        name: "FK_MovieGenre_Genre",
                        column: x => x.GenreID,
                        principalTable: "Genre",
                        principalColumn: "GenreID");
                    table.ForeignKey(
                        name: "FK_MovieGenre_Movie",
                        column: x => x.MovieID,
                        principalTable: "Movie",
                        principalColumn: "MovieID");
                });

            migrationBuilder.CreateTable(
                name: "Seat",
                columns: table => new
                {
                    SeatID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomID = table.Column<int>(type: "int", nullable: false),
                    SeatRow = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    SeatCol = table.Column<int>(type: "int", nullable: false),
                    SeatTypeID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seat", x => x.SeatID);
                    table.CheckConstraint("CK_Seat_SeatCol", "[SeatCol] > 0");
                    table.CheckConstraint("CK_Seat_Status", "[Status] IN ('active', 'inactive')");
                    table.ForeignKey(
                        name: "FK_Seat_Room",
                        column: x => x.RoomID,
                        principalTable: "Room",
                        principalColumn: "RoomID");
                    table.ForeignKey(
                        name: "FK_Seat_SeatType",
                        column: x => x.SeatTypeID,
                        principalTable: "SeatType",
                        principalColumn: "SeatTypeID");
                });

            migrationBuilder.CreateTable(
                name: "Showtime",
                columns: table => new
                {
                    ShowtimeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MovieID = table.Column<int>(type: "int", nullable: false),
                    RoomID = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "scheduled"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Showtime", x => x.ShowtimeID);
                    table.CheckConstraint("CK_Showtime_BasePrice", "[BasePrice] >= 0");
                    table.CheckConstraint("CK_Showtime_Status", "[Status] IN ('scheduled', 'ongoing', 'completed', 'cancelled')");
                    table.CheckConstraint("CK_Showtime_Time", "[EndTime] > [StartTime]");
                    table.ForeignKey(
                        name: "FK_Showtime_Movie",
                        column: x => x.MovieID,
                        principalTable: "Movie",
                        principalColumn: "MovieID");
                    table.ForeignKey(
                        name: "FK_Showtime_Room",
                        column: x => x.RoomID,
                        principalTable: "Room",
                        principalColumn: "RoomID");
                });

            migrationBuilder.CreateTable(
                name: "AdminActionLog",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminID = table.Column<int>(type: "int", nullable: false),
                    TargetUserID = table.Column<int>(type: "int", nullable: true),
                    TargetTable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetID = table.Column<int>(type: "int", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminActionLog", x => x.LogID);
                    table.CheckConstraint("CK_AdminActionLog_ActionType", "[ActionType] IN ('lock_user','unlock_user','change_role','cancel_booking','refund_processed','payment_viewed','booking_created','account_status_changed')");
                    table.ForeignKey(
                        name: "FK_AdminActionLog_Admin",
                        column: x => x.AdminID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_AdminActionLog_TargetUser",
                        column: x => x.TargetUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "EmailLog",
                columns: table => new
                {
                    EmailLogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: true),
                    ToEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeliveryStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "sent"),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLog", x => x.EmailLogID);
                    table.CheckConstraint("CK_EmailLog_DeliveryStatus", "[DeliveryStatus] IN ('sent', 'failed', 'retrying')");
                    table.CheckConstraint("CK_EmailLog_EventType", "[EventType] IN ('register','booking_confirmed','booking_cancelled','forgot_password','refund_processed','points_earned','reward_redeemed')");
                    table.CheckConstraint("CK_EmailLog_RetryCount", "[RetryCount] >= 0");
                    table.ForeignKey(
                        name: "FK_EmailLog_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "EmailVerificationToken",
                columns: table => new
                {
                    TokenID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerificationToken", x => x.TokenID);
                    table.ForeignKey(
                        name: "FK_EmailVerificationToken_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    NotificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.NotificationID);
                    table.CheckConstraint("CK_Notification_Type", "[Type] IN ('booking', 'payment', 'refund', 'promotion', 'system')");
                    table.ForeignKey(
                        name: "FK_Notification_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetToken",
                columns: table => new
                {
                    TokenID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetToken", x => x.TokenID);
                    table.ForeignKey(
                        name: "FK_PasswordResetToken_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Wallet",
                columns: table => new
                {
                    WalletID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallet", x => x.WalletID);
                    table.CheckConstraint("CK_Wallet_Balance", "[Balance] >= 0");
                    table.ForeignKey(
                        name: "FK_Wallet_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Booking",
                columns: table => new
                {
                    BookingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    ShowtimeID = table.Column<int>(type: "int", nullable: false),
                    CreatedByStaffID = table.Column<int>(type: "int", nullable: true),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PointsEarned = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    PointsRedeemed = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Booking", x => x.BookingID);
                    table.CheckConstraint("CK_Booking_Amounts", "[SubTotal] >= 0 AND [DiscountAmount] >= 0 AND [FinalAmount] >= 0");
                    table.CheckConstraint("CK_Booking_Points", "ISNULL([PointsEarned],0) >= 0 AND ISNULL([PointsRedeemed],0) >= 0");
                    table.CheckConstraint("CK_Booking_Status", "[Status] IN ('pending', 'paid', 'cancelled', 'refunded', 'used', 'expired', 'payment_failed', 'partially_refunded')");
                    table.ForeignKey(
                        name: "FK_Booking_CreatedByStaff",
                        column: x => x.CreatedByStaffID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Booking_Showtime",
                        column: x => x.ShowtimeID,
                        principalTable: "Showtime",
                        principalColumn: "ShowtimeID");
                    table.ForeignKey(
                        name: "FK_Booking_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "SeatHold",
                columns: table => new
                {
                    HoldID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeatID = table.Column<int>(type: "int", nullable: false),
                    ShowtimeID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    HeldAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "holding")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatHold", x => x.HoldID);
                    table.CheckConstraint("CK_SeatHold_ExpiresAt", "[ExpiresAt] > [HeldAt]");
                    table.CheckConstraint("CK_SeatHold_Status", "[Status] IN ('holding', 'confirmed', 'released', 'expired')");
                    table.ForeignKey(
                        name: "FK_SeatHold_Seat",
                        column: x => x.SeatID,
                        principalTable: "Seat",
                        principalColumn: "SeatID");
                    table.ForeignKey(
                        name: "FK_SeatHold_Showtime",
                        column: x => x.ShowtimeID,
                        principalTable: "Showtime",
                        principalColumn: "ShowtimeID");
                    table.ForeignKey(
                        name: "FK_SeatHold_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "BookingFnB",
                columns: table => new
                {
                    BookingFnBID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    ItemID = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingFnB", x => x.BookingFnBID);
                    table.CheckConstraint("CK_BookingFnB_Price", "[UnitPrice] >= 0 AND [SubTotal] >= 0");
                    table.CheckConstraint("CK_BookingFnB_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_BookingFnB_Booking",
                        column: x => x.BookingID,
                        principalTable: "Booking",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_BookingFnB_Product",
                        column: x => x.ItemID,
                        principalTable: "Product",
                        principalColumn: "ItemID");
                });

            migrationBuilder.CreateTable(
                name: "BookingSeat",
                columns: table => new
                {
                    BookingSeatID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    SeatID = table.Column<int>(type: "int", nullable: false),
                    TicketPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingSeat", x => x.BookingSeatID);
                    table.CheckConstraint("CK_BookingSeat_TicketPrice", "[TicketPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_BookingSeat_Booking",
                        column: x => x.BookingID,
                        principalTable: "Booking",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_BookingSeat_Seat",
                        column: x => x.SeatID,
                        principalTable: "Seat",
                        principalColumn: "SeatID");
                });

            migrationBuilder.CreateTable(
                name: "BookingVoucher",
                columns: table => new
                {
                    BookingVoucherID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    VoucherID = table.Column<int>(type: "int", nullable: false),
                    DiscountApplied = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingVoucher", x => x.BookingVoucherID);
                    table.CheckConstraint("CK_BookingVoucher_DiscountApplied", "[DiscountApplied] >= 0");
                    table.ForeignKey(
                        name: "FK_BookingVoucher_Booking",
                        column: x => x.BookingID,
                        principalTable: "Booking",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_BookingVoucher_Voucher",
                        column: x => x.VoucherID,
                        principalTable: "Voucher",
                        principalColumn: "VoucherID");
                });

            migrationBuilder.CreateTable(
                name: "Invoice",
                columns: table => new
                {
                    InvoiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    InvoiceCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoice", x => x.InvoiceID);
                    table.CheckConstraint("CK_Invoice_Amount", "[TotalAmount] >= 0 AND [TaxAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_Invoice_Booking",
                        column: x => x.BookingID,
                        principalTable: "Booking",
                        principalColumn: "BookingID");
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPoints",
                columns: table => new
                {
                    PointID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    BookingID = table.Column<int>(type: "int", nullable: true),
                    PointsDelta = table.Column<int>(type: "int", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPoints", x => x.PointID);
                    table.CheckConstraint("CK_LoyaltyPoints_TransactionType", "[TransactionType] IN ('earn', 'redeem', 'expire', 'adjust')");
                    table.ForeignKey(
                        name: "FK_LoyaltyPoints_Booking",
                        column: x => x.BookingID,
                        principalTable: "Booking",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_LoyaltyPoints_Users",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    PaymentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.PaymentID);
                    table.CheckConstraint("CK_Payment_Amount", "[Amount] >= 0");
                    table.CheckConstraint("CK_Payment_Method", "[PaymentMethod] IN ('vnpay', 'momo', 'credit_card', 'banking', 'cash', 'wallet')");
                    table.CheckConstraint("CK_Payment_Status", "[Status] IN ('pending', 'success', 'failed', 'refunded', 'cancelled', 'expired')");
                    table.ForeignKey(
                        name: "FK_Payment_Booking",
                        column: x => x.BookingID,
                        principalTable: "Booking",
                        principalColumn: "BookingID");
                });

            migrationBuilder.CreateTable(
                name: "Ticket",
                columns: table => new
                {
                    TicketID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingSeatID = table.Column<int>(type: "int", nullable: false),
                    QRCode = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "valid"),
                    CheckedInAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckedInByID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ticket", x => x.TicketID);
                    table.CheckConstraint("CK_Ticket_Status", "[Status] IN ('valid', 'used', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_Ticket_BookingSeat",
                        column: x => x.BookingSeatID,
                        principalTable: "BookingSeat",
                        principalColumn: "BookingSeatID");
                    table.ForeignKey(
                        name: "FK_Ticket_CheckedInBy",
                        column: x => x.CheckedInByID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "PaymentSession",
                columns: table => new
                {
                    SessionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentID = table.Column<int>(type: "int", nullable: false),
                    GatewayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GatewayOrderNo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    QRCodeURL = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "waiting"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentSession", x => x.SessionID);
                    table.CheckConstraint("CK_PaymentSession_GatewayName", "[GatewayName] IN ('vnpay', 'momo')");
                    table.CheckConstraint("CK_PaymentSession_Status", "[Status] IN ('waiting', 'processing', 'completed', 'expired', 'cancelled')");
                    table.ForeignKey(
                        name: "FK_PaymentSession_Payment",
                        column: x => x.PaymentID,
                        principalTable: "Payment",
                        principalColumn: "PaymentID");
                });

            migrationBuilder.CreateTable(
                name: "Refund",
                columns: table => new
                {
                    RefundID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookingID = table.Column<int>(type: "int", nullable: false),
                    PaymentID = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "pending"),
                    ProcessedBy = table.Column<int>(type: "int", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refund", x => x.RefundID);
                    table.CheckConstraint("CK_Refund_Amount", "[Amount] >= 0");
                    table.CheckConstraint("CK_Refund_Status", "[Status] IN ('pending', 'approved', 'rejected', 'processing', 'completed', 'failed')");
                    table.ForeignKey(
                        name: "FK_Refund_Booking",
                        column: x => x.BookingID,
                        principalTable: "Booking",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_Refund_Payment",
                        column: x => x.PaymentID,
                        principalTable: "Payment",
                        principalColumn: "PaymentID");
                    table.ForeignKey(
                        name: "FK_Refund_ProcessedBy",
                        column: x => x.ProcessedBy,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "WalletTransaction",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WalletID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BookingID = table.Column<int>(type: "int", nullable: true),
                    RefundID = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransaction", x => x.TransactionID);
                    table.CheckConstraint("CK_WalletTransaction_BalanceAfter", "[BalanceAfter] >= 0");
                    table.CheckConstraint("CK_WalletTransaction_Type", "[TransactionType] IN ('top_up', 'payment', 'refund')");
                    table.ForeignKey(
                        name: "FK_WalletTransaction_Booking",
                        column: x => x.BookingID,
                        principalTable: "Booking",
                        principalColumn: "BookingID");
                    table.ForeignKey(
                        name: "FK_WalletTransaction_Refund",
                        column: x => x.RefundID,
                        principalTable: "Refund",
                        principalColumn: "RefundID");
                    table.ForeignKey(
                        name: "FK_WalletTransaction_Wallet",
                        column: x => x.WalletID,
                        principalTable: "Wallet",
                        principalColumn: "WalletID");
                });

            migrationBuilder.InsertData(
                table: "LoyaltyTiers",
                columns: new[] { "TierID", "DiscountRate", "MinPoints", "TierName" },
                values: new object[,]
                {
                    { 1, 0.00m, 0, "silver" },
                    { 2, 0.05m, 1000, "gold" },
                    { 3, 0.10m, 5000, "platinum" },
                    { 4, 0.15m, 10000, "megavip" }
                });

            migrationBuilder.InsertData(
                table: "SeatType",
                columns: new[] { "SeatTypeID", "TypeName" },
                values: new object[] { 1, "standard" });

            migrationBuilder.InsertData(
                table: "SeatType",
                columns: new[] { "SeatTypeID", "ExtraPrice", "TypeName" },
                values: new object[,]
                {
                    { 2, 20000m, "vip" },
                    { 3, 50000m, "couple" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminActionLog_AdminID",
                table: "AdminActionLog",
                column: "AdminID");

            migrationBuilder.CreateIndex(
                name: "IX_AdminActionLog_TargetUserID",
                table: "AdminActionLog",
                column: "TargetUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_CreatedByStaffID",
                table: "Booking",
                column: "CreatedByStaffID");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_ShowtimeID",
                table: "Booking",
                column: "ShowtimeID");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_Status",
                table: "Booking",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Booking_UserID",
                table: "Booking",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ_Booking_BookingCode",
                table: "Booking",
                column: "BookingCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingFnB_ItemID",
                table: "BookingFnB",
                column: "ItemID");

            migrationBuilder.CreateIndex(
                name: "UQ_BookingFnB_BookingID_ItemID",
                table: "BookingFnB",
                columns: new[] { "BookingID", "ItemID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingSeat_SeatID",
                table: "BookingSeat",
                column: "SeatID");

            migrationBuilder.CreateIndex(
                name: "UQ_BookingSeat_BookingID_SeatID",
                table: "BookingSeat",
                columns: new[] { "BookingID", "SeatID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingVoucher_VoucherID",
                table: "BookingVoucher",
                column: "VoucherID");

            migrationBuilder.CreateIndex(
                name: "UQ_BookingVoucher_BookingID",
                table: "BookingVoucher",
                column: "BookingID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailLog_UserID",
                table: "EmailLog",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationToken_UserID",
                table: "EmailVerificationToken",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ_EmailVerificationToken_Token",
                table: "EmailVerificationToken",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Genre_GenreName",
                table: "Genre",
                column: "GenreName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Invoice_BookingID",
                table: "Invoice",
                column: "BookingID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Invoice_InvoiceCode",
                table: "Invoice",
                column: "InvoiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPoints_BookingID",
                table: "LoyaltyPoints",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPoints_UserID",
                table: "LoyaltyPoints",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ_LoyaltyTiers_TierName",
                table: "LoyaltyTiers",
                column: "TierName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovieGenre_GenreID",
                table: "MovieGenre",
                column: "GenreID");

            migrationBuilder.CreateIndex(
                name: "UQ_MovieGenre_MovieID_GenreID",
                table: "MovieGenre",
                columns: new[] { "MovieID", "GenreID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserID_IsRead",
                table: "Notification",
                columns: new[] { "UserID", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetToken_UserID",
                table: "PasswordResetToken",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ_PasswordResetToken_Token",
                table: "PasswordResetToken",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Status",
                table: "Payment",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UQ_Payment_BookingID",
                table: "Payment",
                column: "BookingID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Payment_TransactionCode",
                table: "Payment",
                column: "TransactionCode",
                unique: true,
                filter: "[TransactionCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSession_PaymentID",
                table: "PaymentSession",
                column: "PaymentID");

            migrationBuilder.CreateIndex(
                name: "UQ_PaymentSession_GatewayOrderNo",
                table: "PaymentSession",
                column: "GatewayOrderNo",
                unique: true,
                filter: "[GatewayOrderNo] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Refund_BookingID",
                table: "Refund",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_Refund_PaymentID",
                table: "Refund",
                column: "PaymentID");

            migrationBuilder.CreateIndex(
                name: "IX_Refund_ProcessedBy",
                table: "Refund",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Room_CinemaID",
                table: "Room",
                column: "CinemaID");

            migrationBuilder.CreateIndex(
                name: "UQ_Room_CinemaID_RoomName",
                table: "Room",
                columns: new[] { "CinemaID", "RoomName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seat_RoomID",
                table: "Seat",
                column: "RoomID");

            migrationBuilder.CreateIndex(
                name: "IX_Seat_SeatTypeID",
                table: "Seat",
                column: "SeatTypeID");

            migrationBuilder.CreateIndex(
                name: "UQ_Seat_RoomID_SeatRow_SeatCol",
                table: "Seat",
                columns: new[] { "RoomID", "SeatRow", "SeatCol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatHold_ShowtimeID",
                table: "SeatHold",
                column: "ShowtimeID");

            migrationBuilder.CreateIndex(
                name: "IX_SeatHold_Status_Lookup",
                table: "SeatHold",
                columns: new[] { "SeatID", "ShowtimeID", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SeatHold_UserID",
                table: "SeatHold",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UQ_SeatHold_ActiveHolding",
                table: "SeatHold",
                columns: new[] { "SeatID", "ShowtimeID" },
                unique: true,
                filter: "[Status] = 'holding'");

            migrationBuilder.CreateIndex(
                name: "UQ_SeatType_TypeName",
                table: "SeatType",
                column: "TypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Showtime_Conflict_Check",
                table: "Showtime",
                columns: new[] { "RoomID", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Showtime_MovieID",
                table: "Showtime",
                column: "MovieID");

            migrationBuilder.CreateIndex(
                name: "IX_Showtime_RoomID_StartTime",
                table: "Showtime",
                columns: new[] { "RoomID", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Ticket_CheckedInByID",
                table: "Ticket",
                column: "CheckedInByID");

            migrationBuilder.CreateIndex(
                name: "UQ_Ticket_BookingSeatID",
                table: "Ticket",
                column: "BookingSeatID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Ticket_QRCode",
                table: "Ticket",
                column: "QRCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CinemaID",
                table: "Users",
                column: "CinemaID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LoyaltyTierID",
                table: "Users",
                column: "LoyaltyTierID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_Status",
                table: "Users",
                columns: new[] { "Role", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true,
                filter: "[Phone] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_Voucher_VoucherCode",
                table: "Voucher",
                column: "VoucherCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Wallet_UserID",
                table: "Wallet",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_BookingID",
                table: "WalletTransaction",
                column: "BookingID");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_RefundID",
                table: "WalletTransaction",
                column: "RefundID");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_WalletID",
                table: "WalletTransaction",
                column: "WalletID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminActionLog");

            migrationBuilder.DropTable(
                name: "BookingFnB");

            migrationBuilder.DropTable(
                name: "BookingVoucher");

            migrationBuilder.DropTable(
                name: "EmailLog");

            migrationBuilder.DropTable(
                name: "EmailVerificationToken");

            migrationBuilder.DropTable(
                name: "Invoice");

            migrationBuilder.DropTable(
                name: "LoyaltyPoints");

            migrationBuilder.DropTable(
                name: "MovieGenre");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "PasswordResetToken");

            migrationBuilder.DropTable(
                name: "PaymentSession");

            migrationBuilder.DropTable(
                name: "SeatHold");

            migrationBuilder.DropTable(
                name: "Ticket");

            migrationBuilder.DropTable(
                name: "WalletTransaction");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "Voucher");

            migrationBuilder.DropTable(
                name: "Genre");

            migrationBuilder.DropTable(
                name: "BookingSeat");

            migrationBuilder.DropTable(
                name: "Refund");

            migrationBuilder.DropTable(
                name: "Wallet");

            migrationBuilder.DropTable(
                name: "Seat");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "SeatType");

            migrationBuilder.DropTable(
                name: "Booking");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Showtime");

            migrationBuilder.DropTable(
                name: "LoyaltyTiers");

            migrationBuilder.DropTable(
                name: "Movie");

            migrationBuilder.DropTable(
                name: "Room");

            migrationBuilder.DropTable(
                name: "Cinema");
        }
    }
}
