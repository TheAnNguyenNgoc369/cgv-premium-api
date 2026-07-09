# Hướng dẫn Test Voucher Redemption Feature

## 1. Tạo Voucher Test Data

### SQL Script - Tạo voucher có thể đổi điểm

```sql
-- Voucher 1: Giảm 50k, cần 500 điểm, còn 100 voucher, mỗi user tối đa đổi 2 lần
INSERT INTO Voucher (
    VoucherCode, Category, DiscountType, DiscountValue, 
    MinOrderValue, MaxUses, UsedCount, 
    ValidFrom, ValidUntil, 
    IsActive, IsRedeemable, 
    RequiredPoints, RemainingQuantity, ExchangeLimit,
    Description, CreatedAt
)
VALUES (
    'REDEEM50K', 'Discount', 'fixed', 50000,
    100000, NULL, 0,
    DATEADD(day, -1, GETUTCDATE()), DATEADD(day, 30, GETUTCDATE()),
    1, 1,
    500, 100, 2,
    N'Giảm 50k cho đơn từ 100k - Đổi bằng 500 điểm',
    GETUTCDATE()
);

-- Voucher 2: Giảm 20%, cần 300 điểm, không giới hạn số lượng, mỗi user đổi 1 lần
INSERT INTO Voucher (
    VoucherCode, Category, DiscountType, DiscountValue, 
    MinOrderValue, MaxUses, UsedCount, 
    ValidFrom, ValidUntil, 
    IsActive, IsRedeemable, 
    RequiredPoints, RemainingQuantity, ExchangeLimit,
    Description, CreatedAt
)
VALUES (
    'REDEEM20PCT', 'Discount', 'percent', 20,
    50000, NULL, 0,
    DATEADD(day, -1, GETUTCDATE()), DATEADD(day, 30, GETUTCDATE()),
    1, 1,
    300, NULL, 1,
    N'Giảm 20% cho đơn từ 50k - Đổi bằng 300 điểm',
    GETUTCDATE()
);

-- Voucher 3: Combo, cần 1000 điểm, chỉ còn 5 voucher
INSERT INTO Voucher (
    VoucherCode, Category, DiscountType, DiscountValue, 
    MinOrderValue, MaxUses, UsedCount, 
    ValidFrom, ValidUntil, 
    IsActive, IsRedeemable, 
    RequiredPoints, RemainingQuantity, ExchangeLimit,
    Description, CreatedAt
)
VALUES (
    'COMBOSPECIAL', 'Combo', 'fixed', 100000,
    200000, NULL, 0,
    DATEADD(day, -1, GETUTCDATE()), DATEADD(day, 30, GETUTCDATE()),
    1, 1,
    1000, 5, NULL,
    N'Giảm 100k cho Combo - Đổi bằng 1000 điểm',
    GETUTCDATE()
);

-- Voucher 4: Không thể đổi (IsRedeemable = false) - để test validation
INSERT INTO Voucher (
    VoucherCode, Category, DiscountType, DiscountValue, 
    MinOrderValue, MaxUses, UsedCount, 
    ValidFrom, ValidUntil, 
    IsActive, IsRedeemable, 
    RequiredPoints, RemainingQuantity, ExchangeLimit,
    Description, CreatedAt
)
VALUES (
    'NOTREDEEMABLE', 'Discount', 'fixed', 30000,
    50000, NULL, 0,
    DATEADD(day, -1, GETUTCDATE()), DATEADD(day, 30, GETUTCDATE()),
    1, 0,
    200, 50, NULL,
    N'Voucher không thể đổi điểm',
    GETUTCDATE()
);

-- Voucher 5: Đã hết hạn - để test validation
INSERT INTO Voucher (
    VoucherCode, Category, DiscountType, DiscountValue, 
    MinOrderValue, MaxUses, UsedCount, 
    ValidFrom, ValidUntil, 
    IsActive, IsRedeemable, 
    RequiredPoints, RemainingQuantity, ExchangeLimit,
    Description, CreatedAt
)
VALUES (
    'EXPIRED', 'Discount', 'fixed', 40000,
    50000, NULL, 0,
    DATEADD(day, -10, GETUTCDATE()), DATEADD(day, -1, GETUTCDATE()),
    1, 1,
    400, 50, NULL,
    N'Voucher đã hết hạn',
    GETUTCDATE()
);
```

### Cập nhật điểm cho user test

```sql
-- Cập nhật user có đủ điểm để test (thay {USER_ID} bằng ID user test của bạn)
UPDATE Users 
SET TotalPoints = 2000, UpdatedAt = GETUTCDATE()
WHERE UserID = {USER_ID};
```

## 2. Test Scenarios

### A. API Endpoint Tests

#### Test 1: GET /api/vouchers/redeemable
**Mục đích:** Lấy danh sách voucher có thể đổi

**Request:**
```http
GET /api/vouchers/redeemable
Authorization: Bearer {customer_token}
```

**Expected Response:**
```json
{
  "success": true,
  "vouchers": [
    {
      "voucherId": 1,
      "voucherCode": "REDEEM50K",
      "category": "Discount",
      "discountType": "fixed",
      "discountValue": 50000,
      "requiredPoints": 500,
      "remainingQuantity": 100,
      "exchangeLimit": 2,
      "validFrom": "...",
      "validUntil": "...",
      "imageUrl": null,
      "description": "Giảm 50k cho đơn từ 100k - Đổi bằng 500 điểm"
    }
  ]
}
```

**Validation Points:**
- ✓ Chỉ trả về voucher có `IsRedeemable = true`
- ✓ Chỉ trả về voucher `IsActive = true`
- ✓ Chỉ trả về voucher còn hạn (ValidFrom <= now <= ValidUntil)
- ✓ Chỉ trả về voucher còn số lượng (RemainingQuantity > 0 hoặc NULL)
- ✓ Không trả về voucher EXPIRED, NOTREDEEMABLE

---

#### Test 2: POST /api/vouchers/redeem - SUCCESS
**Mục đích:** Đổi voucher thành công

**Request:**
```http
POST /api/vouchers/redeem
Authorization: Bearer {customer_token}
Content-Type: application/json

{
  "voucherId": 1
}
```

**Expected Response:**
```json
{
  "success": true,
  "remainingPoints": 1500,
  "voucherCode": "REDEEM50K"
}
```

**Validation sau khi đổi:**
```sql
-- 1. Check user points đã bị trừ
SELECT UserID, TotalPoints FROM Users WHERE UserID = {USER_ID};
-- Expected: TotalPoints = 1500 (2000 - 500)

-- 2. Check UserVoucher đã được tạo
SELECT * FROM UserVoucher 
WHERE UserID = {USER_ID} AND VoucherID = 1;
-- Expected: 1 record, Status = 'available'

-- 3. Check LoyaltyPoints history
SELECT * FROM LoyaltyPoints 
WHERE UserID = {USER_ID} AND VoucherID = 1;
-- Expected: 1 record, TransactionType = 'exchange', PointsDelta = -500

-- 4. Check Voucher RemainingQuantity giảm
SELECT RemainingQuantity FROM Voucher WHERE VoucherID = 1;
-- Expected: RemainingQuantity = 99 (100 - 1)

-- 5. Check AdminActionLog
SELECT * FROM AdminActionLog 
WHERE ActionType = 'redeem_voucher' 
AND AdminID = {USER_ID}
ORDER BY CreatedAt DESC;
-- Expected: 1 record mới
```

---

#### Test 3: POST /api/vouchers/redeem - INSUFFICIENT POINTS
**Mục đích:** User không đủ điểm

**Setup:**
```sql
-- Set user chỉ có 200 điểm
UPDATE Users SET TotalPoints = 200 WHERE UserID = {USER_ID};
```

**Request:**
```http
POST /api/vouchers/redeem
Authorization: Bearer {customer_token}
Content-Type: application/json

{
  "voucherId": 1
}
```

**Expected Response:**
```json
{
  "success": false,
  "message": "Insufficient points. Required: 500, Available: 200"
}
```

---

#### Test 4: POST /api/vouchers/redeem - EXCHANGE LIMIT REACHED
**Mục đích:** User đã đổi đủ số lần cho phép

**Setup:**
```sql
-- User đã đổi 2 lần (ExchangeLimit = 2)
INSERT INTO UserVoucher (UserID, VoucherID, RedeemedAt, ExpiredAt, Status)
VALUES 
({USER_ID}, 1, GETUTCDATE(), DATEADD(day, 30, GETUTCDATE()), 'available'),
({USER_ID}, 1, GETUTCDATE(), DATEADD(day, 30, GETUTCDATE()), 'available');
```

**Request:**
```http
POST /api/vouchers/redeem
Authorization: Bearer {customer_token}
Content-Type: application/json

{
  "voucherId": 1
}
```

**Expected Response:**
```json
{
  "success": false,
  "message": "Exchange limit reached. Maximum 2 redemptions per user"
}
```

---

#### Test 5: POST /api/vouchers/redeem - OUT OF STOCK
**Mục đích:** Voucher hết số lượng

**Setup:**
```sql
-- Set voucher hết số lượng
UPDATE Voucher SET RemainingQuantity = 0 WHERE VoucherID = 1;
```

**Request:**
```http
POST /api/vouchers/redeem
Authorization: Bearer {customer_token}
Content-Type: application/json

{
  "voucherId": 1
}
```

**Expected Response:**
```json
{
  "success": false,
  "message": "Voucher is out of stock"
}
```

---

#### Test 6: POST /api/vouchers/redeem - EXPIRED VOUCHER
**Mục đích:** Voucher hết hạn

**Request:**
```http
POST /api/vouchers/redeem
Authorization: Bearer {customer_token}
Content-Type: application/json

{
  "voucherId": 5
}
```

**Expected Response:**
```json
{
  "success": false,
  "message": "Voucher has expired"
}
```

---

#### Test 7: POST /api/vouchers/redeem - NOT REDEEMABLE
**Mục đích:** Voucher không cho phép đổi

**Request:**
```http
POST /api/vouchers/redeem
Authorization: Bearer {customer_token}
Content-Type: application/json

{
  "voucherId": 4
}
```

**Expected Response:**
```json
{
  "success": false,
  "message": "Voucher is not redeemable"
}
```

---

#### Test 8: POST /api/vouchers/redeem - NON-CUSTOMER USER
**Mục đích:** Staff/Admin không được đổi

**Request:**
```http
POST /api/vouchers/redeem
Authorization: Bearer {staff_or_admin_token}
Content-Type: application/json

{
  "voucherId": 1
}
```

**Expected Response:**
```json
{
  "success": false,
  "message": "Only customers can redeem vouchers"
}
```

---

#### Test 9: GET /api/vouchers/my-vouchers
**Mục đích:** Xem danh sách voucher đã đổi

**Request:**
```http
GET /api/vouchers/my-vouchers
Authorization: Bearer {customer_token}
```

**Expected Response:**
```json
{
  "success": true,
  "vouchers": [
    {
      "userVoucherId": 1,
      "voucherId": 1,
      "voucherCode": "REDEEM50K",
      "discountType": "fixed",
      "discountValue": 50000,
      "status": "available",
      "redeemedAt": "...",
      "expiredAt": "...",
      "usedAt": null,
      "imageUrl": null
    }
  ]
}
```

---

### B. Transaction & Concurrency Tests

#### Test 10: Race Condition - Nhiều user đổi cùng lúc
**Mục đích:** Verify transaction safety khi nhiều request đồng thời

**Setup:**
```sql
-- Voucher chỉ còn 1 cái
UPDATE Voucher SET RemainingQuantity = 1 WHERE VoucherID = 1;

-- 2 users đều có đủ điểm
UPDATE Users SET TotalPoints = 1000 WHERE UserID IN ({USER1_ID}, {USER2_ID});
```

**Thực hiện:**
1. Gửi 2 requests đổi voucher từ 2 users khác nhau cùng lúc
2. Chỉ 1 request được thành công
3. Request còn lại nhận error "out of stock"

**Validation:**
```sql
-- Chỉ có 1 UserVoucher được tạo
SELECT COUNT(*) FROM UserVoucher WHERE VoucherID = 1;
-- Expected: COUNT = 1

-- RemainingQuantity = 0
SELECT RemainingQuantity FROM Voucher WHERE VoucherID = 1;
-- Expected: 0
```

---

#### Test 11: Transaction Rollback on Error
**Mục đích:** Verify rollback khi có lỗi

**Cách test:**
- Tạm thời comment out một phần code trong transaction
- Trigger error
- Verify không có thay đổi nào trong DB (points không trừ, UserVoucher không tạo)

---

### C. Audit & History Tests

#### Test 12: Loyalty Points History
**Validation:**
```sql
SELECT 
    lp.PointID,
    lp.UserID,
    lp.VoucherID,
    v.VoucherCode,
    lp.PointsDelta,
    lp.TransactionType,
    lp.Description,
    lp.CreatedAt
FROM LoyaltyPoints lp
INNER JOIN Voucher v ON lp.VoucherID = v.VoucherID
WHERE lp.UserID = {USER_ID}
AND lp.TransactionType = 'exchange'
ORDER BY lp.CreatedAt DESC;
```

**Expected:**
- TransactionType = 'exchange'
- PointsDelta = âm (trừ điểm)
- VoucherID có giá trị
- Description chứa voucher code

---

#### Test 13: Admin Action Log
**Validation:**
```sql
SELECT 
    AdminID,
    TargetTable,
    ActionType,
    Description,
    IPAddress,
    CreatedAt
FROM AdminActionLog
WHERE ActionType = 'redeem_voucher'
ORDER BY CreatedAt DESC;
```

**Expected:**
- ActionType = 'redeem_voucher'
- AdminID = UserID của customer
- TargetTable = 'UserVoucher'
- Description chứa thông tin voucher

---

## 3. Postman Collection

Tạo Postman collection với các test cases trên:

1. Setup environment variables:
   - `base_url`: http://localhost:5000
   - `customer_token`: JWT token của customer
   - `staff_token`: JWT token của staff

2. Import các requests vào collection
3. Chạy collection để test tự động

---

## 4. Performance Tests

### Load Test: Nhiều users đổi voucher đồng thời

```bash
# Sử dụng Apache Bench hoặc k6
k6 run --vus 50 --duration 30s voucher-redeem-load-test.js
```

**Mục tiêu:**
- Response time < 500ms
- Success rate > 99%
- Không có duplicate UserVoucher
- RemainingQuantity accurate

---

## 5. Checklist Before Production

- [ ] Tất cả unit tests pass
- [ ] Integration tests pass
- [ ] API endpoints documented trong Swagger
- [ ] Migration đã chạy trên staging
- [ ] Load test đạt yêu cầu
- [ ] Rollback plan đã chuẩn bị
- [ ] Monitoring/alerting đã setup
- [ ] Team đã review code
