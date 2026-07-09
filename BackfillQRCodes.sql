-- Backfill QR Codes cho bookings đã thanh toán nhưng chưa có QR Code
-- Run script này sau khi deploy code mới

UPDATE Booking
SET QRCode = LOWER(NEWID())
WHERE Status IN ('paid', 'used')
  AND QRCode IS NULL;

-- Verify kết quả
SELECT
    Status,
    COUNT(*) as Total,
    SUM(CASE WHEN QRCode IS NULL THEN 1 ELSE 0 END) as MissingQR,
    SUM(CASE WHEN QRCode IS NOT NULL THEN 1 ELSE 0 END) as HasQR
FROM Booking
WHERE Status IN ('paid', 'used')
GROUP BY Status;
