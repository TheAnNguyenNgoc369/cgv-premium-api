-- Seed data for LoyaltyTier table
-- Run this script to initialize the 4-tier membership system

-- Check if tiers already exist to avoid duplicates
IF NOT EXISTS (SELECT 1 FROM LoyaltyTiers WHERE TierName = 'silver')
BEGIN
    INSERT INTO LoyaltyTiers (TierName, MinPoints, DiscountRate)
    VALUES ('silver', 0, 0.00);
END

IF NOT EXISTS (SELECT 1 FROM LoyaltyTiers WHERE TierName = 'gold')
BEGIN
    INSERT INTO LoyaltyTiers (TierName, MinPoints, DiscountRate)
    VALUES ('gold', 1000, 0.05);
END

IF NOT EXISTS (SELECT 1 FROM LoyaltyTiers WHERE TierName = 'platinum')
BEGIN
    INSERT INTO LoyaltyTiers (TierName, MinPoints, DiscountRate)
    VALUES ('platinum', 5000, 0.10);
END

IF NOT EXISTS (SELECT 1 FROM LoyaltyTiers WHERE TierName = 'megavip')
BEGIN
    INSERT INTO LoyaltyTiers (TierName, MinPoints, DiscountRate)
    VALUES ('megavip', 10000, 0.15);
END

-- Verify the data
SELECT * FROM LoyaltyTiers ORDER BY MinPoints;

-- Optional: Set all existing users to Silver tier by default
-- Uncomment if needed
/*
UPDATE Users
SET LoyaltyTierID = (SELECT TierID FROM LoyaltyTiers WHERE TierName = 'silver')
WHERE LoyaltyTierID IS NULL;
*/
