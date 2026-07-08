-- Update CHECK constraint to add 'check_in', 'redeem_voucher', and 'refund'
-- Run this script in SQL Server Management Studio or via sqlcmd

USE [cgv-premium-db];
GO

-- Drop existing constraint
ALTER TABLE AdminActionLog DROP CONSTRAINT CK_AdminActionLog_ActionType;
GO

-- Add new constraint with all action types including 'check_in', 'redeem_voucher', 'refund'
ALTER TABLE AdminActionLog ADD CONSTRAINT CK_AdminActionLog_ActionType
CHECK ([ActionType] IN (
    'create_showtime_type','update_showtime_type','delete_showtime_type','generate_showtime_by_type',
    'create_user','update_user','lock_user','unlock_user','change_role','delete_user',
    'create_voucher','update_voucher','delete_voucher',
    'create_showtime','update_showtime','delete_showtime',
    'update_ticket_price',
    'generate_seat','update_seat','delete_seat',
    'create_cinema','update_cinema','delete_cinema',
    'create_genre','update_genre','delete_genre',
    'create_movie','update_movie','delete_movie',
    'create_room_type','update_room_type','delete_room_type',
    'export_report','refund','check_in','redeem_voucher'
));
GO

PRINT 'CHECK constraint updated successfully';
GO
