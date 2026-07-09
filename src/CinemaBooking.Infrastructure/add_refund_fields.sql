-- Add MaxRefundPerMonth column to LoyaltyTiers table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('LoyaltyTiers') AND name = 'MaxRefundPerMonth')
BEGIN
    ALTER TABLE LoyaltyTiers ADD MaxRefundPerMonth INT NOT NULL DEFAULT 0;
    PRINT 'Added MaxRefundPerMonth column to LoyaltyTiers';
END
ELSE
BEGIN
    PRINT 'MaxRefundPerMonth column already exists in LoyaltyTiers';
END
GO

-- Add refund-related columns to Payment table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payment') AND name = 'RefundReason')
BEGIN
    ALTER TABLE Payment ADD RefundReason NVARCHAR(MAX) NULL;
    PRINT 'Added RefundReason column to Payment';
END
ELSE
BEGIN
    PRINT 'RefundReason column already exists in Payment';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payment') AND name = 'RefundAmount')
BEGIN
    ALTER TABLE Payment ADD RefundAmount DECIMAL(18,2) NULL;
    PRINT 'Added RefundAmount column to Payment';
END
ELSE
BEGIN
    PRINT 'RefundAmount column already exists in Payment';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payment') AND name = 'RefundedAt')
BEGIN
    ALTER TABLE Payment ADD RefundedAt DATETIME2 NULL;
    PRINT 'Added RefundedAt column to Payment';
END
ELSE
BEGIN
    PRINT 'RefundedAt column already exists in Payment';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Payment') AND name = 'RefundedBy')
BEGIN
    ALTER TABLE Payment ADD RefundedBy INT NULL;
    PRINT 'Added RefundedBy column to Payment';
END
ELSE
BEGIN
    PRINT 'RefundedBy column already exists in Payment';
END
GO

-- Add FK constraint for Payment.RefundedBy
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Payment_RefundedBy')
BEGIN
    ALTER TABLE Payment ADD CONSTRAINT FK_Payment_RefundedBy
        FOREIGN KEY (RefundedBy) REFERENCES Users(UserID);
    PRINT 'Added FK_Payment_RefundedBy constraint';
END
ELSE
BEGIN
    PRINT 'FK_Payment_RefundedBy constraint already exists';
END
GO

-- Add WalletID column to Refund table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Refund') AND name = 'WalletID')
BEGIN
    ALTER TABLE Refund ADD WalletID INT NULL;
    PRINT 'Added WalletID column to Refund';
END
ELSE
BEGIN
    PRINT 'WalletID column already exists in Refund';
END
GO

-- Add FK constraint for Refund.WalletID
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Refund_Wallet')
BEGIN
    ALTER TABLE Refund ADD CONSTRAINT FK_Refund_Wallet
        FOREIGN KEY (WalletID) REFERENCES Wallet(WalletID);
    PRINT 'Added FK_Refund_Wallet constraint';
END
ELSE
BEGIN
    PRINT 'FK_Refund_Wallet constraint already exists';
END
GO

-- Update MaxRefundPerMonth values for existing tiers
UPDATE LoyaltyTiers SET MaxRefundPerMonth = 1 WHERE TierName = 'silver';
UPDATE LoyaltyTiers SET MaxRefundPerMonth = 3 WHERE TierName = 'gold';
UPDATE LoyaltyTiers SET MaxRefundPerMonth = 5 WHERE TierName = 'platinum';
UPDATE LoyaltyTiers SET MaxRefundPerMonth = 7 WHERE TierName = 'megavip';
PRINT 'Updated MaxRefundPerMonth values for all loyalty tiers';
GO

PRINT 'Database schema update completed successfully!';
