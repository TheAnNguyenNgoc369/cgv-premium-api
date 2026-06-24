-- Seed data for LoyaltyTier table
-- Run this script to initialize the membership tiers

-- Check if tiers already exist to avoid duplicates
IF NOT EXISTS (SELECT 1 FROM LoyaltyTiers WHERE TierName = 'Member')
BEGIN
    INSERT INTO LoyaltyTiers (TierName, MinPoints, DiscountRate)
    VALUES ('Member', 0, 0.00);
END

IF NOT EXISTS (SELECT 1 FROM LoyaltyTiers WHERE TierName = 'VIP')
BEGIN
    INSERT INTO LoyaltyTiers (TierName, MinPoints, DiscountRate)
    VALUES ('VIP', 200, 0.10);
END

-- Verify the data
SELECT * FROM LoyaltyTiers ORDER BY MinPoints;

-- Optional: Set all existing users to Member tier by default
-- Uncomment if needed
/*
UPDATE Users
SET LoyaltyTierID = (SELECT TierID FROM LoyaltyTiers WHERE TierName = 'Member')
WHERE LoyaltyTierID IS NULL;
*/
