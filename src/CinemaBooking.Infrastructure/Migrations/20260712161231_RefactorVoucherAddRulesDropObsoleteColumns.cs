using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaBooking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorVoucherAddRulesDropObsoleteColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ----------------------------------------------------------------
            // Idempotent drops: skip if the constraint/column doesn't exist.
            // This handles DBs where prior migrations left an inconsistent state
            // (drift between model snapshot and physical schema).
            // ----------------------------------------------------------------

            // Drop CK_Voucher_Category if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = 'CK_Voucher_Category'
                             AND parent_object_id = OBJECT_ID('dbo.Voucher'))
                BEGIN
                    ALTER TABLE [Voucher] DROP CONSTRAINT [CK_Voucher_Category];
                END
            ");

            // Drop CK_Voucher_RemainingQuantity if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = 'CK_Voucher_RemainingQuantity'
                             AND parent_object_id = OBJECT_ID('dbo.Voucher'))
                BEGIN
                    ALTER TABLE [Voucher] DROP CONSTRAINT [CK_Voucher_RemainingQuantity];
                END
            ");

            // Drop CK_PaymentSession_GatewayName if it exists (will re-add updated version)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = 'CK_PaymentSession_GatewayName'
                             AND parent_object_id = OBJECT_ID('dbo.PaymentSession'))
                BEGIN
                    ALTER TABLE [PaymentSession] DROP CONSTRAINT [CK_PaymentSession_GatewayName];
                END
            ");

            // Drop CK_Payment_Method if it exists (will re-add updated version)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = 'CK_Payment_Method'
                             AND parent_object_id = OBJECT_ID('dbo.Payment'))
                BEGIN
                    ALTER TABLE [Payment] DROP CONSTRAINT [CK_Payment_Method];
                END
            ");

            // Drop Category column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns
                           WHERE name = 'Category'
                             AND object_id = OBJECT_ID('dbo.Voucher'))
                BEGIN
                    -- Drop default constraint if present
                    DECLARE @default_name NVARCHAR(200);
                    SELECT @default_name = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON dc.parent_column_id = c.column_id
                                            AND dc.parent_object_id = c.object_id
                    WHERE c.name = 'Category'
                      AND c.object_id = OBJECT_ID('dbo.Voucher');
                    IF @default_name IS NOT NULL
                        EXEC('ALTER TABLE [Voucher] DROP CONSTRAINT [' + @default_name + ']');

                    ALTER TABLE [Voucher] DROP COLUMN [Category];
                END
            ");

            // Drop RemainingQuantity column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns
                           WHERE name = 'RemainingQuantity'
                             AND object_id = OBJECT_ID('dbo.Voucher'))
                BEGIN
                    -- Drop default constraint if present
                    DECLARE @default_name NVARCHAR(200);
                    SELECT @default_name = dc.name
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON dc.parent_column_id = c.column_id
                                            AND dc.parent_object_id = c.object_id
                    WHERE c.name = 'RemainingQuantity'
                      AND c.object_id = OBJECT_ID('dbo.Voucher');
                    IF @default_name IS NOT NULL
                        EXEC('ALTER TABLE [Voucher] DROP CONSTRAINT [' + @default_name + ']');

                    ALTER TABLE [Voucher] DROP COLUMN [RemainingQuantity];
                END
            ");

            // ----------------------------------------------------------------
            // Create VoucherRules table (idempotent)
            // ----------------------------------------------------------------
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables
                               WHERE name = 'VoucherRules'
                                 AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    CREATE TABLE [VoucherRules] (
                        [RuleID]    INT             IDENTITY(1,1) NOT NULL,
                        [VoucherID] INT             NOT NULL,
                        [RuleType]  NVARCHAR(50)    NOT NULL,
                        [RuleValue] NVARCHAR(100)   NOT NULL,
                        [CreatedAt] DATETIME2       NOT NULL,
                        CONSTRAINT [PK_VoucherRules] PRIMARY KEY CLUSTERED ([RuleID] ASC),
                        CONSTRAINT [FK_VoucherRules_Voucher_VoucherID]
                            FOREIGN KEY ([VoucherID]) REFERENCES [Voucher]([VoucherID])
                    );
                END
            ");

            // Indexes (idempotent)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_VoucherRules_VoucherID'
                                 AND object_id = OBJECT_ID('dbo.VoucherRules'))
                BEGIN
                    CREATE INDEX [IX_VoucherRules_VoucherID]
                        ON [VoucherRules] ([VoucherID]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes
                               WHERE name = 'IX_VoucherRules_VoucherID_RuleType'
                                 AND object_id = OBJECT_ID('dbo.VoucherRules'))
                BEGIN
                    CREATE INDEX [IX_VoucherRules_VoucherID_RuleType]
                        ON [VoucherRules] ([VoucherID], [RuleType]);
                END
            ");

            // ----------------------------------------------------------------
            // Re-add updated check constraints (idempotent — dropped above)
            // ----------------------------------------------------------------
            // Preserve 'vnpay' in allowed values to remain compatible with historical rows.
            // If the business truly wants to drop vnpay support, do a separate data-migration
            // pass first (delete/update vnpay rows), then a follow-up schema migration.
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                               WHERE name = 'CK_PaymentSession_GatewayName'
                                 AND parent_object_id = OBJECT_ID('dbo.PaymentSession'))
                BEGIN
                    ALTER TABLE [PaymentSession]
                        ADD CONSTRAINT [CK_PaymentSession_GatewayName]
                        CHECK ([GatewayName] IN ('vnpay', 'payos', 'momo'));
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                               WHERE name = 'CK_Payment_Method'
                                 AND parent_object_id = OBJECT_ID('dbo.Payment'))
                BEGIN
                    ALTER TABLE [Payment]
                        ADD CONSTRAINT [CK_Payment_Method]
                        CHECK ([PaymentMethod] IN ('vnpay', 'payos', 'momo', 'credit_card', 'banking', 'cash', 'wallet'));
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore Voucher.Category + RemainingQuantity, restore old check constraints,
            // drop VoucherRules. Idempotent for defensive rollback.

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables
                           WHERE name = 'VoucherRules'
                             AND schema_id = SCHEMA_ID('dbo'))
                BEGIN
                    DROP TABLE [VoucherRules];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = 'CK_PaymentSession_GatewayName'
                             AND parent_object_id = OBJECT_ID('dbo.PaymentSession'))
                BEGIN
                    ALTER TABLE [PaymentSession] DROP CONSTRAINT [CK_PaymentSession_GatewayName];
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.check_constraints
                           WHERE name = 'CK_Payment_Method'
                             AND parent_object_id = OBJECT_ID('dbo.Payment'))
                BEGIN
                    ALTER TABLE [Payment] DROP CONSTRAINT [CK_Payment_Method];
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns
                               WHERE name = 'Category'
                                 AND object_id = OBJECT_ID('dbo.Voucher'))
                BEGIN
                    ALTER TABLE [Voucher] ADD [Category] NVARCHAR(50) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns
                               WHERE name = 'RemainingQuantity'
                                 AND object_id = OBJECT_ID('dbo.Voucher'))
                BEGIN
                    ALTER TABLE [Voucher] ADD [RemainingQuantity] INT NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                               WHERE name = 'CK_Voucher_Category'
                                 AND parent_object_id = OBJECT_ID('dbo.Voucher'))
                BEGIN
                    ALTER TABLE [Voucher]
                        ADD CONSTRAINT [CK_Voucher_Category]
                        CHECK ([Category] IS NULL OR [Category] IN ('Discount', 'Combo', 'Cashback'));
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                               WHERE name = 'CK_Voucher_RemainingQuantity'
                                 AND parent_object_id = OBJECT_ID('dbo.Voucher'))
                BEGIN
                    ALTER TABLE [Voucher]
                        ADD CONSTRAINT [CK_Voucher_RemainingQuantity]
                        CHECK ([RemainingQuantity] IS NULL OR [RemainingQuantity] >= 0);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                               WHERE name = 'CK_PaymentSession_GatewayName'
                                 AND parent_object_id = OBJECT_ID('dbo.PaymentSession'))
                BEGIN
                    ALTER TABLE [PaymentSession]
                        ADD CONSTRAINT [CK_PaymentSession_GatewayName]
                        CHECK ([GatewayName] IN ('vnpay', 'payos', 'momo'));
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
                               WHERE name = 'CK_Payment_Method'
                                 AND parent_object_id = OBJECT_ID('dbo.Payment'))
                BEGIN
                    ALTER TABLE [Payment]
                        ADD CONSTRAINT [CK_Payment_Method]
                        CHECK ([PaymentMethod] IN ('vnpay', 'payos', 'momo', 'credit_card', 'banking', 'cash', 'wallet'));
                END
            ");
        }
    }
}
