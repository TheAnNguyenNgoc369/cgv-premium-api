
CREATE DATABASE CGVPremiumDB;
GO
USE CGVPremiumDB;
GO

DROP TABLE IF EXISTS Notification;
DROP TABLE IF EXISTS EmailLog;
DROP TABLE IF EXISTS AdminActionLog;
DROP TABLE IF EXISTS LoyaltyPoints;
DROP TABLE IF EXISTS BookingVoucher;
DROP TABLE IF EXISTS Voucher;
DROP TABLE IF EXISTS Refund;
DROP TABLE IF EXISTS Invoice;
DROP TABLE IF EXISTS PaymentSession;
DROP TABLE IF EXISTS Payment;
DROP TABLE IF EXISTS BookingFnB;
DROP TABLE IF EXISTS Product;
DROP TABLE IF EXISTS Ticket;
DROP TABLE IF EXISTS BookingSeat;
DROP TABLE IF EXISTS Booking;
DROP TABLE IF EXISTS SeatHold;
DROP TABLE IF EXISTS Showtime;
DROP TABLE IF EXISTS Seat;
DROP TABLE IF EXISTS SeatType;
DROP TABLE IF EXISTS Room;
DROP TABLE IF EXISTS MovieGenre;
DROP TABLE IF EXISTS Movie;
DROP TABLE IF EXISTS Genre;
DROP TABLE IF EXISTS WalletTransaction;
DROP TABLE IF EXISTS Wallet;
DROP TABLE IF EXISTS PasswordResetToken;
DROP TABLE IF EXISTS EmailVerificationToken;
DROP TABLE IF EXISTS Users;
DROP TABLE IF EXISTS LoyaltyTiers;
DROP TABLE IF EXISTS Cinema;
GO

CREATE TABLE Cinema (
    CinemaID INT IDENTITY(1,1) NOT NULL,
    CinemaName NVARCHAR(150) NOT NULL,
    Address NVARCHAR(255) NOT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Cinema_Status DEFAULT 'active',
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_Cinema_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL CONSTRAINT DF_Cinema_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Cinema PRIMARY KEY (CinemaID),
    CONSTRAINT CK_Cinema_Status CHECK (Status IN ('active', 'inactive', 'maintenance'))
);
GO

CREATE TABLE LoyaltyTiers (
    TierID INT IDENTITY(1,1) NOT NULL,
    TierName VARCHAR(20) NOT NULL,
    MinPoints INT NOT NULL,
    DiscountRate DECIMAL(4,2) NOT NULL,
    CONSTRAINT PK_LoyaltyTiers PRIMARY KEY (TierID),
    CONSTRAINT UQ_LoyaltyTiers_TierName UNIQUE (TierName),
    CONSTRAINT CK_LoyaltyTiers_TierName CHECK (TierName IN ('silver', 'gold', 'platinum', 'megavip')),
    CONSTRAINT CK_LoyaltyTiers_MinPoints CHECK (MinPoints >= 0),
    CONSTRAINT CK_LoyaltyTiers_DiscountRate CHECK (DiscountRate >= 0 AND DiscountRate <= 1)
);
GO

CREATE TABLE Users (
    UserID INT IDENTITY(1,1) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    Phone VARCHAR(15) NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    AvatarURL VARCHAR(500) NULL,
    Role VARCHAR(20) NOT NULL,
    CinemaID INT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Users_Status DEFAULT 'unverified',
    LoyaltyTierID INT NULL,
    TotalPoints INT NOT NULL CONSTRAINT DF_Users_TotalPoints DEFAULT 0,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Users PRIMARY KEY (UserID),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT UQ_Users_Phone UNIQUE (Phone),
    CONSTRAINT FK_Users_Cinema FOREIGN KEY (CinemaID) REFERENCES Cinema(CinemaID),
    CONSTRAINT FK_Users_LoyaltyTiers FOREIGN KEY (LoyaltyTierID) REFERENCES LoyaltyTiers(TierID),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('customer', 'staff', 'admin', 'manager')),
    CONSTRAINT CK_Users_Status CHECK (Status IN ('unverified', 'active', 'locked', 'inactive')),
    CONSTRAINT CK_Users_TotalPoints CHECK (TotalPoints >= 0)
);
GO

CREATE TABLE EmailVerificationToken (
    TokenID INT IDENTITY(1,1) NOT NULL,
    UserID INT NOT NULL,
    Token VARCHAR(255) NOT NULL,
    ExpiresAt DATETIME NOT NULL,
    VerifiedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_EmailVerificationToken_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_EmailVerificationToken PRIMARY KEY (TokenID),
    CONSTRAINT UQ_EmailVerificationToken_Token UNIQUE (Token),
    CONSTRAINT FK_EmailVerificationToken_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

CREATE TABLE PasswordResetToken (
    TokenID INT IDENTITY(1,1) NOT NULL,
    UserID INT NOT NULL,
    Token VARCHAR(255) NOT NULL,
    ExpiresAt DATETIME NOT NULL,
    UsedAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_PasswordResetToken_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_PasswordResetToken PRIMARY KEY (TokenID),
    CONSTRAINT UQ_PasswordResetToken_Token UNIQUE (Token),
    CONSTRAINT FK_PasswordResetToken_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

CREATE TABLE Wallet (
    WalletID INT IDENTITY(1,1) NOT NULL,
    UserID INT NOT NULL,
    Balance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Wallet_Balance DEFAULT 0,
    CONSTRAINT PK_Wallet PRIMARY KEY (WalletID),
    CONSTRAINT UQ_Wallet_UserID UNIQUE (UserID),
    CONSTRAINT FK_Wallet_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT CK_Wallet_Balance CHECK (Balance >= 0)
);
GO

CREATE TABLE Genre (
    GenreID INT IDENTITY(1,1) NOT NULL,
    GenreName NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_Genre PRIMARY KEY (GenreID),
    CONSTRAINT UQ_Genre_GenreName UNIQUE (GenreName)
);
GO

CREATE TABLE Movie (
    MovieID INT IDENTITY(1,1) NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    AgeRating VARCHAR(10) NOT NULL,
    Director NVARCHAR(100) NULL,
    Cast NVARCHAR(MAX) NULL,
    Description NVARCHAR(MAX) NULL,
    PosterURL VARCHAR(500) NULL,
    TrailerURL VARCHAR(500) NULL,
    DurationMin INT NOT NULL,
    ShowingFrom DATE NULL,
    ShowingTo DATE NULL,
    Status VARCHAR(30) NOT NULL CONSTRAINT DF_Movie_Status DEFAULT 'coming_soon',
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_Movie_CreatedAt DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL CONSTRAINT DF_Movie_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Movie PRIMARY KEY (MovieID),
    CONSTRAINT CK_Movie_AgeRating CHECK (AgeRating IN ('P', 'C13', 'C16', 'C18')),
    CONSTRAINT CK_Movie_DurationMin CHECK (DurationMin > 0),
    CONSTRAINT CK_Movie_Status CHECK (Status IN ('coming_soon', 'now_showing', 'ended')),
    CONSTRAINT CK_Movie_ShowingDate CHECK (ShowingFrom IS NULL OR ShowingTo IS NULL OR ShowingFrom <= ShowingTo)
);
GO

CREATE TABLE MovieGenre (
    MovieGenreID INT IDENTITY(1,1) NOT NULL,
    MovieID INT NOT NULL,
    GenreID INT NOT NULL,
    CONSTRAINT PK_MovieGenre PRIMARY KEY (MovieGenreID),
    CONSTRAINT FK_MovieGenre_Movie FOREIGN KEY (MovieID) REFERENCES Movie(MovieID),
    CONSTRAINT FK_MovieGenre_Genre FOREIGN KEY (GenreID) REFERENCES Genre(GenreID),
    CONSTRAINT UQ_MovieGenre_MovieID_GenreID UNIQUE (MovieID, GenreID)
);
GO

CREATE TABLE Room (
    RoomID INT IDENTITY(1,1) NOT NULL,
    CinemaID INT NOT NULL,
    RoomName NVARCHAR(50) NOT NULL,
    RoomType VARCHAR(20) NOT NULL,
    Capacity INT NOT NULL,
    Description NVARCHAR(500) NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Room_Status DEFAULT 'active',
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_Room_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Room PRIMARY KEY (RoomID),
    CONSTRAINT FK_Room_Cinema FOREIGN KEY (CinemaID) REFERENCES Cinema(CinemaID),
    CONSTRAINT UQ_Room_CinemaID_RoomName UNIQUE (CinemaID, RoomName),
    CONSTRAINT CK_Room_RoomType CHECK (RoomType IN ('Standard', 'VIP', 'IMAX', '3D')),
    CONSTRAINT CK_Room_Capacity CHECK (Capacity > 0),
    CONSTRAINT CK_Room_Status CHECK (Status IN ('active', 'maintenance', 'inactive'))
);
GO

CREATE TABLE SeatType (
    SeatTypeID INT IDENTITY(1,1) NOT NULL,
    TypeName VARCHAR(20) NOT NULL,
    ExtraPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_SeatType_ExtraPrice DEFAULT 0,
    CONSTRAINT PK_SeatType PRIMARY KEY (SeatTypeID),
    CONSTRAINT UQ_SeatType_TypeName UNIQUE (TypeName),
    CONSTRAINT CK_SeatType_TypeName CHECK (TypeName IN ('standard', 'vip', 'couple')),
    CONSTRAINT CK_SeatType_ExtraPrice CHECK (ExtraPrice >= 0)
);
GO

CREATE TABLE Seat (
    SeatID INT IDENTITY(1,1) NOT NULL,
    RoomID INT NOT NULL,
    SeatRow VARCHAR(5) NOT NULL,
    SeatCol INT NOT NULL,
    SeatTypeID INT NOT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Seat_Status DEFAULT 'active',
    CONSTRAINT PK_Seat PRIMARY KEY (SeatID),
    CONSTRAINT FK_Seat_Room FOREIGN KEY (RoomID) REFERENCES Room(RoomID),
    CONSTRAINT FK_Seat_SeatType FOREIGN KEY (SeatTypeID) REFERENCES SeatType(SeatTypeID),
    CONSTRAINT UQ_Seat_RoomID_SeatRow_SeatCol UNIQUE (RoomID, SeatRow, SeatCol),
    CONSTRAINT CK_Seat_SeatCol CHECK (SeatCol > 0),
    CONSTRAINT CK_Seat_Status CHECK (Status IN ('active', 'inactive'))
);
GO

CREATE TABLE Showtime (
    ShowtimeID INT IDENTITY(1,1) NOT NULL,
    MovieID INT NOT NULL,
    RoomID INT NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    BasePrice DECIMAL(18,2) NOT NULL,
    Status VARCHAR(30) NOT NULL CONSTRAINT DF_Showtime_Status DEFAULT 'scheduled',
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_Showtime_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Showtime PRIMARY KEY (ShowtimeID),
    CONSTRAINT FK_Showtime_Movie FOREIGN KEY (MovieID) REFERENCES Movie(MovieID),
    CONSTRAINT FK_Showtime_Room FOREIGN KEY (RoomID) REFERENCES Room(RoomID),
    CONSTRAINT CK_Showtime_Time CHECK (EndTime > StartTime),
    CONSTRAINT CK_Showtime_BasePrice CHECK (BasePrice >= 0),
    CONSTRAINT CK_Showtime_Status CHECK (Status IN ('scheduled', 'ongoing', 'completed', 'cancelled'))
);
GO

CREATE INDEX IX_Showtime_Conflict_Check ON Showtime(RoomID, StartTime, EndTime);
GO

CREATE TABLE SeatHold (
    HoldID INT IDENTITY(1,1) NOT NULL,
    SeatID INT NOT NULL,
    ShowtimeID INT NOT NULL,
    UserID INT NOT NULL,
    HeldAt DATETIME NOT NULL CONSTRAINT DF_SeatHold_HeldAt DEFAULT GETDATE(),
    ExpiresAt DATETIME NOT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_SeatHold_Status DEFAULT 'holding',
    CONSTRAINT PK_SeatHold PRIMARY KEY (HoldID),
    CONSTRAINT FK_SeatHold_Seat FOREIGN KEY (SeatID) REFERENCES Seat(SeatID),
    CONSTRAINT FK_SeatHold_Showtime FOREIGN KEY (ShowtimeID) REFERENCES Showtime(ShowtimeID),
    CONSTRAINT FK_SeatHold_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT CK_SeatHold_Status CHECK (Status IN ('holding', 'confirmed', 'released', 'expired')),
    CONSTRAINT CK_SeatHold_ExpiresAt CHECK (ExpiresAt > HeldAt)
);
GO

CREATE INDEX IX_SeatHold_Status_Lookup ON SeatHold(SeatID, ShowtimeID, Status);
GO

CREATE UNIQUE INDEX UQ_SeatHold_ActiveHolding
ON SeatHold(SeatID, ShowtimeID)
WHERE Status = 'holding';
GO

CREATE TABLE Booking (
    BookingID INT IDENTITY(1,1) NOT NULL,
    BookingCode VARCHAR(50) NOT NULL,
    UserID INT NOT NULL,
    ShowtimeID INT NOT NULL,
    CreatedByStaffID INT NULL,
    SubTotal DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Booking_DiscountAmount DEFAULT 0,
    FinalAmount DECIMAL(18,2) NOT NULL,
    PointsEarned INT NULL CONSTRAINT DF_Booking_PointsEarned DEFAULT 0,
    PointsRedeemed INT NULL CONSTRAINT DF_Booking_PointsRedeemed DEFAULT 0,
    Status VARCHAR(30) NOT NULL CONSTRAINT DF_Booking_Status DEFAULT 'pending',
    BookingDate DATETIME NOT NULL CONSTRAINT DF_Booking_BookingDate DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL CONSTRAINT DF_Booking_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Booking PRIMARY KEY (BookingID),
    CONSTRAINT UQ_Booking_BookingCode UNIQUE (BookingCode),
    CONSTRAINT FK_Booking_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_Booking_Showtime FOREIGN KEY (ShowtimeID) REFERENCES Showtime(ShowtimeID),
    CONSTRAINT FK_Booking_CreatedByStaff FOREIGN KEY (CreatedByStaffID) REFERENCES Users(UserID),
    CONSTRAINT CK_Booking_Amounts CHECK (SubTotal >= 0 AND DiscountAmount >= 0 AND FinalAmount >= 0),
    CONSTRAINT CK_Booking_Points CHECK (ISNULL(PointsEarned,0) >= 0 AND ISNULL(PointsRedeemed,0) >= 0),
    CONSTRAINT CK_Booking_Status CHECK (Status IN ('pending', 'paid', 'cancelled', 'refunded', 'used', 'expired', 'payment_failed', 'partially_refunded'))
);
GO

CREATE TABLE BookingSeat (
    BookingSeatID INT IDENTITY(1,1) NOT NULL,
    BookingID INT NOT NULL,
    SeatID INT NOT NULL,
    TicketPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_BookingSeat PRIMARY KEY (BookingSeatID),
    CONSTRAINT FK_BookingSeat_Booking FOREIGN KEY (BookingID) REFERENCES Booking(BookingID),
    CONSTRAINT FK_BookingSeat_Seat FOREIGN KEY (SeatID) REFERENCES Seat(SeatID),
    CONSTRAINT UQ_BookingSeat_BookingID_SeatID UNIQUE (BookingID, SeatID),
    CONSTRAINT CK_BookingSeat_TicketPrice CHECK (TicketPrice >= 0)
);
GO

CREATE TABLE Ticket (
    TicketID INT IDENTITY(1,1) NOT NULL,
    BookingSeatID INT NOT NULL,
    QRCode VARCHAR(500) NOT NULL,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Ticket_Status DEFAULT 'valid',
    CheckedInAt DATETIME NULL,
    CheckedInByID INT NULL,
    CONSTRAINT PK_Ticket PRIMARY KEY (TicketID),
    CONSTRAINT UQ_Ticket_BookingSeatID UNIQUE (BookingSeatID),
    CONSTRAINT UQ_Ticket_QRCode UNIQUE (QRCode),
    CONSTRAINT FK_Ticket_BookingSeat FOREIGN KEY (BookingSeatID) REFERENCES BookingSeat(BookingSeatID),
    CONSTRAINT FK_Ticket_CheckedInBy FOREIGN KEY (CheckedInByID) REFERENCES Users(UserID),
    CONSTRAINT CK_Ticket_Status CHECK (Status IN ('valid', 'used', 'cancelled'))
);
GO

CREATE TABLE Product (
    ItemID INT IDENTITY(1,1) NOT NULL,
    ItemName NVARCHAR(150) NOT NULL,
    ItemType VARCHAR(30) NOT NULL,
    Description NVARCHAR(500) NULL,
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT NOT NULL CONSTRAINT DF_Product_StockQuantity DEFAULT 0,
    ImageURL VARCHAR(500) NULL,
    IsOnMenu BIT NOT NULL CONSTRAINT DF_Product_IsOnMenu DEFAULT 1,
    IsLoyaltyEligible BIT NOT NULL CONSTRAINT DF_Product_IsLoyaltyEligible DEFAULT 0,
    Status VARCHAR(20) NOT NULL CONSTRAINT DF_Product_Status DEFAULT 'in_stock',
    UpdatedAt DATETIME NOT NULL CONSTRAINT DF_Product_UpdatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Product PRIMARY KEY (ItemID),
    CONSTRAINT CK_Product_ItemType CHECK (ItemType IN ('combo', 'snack', 'beverage', 'dessert')),
    CONSTRAINT CK_Product_Price CHECK (Price >= 0),
    CONSTRAINT CK_Product_StockQuantity CHECK (StockQuantity >= 0),
    CONSTRAINT CK_Product_Status CHECK (Status IN ('in_stock', 'low_stock', 'out_of_stock', 'inactive'))
);
GO

CREATE TABLE BookingFnB (
    BookingFnBID INT IDENTITY(1,1) NOT NULL,
    BookingID INT NOT NULL,
    ItemID INT NOT NULL,
    Quantity INT NOT NULL CONSTRAINT DF_BookingFnB_Quantity DEFAULT 1,
    UnitPrice DECIMAL(18,2) NOT NULL,
    SubTotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_BookingFnB PRIMARY KEY (BookingFnBID),
    CONSTRAINT FK_BookingFnB_Booking FOREIGN KEY (BookingID) REFERENCES Booking(BookingID),
    CONSTRAINT FK_BookingFnB_Product FOREIGN KEY (ItemID) REFERENCES Product(ItemID),
    CONSTRAINT UQ_BookingFnB_BookingID_ItemID UNIQUE (BookingID, ItemID),
    CONSTRAINT CK_BookingFnB_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_BookingFnB_Price CHECK (UnitPrice >= 0 AND SubTotal >= 0)
);
GO

CREATE TABLE Payment (
    PaymentID INT IDENTITY(1,1) NOT NULL,
    BookingID INT NOT NULL,
    PaymentMethod VARCHAR(50) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TransactionCode VARCHAR(200) NULL,
    Status VARCHAR(30) NOT NULL CONSTRAINT DF_Payment_Status DEFAULT 'pending',
    PaidAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_Payment_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Payment PRIMARY KEY (PaymentID),
    CONSTRAINT UQ_Payment_BookingID UNIQUE (BookingID),
    CONSTRAINT UQ_Payment_TransactionCode UNIQUE (TransactionCode),
    CONSTRAINT FK_Payment_Booking FOREIGN KEY (BookingID) REFERENCES Booking(BookingID),
    CONSTRAINT CK_Payment_Method CHECK (PaymentMethod IN ('vnpay', 'momo', 'credit_card', 'banking', 'cash', 'wallet')),
    CONSTRAINT CK_Payment_Amount CHECK (Amount >= 0),
    CONSTRAINT CK_Payment_Status CHECK (Status IN ('pending', 'success', 'failed', 'refunded', 'cancelled', 'expired'))
);
GO

CREATE TABLE PaymentSession (
    SessionID INT IDENTITY(1,1) NOT NULL,
    PaymentID INT NOT NULL,
    GatewayName VARCHAR(50) NOT NULL,
    GatewayOrderNo VARCHAR(200) NULL,
    QRCodeURL VARCHAR(500) NULL,
    ExpiresAt DATETIME NOT NULL,
    Status VARCHAR(30) NOT NULL CONSTRAINT DF_PaymentSession_Status DEFAULT 'waiting',
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_PaymentSession_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_PaymentSession PRIMARY KEY (SessionID),
    CONSTRAINT UQ_PaymentSession_GatewayOrderNo UNIQUE (GatewayOrderNo),
    CONSTRAINT FK_PaymentSession_Payment FOREIGN KEY (PaymentID) REFERENCES Payment(PaymentID),
    CONSTRAINT CK_PaymentSession_GatewayName CHECK (GatewayName IN ('vnpay', 'momo')),
    CONSTRAINT CK_PaymentSession_Status CHECK (Status IN ('waiting', 'processing', 'completed', 'expired', 'cancelled'))
);
GO

CREATE TABLE Invoice (
    InvoiceID INT IDENTITY(1,1) NOT NULL,
    BookingID INT NOT NULL,
    InvoiceCode VARCHAR(50) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoice_TaxAmount DEFAULT 0,
    IssuedAt DATETIME NOT NULL CONSTRAINT DF_Invoice_IssuedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Invoice PRIMARY KEY (InvoiceID),
    CONSTRAINT UQ_Invoice_BookingID UNIQUE (BookingID),
    CONSTRAINT UQ_Invoice_InvoiceCode UNIQUE (InvoiceCode),
    CONSTRAINT FK_Invoice_Booking FOREIGN KEY (BookingID) REFERENCES Booking(BookingID),
    CONSTRAINT CK_Invoice_Amount CHECK (TotalAmount >= 0 AND TaxAmount >= 0)
);
GO

CREATE TABLE Refund (
    RefundID INT IDENTITY(1,1) NOT NULL,
    BookingID INT NOT NULL,
    PaymentID INT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Reason NVARCHAR(255) NULL,
    Status VARCHAR(30) NOT NULL CONSTRAINT DF_Refund_Status DEFAULT 'pending',
    ProcessedBy INT NULL,
    RequestedAt DATETIME NOT NULL CONSTRAINT DF_Refund_RequestedAt DEFAULT GETDATE(),
    CompletedAt DATETIME NULL,
    CONSTRAINT PK_Refund PRIMARY KEY (RefundID),
    CONSTRAINT FK_Refund_Booking FOREIGN KEY (BookingID) REFERENCES Booking(BookingID),
    CONSTRAINT FK_Refund_Payment FOREIGN KEY (PaymentID) REFERENCES Payment(PaymentID),
    CONSTRAINT FK_Refund_ProcessedBy FOREIGN KEY (ProcessedBy) REFERENCES Users(UserID),
    CONSTRAINT CK_Refund_Amount CHECK (Amount >= 0),
    CONSTRAINT CK_Refund_Status CHECK (Status IN ('pending', 'approved', 'rejected', 'processing', 'completed', 'failed'))
);
GO

CREATE TABLE WalletTransaction (
    TransactionID INT IDENTITY(1,1) NOT NULL,
    WalletID INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    BalanceAfter DECIMAL(18,2) NOT NULL,
    TransactionType VARCHAR(30) NOT NULL,
    BookingID INT NULL,
    RefundID INT NULL,
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_WalletTransaction_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_WalletTransaction PRIMARY KEY (TransactionID),
    CONSTRAINT FK_WalletTransaction_Wallet FOREIGN KEY (WalletID) REFERENCES Wallet(WalletID),
    CONSTRAINT FK_WalletTransaction_Booking FOREIGN KEY (BookingID) REFERENCES Booking(BookingID),
    CONSTRAINT FK_WalletTransaction_Refund FOREIGN KEY (RefundID) REFERENCES Refund(RefundID),
    CONSTRAINT CK_WalletTransaction_Type CHECK (TransactionType IN ('top_up', 'payment', 'refund')),
    CONSTRAINT CK_WalletTransaction_BalanceAfter CHECK (BalanceAfter >= 0)
);
GO

CREATE TABLE Voucher (
    VoucherID INT IDENTITY(1,1) NOT NULL,
    VoucherCode VARCHAR(50) NOT NULL,
    Category VARCHAR(50) NULL,
    DiscountType VARCHAR(20) NOT NULL,
    DiscountValue DECIMAL(18,2) NOT NULL,
    MinOrderValue DECIMAL(18,2) NULL CONSTRAINT DF_Voucher_MinOrderValue DEFAULT 0,
    MaxUses INT NULL,
    UsedCount INT NOT NULL CONSTRAINT DF_Voucher_UsedCount DEFAULT 0,
    ValidFrom DATETIME NOT NULL,
    ValidUntil DATETIME NOT NULL,
    ImageURL VARCHAR(500) NULL,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Voucher_IsActive DEFAULT 1,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_Voucher_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Voucher PRIMARY KEY (VoucherID),
    CONSTRAINT UQ_Voucher_VoucherCode UNIQUE (VoucherCode),
    CONSTRAINT CK_Voucher_Category CHECK (Category IS NULL OR Category IN ('Discount', 'Combo', 'Cashback')),
    CONSTRAINT CK_Voucher_DiscountType CHECK (DiscountType IN ('percent', 'fixed')),
    CONSTRAINT CK_Voucher_DiscountValue CHECK (DiscountValue >= 0),
    CONSTRAINT CK_Voucher_MinOrderValue CHECK (MinOrderValue IS NULL OR MinOrderValue >= 0),
    CONSTRAINT CK_Voucher_MaxUses CHECK (MaxUses IS NULL OR MaxUses > 0),
    CONSTRAINT CK_Voucher_UsedCount CHECK (UsedCount >= 0),
    CONSTRAINT CK_Voucher_ValidDate CHECK (ValidUntil > ValidFrom)
);
GO

CREATE TABLE BookingVoucher (
    BookingVoucherID INT IDENTITY(1,1) NOT NULL,
    BookingID INT NOT NULL,
    VoucherID INT NOT NULL,
    DiscountApplied DECIMAL(18,2) NOT NULL,
    UsedAt DATETIME NOT NULL CONSTRAINT DF_BookingVoucher_UsedAt DEFAULT GETDATE(),
    CONSTRAINT PK_BookingVoucher PRIMARY KEY (BookingVoucherID),
    CONSTRAINT FK_BookingVoucher_Booking FOREIGN KEY (BookingID) REFERENCES Booking(BookingID),
    CONSTRAINT FK_BookingVoucher_Voucher FOREIGN KEY (VoucherID) REFERENCES Voucher(VoucherID),
    CONSTRAINT UQ_BookingVoucher_BookingID UNIQUE (BookingID),
    CONSTRAINT CK_BookingVoucher_DiscountApplied CHECK (DiscountApplied >= 0)
);
GO

CREATE TABLE LoyaltyPoints (
    PointID INT IDENTITY(1,1) NOT NULL,
    UserID INT NOT NULL,
    BookingID INT NULL,
    PointsDelta INT NOT NULL,
    TransactionType VARCHAR(20) NOT NULL,
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_LoyaltyPoints_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_LoyaltyPoints PRIMARY KEY (PointID),
    CONSTRAINT FK_LoyaltyPoints_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_LoyaltyPoints_Booking FOREIGN KEY (BookingID) REFERENCES Booking(BookingID),
    CONSTRAINT CK_LoyaltyPoints_TransactionType CHECK (TransactionType IN ('earn', 'redeem', 'expire', 'adjust'))
);
GO

CREATE TABLE AdminActionLog (
    LogID INT IDENTITY(1,1) NOT NULL,
    AdminID INT NOT NULL,
    TargetUserID INT NULL,
    TargetTable VARCHAR(50) NULL,
    TargetID INT NULL,
    ActionType VARCHAR(50) NOT NULL,
    IPAddress VARCHAR(45) NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_AdminActionLog_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_AdminActionLog PRIMARY KEY (LogID),
    CONSTRAINT FK_AdminActionLog_Admin FOREIGN KEY (AdminID) REFERENCES Users(UserID),
    CONSTRAINT FK_AdminActionLog_TargetUser FOREIGN KEY (TargetUserID) REFERENCES Users(UserID),
    CONSTRAINT CK_AdminActionLog_ActionType CHECK (ActionType IN ('lock_user','unlock_user','change_role','cancel_booking','refund_processed','payment_viewed','booking_created','account_status_changed'))
);
GO

CREATE TABLE EmailLog (
    EmailLogID INT IDENTITY(1,1) NOT NULL,
    UserID INT NULL,
    ToEmail VARCHAR(150) NOT NULL,
    EventType VARCHAR(50) NOT NULL,
    DeliveryStatus VARCHAR(20) NOT NULL CONSTRAINT DF_EmailLog_DeliveryStatus DEFAULT 'sent',
    RetryCount INT NOT NULL CONSTRAINT DF_EmailLog_RetryCount DEFAULT 0,
    SentAt DATETIME NULL,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_EmailLog_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_EmailLog PRIMARY KEY (EmailLogID),
    CONSTRAINT FK_EmailLog_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT CK_EmailLog_EventType CHECK (EventType IN ('register','booking_confirmed','booking_cancelled','forgot_password','refund_processed','points_earned','reward_redeemed')),
    CONSTRAINT CK_EmailLog_DeliveryStatus CHECK (DeliveryStatus IN ('sent', 'failed', 'retrying')),
    CONSTRAINT CK_EmailLog_RetryCount CHECK (RetryCount >= 0)
);
GO

CREATE TABLE Notification (
    NotificationID INT IDENTITY(1,1) NOT NULL,
    UserID INT NOT NULL,
    Title NVARCHAR(150) NOT NULL,
    Message NVARCHAR(500) NOT NULL,
    Type VARCHAR(50) NOT NULL,
    IsRead BIT NOT NULL CONSTRAINT DF_Notification_IsRead DEFAULT 0,
    CreatedAt DATETIME NOT NULL CONSTRAINT DF_Notification_CreatedAt DEFAULT GETDATE(),
    CONSTRAINT PK_Notification PRIMARY KEY (NotificationID),
    CONSTRAINT FK_Notification_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT CK_Notification_Type CHECK (Type IN ('booking', 'payment', 'refund', 'promotion', 'system'))
);
GO

CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Role_Status ON Users(Role, Status);
CREATE INDEX IX_Room_CinemaID ON Room(CinemaID);
CREATE INDEX IX_Seat_RoomID ON Seat(RoomID);
CREATE INDEX IX_Showtime_MovieID ON Showtime(MovieID);
CREATE INDEX IX_Showtime_RoomID_StartTime ON Showtime(RoomID, StartTime);
CREATE INDEX IX_Booking_UserID ON Booking(UserID);
CREATE INDEX IX_Booking_ShowtimeID ON Booking(ShowtimeID);
CREATE INDEX IX_Booking_Status ON Booking(Status);
CREATE INDEX IX_Payment_Status ON Payment(Status);
CREATE INDEX IX_PaymentSession_PaymentID ON PaymentSession(PaymentID);
CREATE INDEX IX_Refund_BookingID ON Refund(BookingID);
CREATE INDEX IX_WalletTransaction_WalletID ON WalletTransaction(WalletID);
CREATE INDEX IX_LoyaltyPoints_UserID ON LoyaltyPoints(UserID);
CREATE INDEX IX_AdminActionLog_AdminID ON AdminActionLog(AdminID);
CREATE INDEX IX_EmailLog_UserID ON EmailLog(UserID);
CREATE INDEX IX_Notification_UserID_IsRead ON Notification(UserID, IsRead);
GO

INSERT INTO SeatType (TypeName, ExtraPrice)
VALUES ('standard', 0), ('vip', 20000), ('couple', 50000);
GO

INSERT INTO LoyaltyTiers (TierName, MinPoints, DiscountRate)
VALUES ('silver', 0, 0.00), ('gold', 1000, 0.05), ('platinum', 5000, 0.10), ('megavip', 10000, 0.15);
GO
