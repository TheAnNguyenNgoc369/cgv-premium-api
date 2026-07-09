-- Mark the AddRefundFields migration as applied without running it
-- This is needed because the database already has these changes from add_refund_fields.sql
-- Run this script on your SQL Server database

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
VALUES ('20260706065145_AddRefundFields', '9.0.6');

-- After running this, verify with:
-- SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId;
