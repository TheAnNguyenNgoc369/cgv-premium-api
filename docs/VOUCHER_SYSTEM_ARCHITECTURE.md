# Voucher System Architecture - Complete Documentation

**Last Updated:** 2026-07-14  
**Status:** Production-Ready  
**Version:** 4.0

---

## Executive Summary

The voucher system is a flexible, rule-based discount engine supporting two voucher types: **Public Vouchers** (direct discounts) and **Loyalty Vouchers** (redeemed with loyalty points). The system uses a pluggable rule engine with 10 configurable rule types to control where and how discounts apply.

**Key Features:**
- ✅ Two voucher types with distinct lifecycles
- ✅ 10 built-in rule types (Cinema, Movie, Membership, etc.)
- ✅ Extensible rule engine (add new rules without changing core code)
- ✅ Fixed concurrency bug: UsedCount cannot exceed MaxUses
- ✅ No N+1 queries: flat validation context
- ✅ Clean architecture: Strategy pattern, Repository pattern

---

## Table of Contents

1. [Voucher Types](#voucher-types)
   - [Public Voucher](#public-voucher)
   - [Loyalty Voucher](#loyalty-voucher)
2. [All Voucher Fields Explained](#all-voucher-fields-explained)
3. [Rule Types Reference](#rule-types-reference)
4. [Booking Flow](#booking-flow)
5. [Sequence Diagrams](#sequence-diagrams)
6. [Adding New Rule Types](#adding-new-rule-types)

---

## Voucher Types

### Public Voucher

#### Purpose
Direct discount offered to all users during a promotional period. No redemption required.

#### When Used
- Marketing campaigns (e.g., "Summer 20% off")
- Flash sales (e.g., weekend-only discounts)
- Category promotions (e.g., "25% off VIP seats")
- Product bundles (e.g., "20% off when buying popcorn")

#### Fields
- `VoucherCode` — Unique identifier (e.g., "SUMMER20")
- `IsRedeemable` — **false** for public vouchers
- `RequiredPoints` — **null** for public vouchers
- `ExchangeLimit` — **null** for public vouchers
- `DiscountType` — "percent" or "fixed"
- `DiscountValue` — 1-100 for percent, >0 for fixed
- `MinOrderValue` — Minimum booking total to apply
- `MaxUses` — Global cap on total uses (can be null for unlimited)
- `UsedCount` — Current usage count (incremented on payment)
- `ValidFrom` / `ValidUntil` — Time window (+07:00 timezone)
- `IsActive` — Admin can deactivate anytime
- `Rules` — Array of validation rules (optional)

#### Lifecycle

```
Created (Admin)
    ↓
IsActive=true, ValidFrom passed, UsedCount=0
    ↓
User attempts booking with voucher code
    ├─ Code valid? Rules satisfied? MinOrderValue met?
    │  ├─ YES → discount applied, UsedCount++ (on payment)
    │  └─ NO → validation error
    ├─ UsedCount >= MaxUses?
    │  └─ YES → "quota exhausted" error
    │
During time period (ValidFrom to ValidUntil)
    ↓
Expired or IsActive=false → no new bookings accepted
    ↓
Deactivated (Admin) → IsActive=false
```

#### MaxUses / UsedCount Behavior

| Scenario | MaxUses | UsedCount | Allowed? |
|----------|---------|-----------|----------|
| No quota | null | 0 | ✅ Unlimited |
| 100 quota | 100 | 99 | ✅ One more use |
| 100 quota | 100 | 100 | ❌ Exhausted |
| 100 quota | 100 | 101 | ❌ Never happens (locked) |

**Critical Guarantee:** UsedCount can NEVER exceed MaxUses due to pessimistic locking.

**When UsedCount Increments:**
- ✅ When booking payment completes
- ❌ NOT during voucher redemption (loyalty flow)
- ❌ NOT during validation

#### Validation

```
1. Check IsActive
2. Check ValidFrom <= now <= ValidUntil
3. Check UsedCount < MaxUses
4. Check BookingTotal >= MinOrderValue
5. Execute all VoucherRules
   └─ If any rule fails → reject
6. Calculate ApplicableAmount (from ApplyScope rule or default to total)
```

#### Example

**Create a weekend food discount:**
```json
{
  "voucherCode": "WEEKEND_FOOD_20",
  "discountType": "percent",
  "discountValue": 20,
  "minOrderValue": 100000,
  "maxUses": 500,
  "validFrom": "2026-07-13T00:00:00+07:00",
  "validUntil": "2026-07-14T23:59:59+07:00",
  "isActive": true,
  "isRedeemable": false,
  "description": "20% off food orders on weekends",
  "rules": [
    { "ruleType": "DayOfWeek", "ruleValue": "Saturday" },
    { "ruleType": "ApplyScope", "ruleValue": "Food" }
  ]
}
```

**User applies voucher:**
- Booking: 2 tickets (200k) + popcorn (100k) = 300k total
- Food total: 100k
- ApplyScope = Food → discount applies to 100k only
- Discount: 100k × 20% = 20k
- Final total: 280k

---

### Loyalty Voucher

#### Purpose
Reward for loyal customers. Users exchange loyalty points for a voucher, which they then use in bookings.

#### When Used
- Loyalty program rewards (e.g., 50k points → 15% discount)
- VIP member benefits (e.g., platinum tier gets exclusive vouchers)
- Referral rewards (e.g., refer friend → 10% off)

#### Fields (unique to loyalty)
- `IsRedeemable` — **true** for loyalty vouchers
- `RequiredPoints` — Points needed to redeem (must be >0)
- `ExchangeLimit` — Max times user can redeem (must be >0)
- `MaxUses` — Global cap on total redemptions (must be >0 for loyalty)

#### UserVoucher Lifecycle

```
Loyalty Voucher Created (IsRedeemable=true)
    ↓
User views redeemable vouchers list
    ↓
User clicks "Redeem" (costs loyalty points)
    ├─ User has enough points?
    ├─ User hasn't exceeded ExchangeLimit?
    ├─ Global UsedCount < MaxUses?
    │  ├─ YES → 
    │  │   ├─ Create UserVoucher (Status=Available)
    │  │   ├─ Deduct points from user
    │  │   ├─ Create LoyaltyPoints transaction (delta = -RequiredPoints)
    │  │   └─ Increment global UsedCount
    │  └─ NO → "Redemption quota exhausted"
    │
    ↓
User has UserVoucher (owned voucher instance)
    ├─ Status: Available
    ├─ RedeemedAt: timestamp
    ├─ ExpiredAt: ValidUntil of voucher
    │
    ↓
User applies voucher in booking
    ├─ Code valid? Rules satisfied?
    │  ├─ YES → discount applied, UserVoucher.UsedAt = now
    │  └─ NO → validation error
    │
    ↓
UserVoucher expires or is used
    ├─ If expired → Status changes to Expired
    ├─ If used → Status changes to Used
```

#### RequiredPoints and ExchangeLimit

| Field | Meaning | Example |
|-------|---------|---------|
| `RequiredPoints` | Points per redemption | 50,000 points → 1 voucher |
| `ExchangeLimit` | Max times per user | Each user can redeem max 3x |
| `MaxUses` | Global cap | Max 1000 vouchers total |

**Example:**
```
RequiredPoints = 50000
ExchangeLimit = 3
MaxUses = 1000

User A: redeems 1× (1/3 used, 50k points deducted)
User B: redeems 2× (2/3 used, 100k points deducted)
User C: tries to redeem → allowed if global < 1000
User A: tries 4th redeem → rejected (3/3 limit reached)
```

#### MaxUses / UsedCount Behavior

**For Loyalty Vouchers:**
- `MaxUses` is the **global redemption cap** (total across ALL users)
- `UsedCount` increments when any user redeems (not when they use the voucher in a booking)
- Once `UsedCount >= MaxUses`, no more users can redeem

| Global State | MaxUses | UsedCount | Can User Redeem? |
|--------------|---------|-----------|------------------|
| Fresh | 1000 | 0 | ✅ Yes (990 left) |
| Nearing limit | 1000 | 999 | ✅ Yes (0 left after) |
| Exhausted | 1000 | 1000 | ❌ No (global quota hit) |

**IMPORTANT:** Incrementing global `UsedCount` happens INSIDE the redemption transaction with pessimistic locking to prevent race conditions.

#### UserVoucher Status Values

| Status | Meaning |
|--------|---------|
| `Available` | Redeemed, not yet used in a booking |
| `Used` | Applied in a booking |
| `Expired` | Passed ValidUntil date without use |

#### Validation (Redemption Phase)

```
1. Voucher exists, IsRedeemable=true, IsActive=true
2. ValidFrom <= now <= ValidUntil
3. User has sufficient loyalty points
4. User hasn't exceeded ExchangeLimit
5. Global UsedCount < MaxUses (checked again inside transaction)
6. Create UserVoucher + LoyaltyPoints transaction
7. Increment global UsedCount atomically
```

#### Validation (Booking Phase)

Same as public vouchers:
```
1. Check IsActive
2. Check ValidFrom <= now <= ValidUntil
3. Check VoucherRules
4. Calculate discount
```

#### Example

**Create a VIP loyalty reward:**
```json
{
  "voucherCode": "VIP_REWARD_Q3",
  "discountType": "percent",
  "discountValue": 15,
  "minOrderValue": 0,
  "maxUses": 100,
  "validFrom": "2026-07-01T00:00:00+07:00",
  "validUntil": "2026-09-30T23:59:59+07:00",
  "isActive": true,
  "isRedeemable": true,
  "requiredPoints": 50000,
  "exchangeLimit": 3,
  "description": "VIP Reward: 15% off, max 3x per user",
  "rules": [
    { "ruleType": "Membership", "ruleValue": "VIP" },
    { "ruleType": "ApplyScope", "ruleValue": "Order" }
  ]
}
```

**User journey:**
1. User A sees "VIP_REWARD_Q3" in redeemable list
2. User A clicks redeem (costs 50k points)
3. System checks: user has 200k points ✅, hasn't redeemed yet ✅, global <100 ✅
4. UserVoucher created, 50k points deducted, global count incremented to 1
5. User A books a ticket with code "VIP_REWARD_Q3"
6. System validates: user is VIP ✅, rules pass ✅
7. 15% discount applied
8. UserVoucher marked as Used

---

## All Voucher Fields Explained

### Identifier & Code
- **VoucherCode** (string, 1-50 chars) — Unique code user enters. Must be distinct per voucher. Used to look up voucher during booking. Example: "SUMMER20", "VIP_REWARD_Q3".

### Discount Configuration
- **DiscountType** (string: "percent" | "fixed") — How discount is calculated.
  - "percent": 20% of applicable amount
  - "fixed": fixed amount (e.g., 50,000 VND)
  
- **DiscountValue** (decimal) — The discount amount.
  - For "percent": 1-100 (enforced: > 0 and <= 100)
  - For "fixed": > 0 (any positive amount)

### Order Constraints
- **MinOrderValue** (decimal, nullable) — Minimum booking total to use voucher. If null, no minimum. If booking total < this, voucher cannot apply.

### Usage Limits
- **MaxUses** (int, nullable) — Global cap on voucher usage.
  - For public: max total bookings with discount
  - For loyalty: max total redemptions
  - If null: unlimited usage
  - If set: must be > 0

- **UsedCount** (int) — Current usage count (incremented on payment for public, on redemption for loyalty). Never exceeds MaxUses due to locking.

### Time Window
- **ValidFrom** (DateTime UTC) — Earliest moment voucher can be applied. Stored as UTC, displayed in +07:00.
- **ValidUntil** (DateTime UTC) — Latest moment voucher can be applied. Must be > ValidFrom.
- Comparison: `ValidFrom <= now <= ValidUntil`

### Active State
- **IsActive** (bool, default=true) — Admin can deactivate voucher without deletion. Deactivated vouchers cannot be applied. Used for soft-deletes and emergency disables.

### Voucher Type Flags
- **IsRedeemable** (bool, default=false)
  - false = public voucher (always available)
  - true = loyalty voucher (requires redemption first)

### Loyalty Fields (for loyalty vouchers only)
- **RequiredPoints** (int, nullable) — Loyalty points cost to redeem. Must be set if IsRedeemable=true. Must be > 0.
- **ExchangeLimit** (int, nullable) — Max times per user can redeem. Only meaningful if IsRedeemable=true. Must be > 0.

### Rules & Restrictions
- **Rules** (VoucherRule[]) — Array of validation rules. Each rule restricts where discount applies.
  - Example: Cinema=3, DayOfWeek=Saturday, Membership=VIP
  - All rules must pass for voucher to be valid
  - Optional: if empty, no restrictions (voucher applies to all bookings)

### Media & Metadata
- **ImageURL** (string, nullable) — CDN URL for voucher promotional image.
- **ImagePublicId** (string, nullable) — Cloudinary public ID for deletion tracking.
- **Description** (string, nullable) — Admin-facing description. Example: "20% off VIP seats on weekends".
- **CreatedAt** (DateTime UTC) — When voucher was created.

---

## Rule Types Reference

All rule types follow the same validation pattern:

```csharp
public ValidationResult Validate(VoucherRule rule, VoucherValidationContext context)
{
    // Extract required data from rule.RuleValue
    // Compare against context
    // Return Success or Failure
}
```

If validation fails, the entire voucher is rejected (fail-fast). If validation passes, the booking continues to the next rule.

### 1. ApplyScope

**Purpose:** Determines which portion of the booking total the discount applies to.

**RuleValue Format:** One of: "Order", "Ticket", "Food"

**Behavior:**
| RuleValue | ApplicableAmount | Example |
|-----------|-----------------|---------|
| Order | Total (tickets + food) | 300k booking → 300k |
| Ticket | Ticket subtotal only | 2 tickets (200k) + food (100k) → 200k |
| Food | Food subtotal only | 2 tickets (200k) + food (100k) → 100k |

**Validation:**
```
if (RuleValue not in ["Order", "Ticket", "Food"])
    → FAIL "Invalid ApplyScope"
if (ApplicableAmount <= 0)
    → FAIL "Booking has no applicable amount in {RuleValue}"
```

**Example:**
```json
{ "ruleType": "ApplyScope", "ruleValue": "Ticket" }
// Voucher only applies to ticket cost, not food
```

**Note:** If no ApplyScope rule exists, ApplicableAmount defaults to total booking.

---

### 2. Cinema

**Purpose:** Restrict voucher to specific cinema.

**RuleValue Format:** Cinema ID as string (e.g., "3")

**Validation:**
```
if (context.CinemaId.ToString() != RuleValue)
    → FAIL "Voucher is not valid for this cinema"
```

**Example:**
```json
{ "ruleType": "Cinema", "ruleValue": "3" }
// Voucher only valid at Cinema ID 3 (e.g., CGV Downtown)
```

---

### 3. Movie

**Purpose:** Restrict voucher to specific movie.

**RuleValue Format:** Movie ID as string (e.g., "15")

**Validation:**
```
if (context.MovieId.ToString() != RuleValue)
    → FAIL "Voucher is not valid for this movie"
```

**Example:**
```json
{ "ruleType": "Movie", "ruleValue": "15" }
// Voucher only valid for Oppenheimer (Movie ID 15)
```

---

### 4. Room

**Purpose:** Restrict voucher to specific room/auditorium.

**RuleValue Format:** Room ID as string (e.g., "2")

**Validation:**
```
if (context.RoomId.ToString() != RuleValue)
    → FAIL "Voucher is not valid for this room"
```

**Example:**
```json
{ "ruleType": "Room", "ruleValue": "2" }
// Voucher only valid in Room 2 (e.g., IMAX auditorium)
```

---

### 5. SeatType

**Purpose:** Require all seats to be specific type (VIP, Standard, Recliner, etc.).

**RuleValue Format:** Seat type name (e.g., "VIP")

**Validation:**
```
foreach (seat in context.Seats)
    if (seat.SeatType != RuleValue)
        → FAIL "Voucher is only valid for {RuleValue} seats"
```

**Example:**
```json
{ "ruleType": "SeatType", "ruleValue": "VIP" }
// All seats must be VIP type
// Booking: VIP + Standard → FAIL
// Booking: VIP + VIP → PASS
```

---

### 6. Membership

**Purpose:** Require user to have specific membership tier.

**RuleValue Format:** Membership tier (e.g., "Gold", "VIP", "Platinum")

**Validation:**
```
if (context.MembershipTier == null)
    → FAIL "Voucher requires membership but user is not authenticated"
if (context.MembershipTier != RuleValue)
    → FAIL "Voucher is only valid for {RuleValue} members"
```

**Example:**
```json
{ "ruleType": "Membership", "ruleValue": "VIP" }
// User must have VIP tier
```

---

### 7. PaymentMethod

**Purpose:** Require specific payment method.

**RuleValue Format:** Payment method (e.g., "Wallet", "CreditCard", "PayOS")

**Validation:**
```
if (!string.Equals(context.PaymentMethod, RuleValue, OrdinalIgnoreCase))
    → FAIL "Voucher is only valid for {RuleValue} payments"
```

**Example:**
```json
{ "ruleType": "PaymentMethod", "ruleValue": "Wallet" }
// Voucher only applies when paying via wallet
```

---

### 8. DayOfWeek

**Purpose:** Restrict voucher to specific day(s) of week.

**RuleValue Format:** Day name (e.g., "Monday", "Saturday")

**Validation:**
```
showtimeDayOfWeek = context.ShowtimeDateTime.DayOfWeek.ToString()
if (showtimeDayOfWeek != RuleValue)
    → FAIL "Voucher is only valid on {RuleValue}"
```

**Example:**
```json
{ "ruleType": "DayOfWeek", "ruleValue": "Saturday" }
// Showtime must be on Saturday
```

**Note:** Single rule = single day. To support multiple days, create separate vouchers or use custom logic.

---

### 9. Product

**Purpose:** Require booking to include specific product.

**RuleValue Format:** Product ID as string (e.g., "10")

**Validation:**
```
if (!context.Products.Any(p => p.ProductID.ToString() == RuleValue))
    → FAIL "Voucher requires a specific product that is not in this booking"
```

**Example:**
```json
{ "ruleType": "Product", "ruleValue": "10" }
// Booking must include Product ID 10 (e.g., Large popcorn)
// Booking: 2 tickets + popcorn (ID=10) → PASS
// Booking: 2 tickets only → FAIL
```

---

### 10. FoodCategory

**Purpose:** Require booking to include product from specific category.

**RuleValue Format:** Category name (e.g., "Snacks", "Drinks")

**Validation:**
```
if (!context.Products.Any(p => 
    p.Category != null && 
    string.Equals(p.Category, RuleValue, OrdinalIgnoreCase)))
    → FAIL "Voucher requires products from {RuleValue} category"
```

**Example:**
```json
{ "ruleType": "FoodCategory", "ruleValue": "Drinks" }
// Booking must include product from Drinks category
// Booking: Coke (category=Drinks) → PASS
// Booking: Popcorn (category=Snacks) → FAIL
```

---

## Booking Flow

### Public Voucher Booking Flow

**Happy Path:**

```
User initiates booking
  ↓
POST /api/bookings
{
  showtimeId: 123,
  seatIds: [45, 46],
  fnbItems: [{itemId: 10, quantity: 2}],
  voucherCode: "SUMMER20"
}
  ↓
BookingService.CalculatePricingAsync
  ├─ Load voucher by code: "SUMMER20"
  ├─ Check IsActive: true ✅
  ├─ Check ValidFrom <= now <= ValidUntil: ✅
  ├─ Check UsedCount (50) < MaxUses (100): ✅
  ├─ Check BookingTotal (300k) >= MinOrderValue (100k): ✅
  │
  ├─ Build VoucherValidationContext
  │  ├─ CinemaId: 3
  │  ├─ MovieId: 15
  │  ├─ RoomId: 2
  │  ├─ ShowtimeDateTime: Saturday
  │  ├─ MembershipTier: "VIP"
  │  ├─ Seats: [{SeatType: "VIP", Price: 100k}]
  │  ├─ Products: [{ProductId: 10, Category: "Snacks", Qty: 2, Price: 100k}]
  │  ├─ PaymentMethod: "Wallet"
  │  ├─ BookingTotal: 300k
  │  ├─ TicketTotal: 200k
  │  └─ FoodTotal: 100k
  │
  ├─ VoucherRuleEngine.ValidateAsync(context)
  │  ├─ Load rules for voucher
  │  ├─ For each rule:
  │  │  ├─ ApplyScope: "Food" → PASS, ApplicableAmount = 100k
  │  │  ├─ DayOfWeek: "Saturday" → PASS (showtime is Saturday)
  │  │  └─ Membership: "VIP" → PASS (user is VIP)
  │  └─ Return Success(ApplicableAmount=100k)
  │
  ├─ Calculate discount
  │  └─ 100k × 20% = 20k
  │
  ├─ Create booking with discount
  ├─ In payment transaction:
  │  ├─ Lock voucher row with UPDLOCK
  │  ├─ UsedCount++ (50 → 51)
  │  ├─ Check UsedCount (51) <= MaxUses (100): ✅
  │  └─ Commit transaction
  │
  └─ Return booking with 20k discount applied
```

**Validation Failure (Rule Rejects):**

```
User applies voucher on weekday
  ↓
VoucherRuleEngine validates
  ├─ DayOfWeek: "Saturday" required
  ├─ Showtime is Monday
  └─ FAIL: "Voucher is only valid on Saturday"
  ↓
BookingService returns error
  ├─ CalculatePricingResponse.Success = false
  ├─ CalculatePricingResponse.Error = "Voucher validation failed: Voucher is only valid on Saturday"
  └─ User sees error, cannot complete booking
```

**Quota Exhausted:**

```
UsedCount = 100, MaxUses = 100
  ↓
User attempts booking
  ↓
BookingService.CalculatePricingAsync
  ├─ Load voucher
  ├─ Check UsedCount (100) < MaxUses (100): ❌ FAIL
  └─ Return error: "Voucher quota has been exhausted"
```

---

### Loyalty Voucher Booking Flow

**Redemption Phase (User exchanges points for voucher):**

```
User views redeemable vouchers
  ↓
GET /api/vouchers/redeemable
  ↓
VoucherService.GetRedeemableVouchersAsync
  ├─ Query IsRedeemable=true, IsActive=true, ValidFrom <= now <= ValidUntil
  └─ Return list of loyalty vouchers
  ↓
User clicks "Redeem" on "VIP_REWARD_Q3" voucher
  ├─ Cost: 50,000 points
  ├─ Max redemptions: 3
  ↓
POST /api/vouchers/{voucherId}/redeem
  ↓
VoucherService.RedeemVoucherAsync
  ├─ Load user
  ├─ Check user.Role = Customer: ✅
  ├─ Check user.Status = Active: ✅
  ├─ Load voucher (IsRedeemable=true)
  ├─ Check IsActive=true: ✅
  ├─ Check ValidFrom <= now <= ValidUntil: ✅
  ├─ Check user.TotalPoints (200k) >= RequiredPoints (50k): ✅
  ├─ Check RedemptionCount (1) < ExchangeLimit (3): ✅
  ├─ Check UsedCount (99) < MaxUses (100): ✅
  │
  ├─ Create UserVoucher
  │  ├─ UserID: 5
  │  ├─ VoucherID: 42
  │  ├─ RedeemedAt: now
  │  ├─ ExpiredAt: ValidUntil
  │  └─ Status: "Available"
  │
  ├─ Create LoyaltyPoints transaction
  │  ├─ UserID: 5
  │  ├─ VoucherID: 42
  │  ├─ PointsDelta: -50000
  │  ├─ TransactionType: "Exchange"
  │  └─ Description: "Redeemed voucher: VIP_REWARD_Q3"
  │
  ├─ Execute atomic transaction:
  │  ├─ Lock global voucher row (UPDLOCK)
  │  ├─ UsedCount++ (99 → 100)
  │  ├─ Check UsedCount (100) <= MaxUses (100): ✅
  │  ├─ Insert UserVoucher
  │  ├─ Insert LoyaltyPoints
  │  └─ Commit
  │
  └─ Return: "Voucher redeemed successfully. Remaining points: 150,000"
```

**Using Redeemed Voucher in Booking:**

```
User has UserVoucher (Status=Available)
  ↓
User initiates booking with code "VIP_REWARD_Q3"
  ↓
POST /api/bookings
{
  showtimeId: 123,
  seatIds: [45, 46],
  fnbItems: [{itemId: 10, quantity: 2}],
  voucherCode: "VIP_REWARD_Q3"
}
  ↓
BookingService.CalculatePricingAsync
  ├─ Load voucher (IsRedeemable=true)
  ├─ Same validation as public voucher
  ├─ Build context + execute rules
  ├─ Calculate discount
  │
  ├─ Apply voucher in booking
  │  ├─ Find UserVoucher with Status=Available
  │  ├─ Update UserVoucher.Status = "Used"
  │  ├─ Update UserVoucher.UsedAt = now
  │  └─ Commit
  │
  └─ Return booking with discount applied
```

**Redemption Quota Exhausted:**

```
ExchangeLimit = 3, User has redeemed 3× already
  ↓
User clicks "Redeem" again
  ↓
VoucherService.RedeemVoucherAsync
  ├─ Check RedemptionCount (3) < ExchangeLimit (3): ❌ FAIL
  └─ Return error: "Exchange limit reached. Maximum 3 redemptions per user"
```

---

## Sequence Diagrams

### Public Voucher Application (Markdown ASCII)

```
User                 API                   BookingService        VoucherRuleEngine    Database
  │                   │                          │                      │                 │
  ├─POST /bookings───>│                          │                      │                 │
  │                   ├─CalculatePricing────────>│                      │                 │
  │                   │                          ├─LoadVoucher──────────────────────────>│
  │                   │                          │<──────────────────────────────────────┤
  │                   │                          ├─ValidateVoucher                       │
  │                   │                          │  ├─IsActive?                          │
  │                   │                          │  ├─TimeValid?                         │
  │                   │                          │  ├─UsedCount<MaxUses?                 │
  │                   │                          │  ├─MinOrderMet?                       │
  │                   │                          │  └─ExecuteRules──────>│               │
  │                   │                          │                      ├─LoadRules────>│
  │                   │                          │                      │<────────────┤
  │                   │                          │                      ├─ApplyScope   │
  │                   │                          │                      ├─Cinema       │
  │                   │                          │                      ├─Membership   │
  │                   │                          │                      └─Success      │
  │                   │                          │<──────────────────────────────────┤
  │                   │                          ├─CalculateDiscount                  │
  │                   │                          ├─CreateBooking                      │
  │                   │                          │  ├─LockVoucher─────────────────────>│
  │                   │                          │  ├─Increment UsedCount              │
  │                   │                          │  ├─Save───────────────────────────>│
  │                   │                          │  └─Commit Transaction──────────────>│
  │                   │<──────Result─────────────┤                      │               │
  │<──200 Booking────┤                          │                      │               │
  │
```

### Loyalty Voucher Redemption (Markdown ASCII)

```
User                 API                   VoucherService         Database
  │                   │                          │                   │
  ├─GET /redeemable──>│                          │                   │
  │                   ├─GetRedeemable───────────>│                   │
  │                   │                          ├─Query──────────────>│
  │                   │                          │<──────Vouchers────┤
  │                   │<──List───────────────────┤                   │
  │<──Loyalty List────┤                          │                   │
  │
  ├─POST /redeem─────>│                          │                   │
  │                   ├─RedeemVoucher───────────>│                   │
  │                   │                          ├─Validate           │
  │                   │                          │  ├─Points enough?   │
  │                   │                          │  ├─ExchangeLimit?   │
  │                   │                          │  └─UsedCount<Max?   │
  │                   │                          │                   │
  │                   │                          ├─Begin Transaction─>│
  │                   │                          ├─LockVoucher────────>│
  │                   │                          ├─UsedCount++────────>│
  │                   │                          ├─CreateUserVoucher─>│
  │                   │                          ├─DeductPoints──────>│
  │                   │                          └─Commit────────────>│
  │                   │<──Success────────────────┤                   │
  │<──201 Redeemed────┤                          │                   │
```

---

## Adding New Rule Types

### Best Practices

**1. Identify What to Validate**

Ask yourself:
- What data from the booking is required?
- What value should it be compared against?
- What's the failure message if it doesn't match?

**Example:** "Validate that showtime is on specific day"
- Booking data: ShowtimeDateTime
- Compare against: DayOfWeek rule value
- Failure: "Voucher is only valid on {day}"

**2. Add Data to VoucherValidationContext (if needed)**

Only if the validator needs data not already in the context.

```csharp
public sealed class VoucherValidationContext
{
    // Add new field
    public string? MovieGenre { get; set; }
}
```

Update BookingService to populate it:
```csharp
var validationContext = new VoucherValidationContext
{
    // ... existing fields
    MovieGenre = showtime.Movie.Genre,  // NEW
};
```

**3. Create Validator Class**

File: `VoucherRuleEngine/Validators/{RuleType}Validator.cs`

```csharp
public sealed class GenreValidator : IVoucherRuleValidator
{
    public string RuleType => VoucherRuleTypes.Genre;  // NEW constant
    
    public ValidationResult Validate(VoucherRule rule, VoucherValidationContext context)
    {
        var requiredGenre = rule.RuleValue;  // e.g., "Action"
        
        if (context.MovieGenre != requiredGenre)
            return ValidationResult.Failure(
                RuleType, 
                $"Voucher is only valid for {requiredGenre} movies.");
        
        return ValidationResult.Success(0);
    }
}
```

**4. Add RuleType Constant**

File: `Shared/Constants/VoucherRuleTypes.cs`

```csharp
public static class VoucherRuleTypes
{
    // ... existing
    public const string Genre = "Genre";  // NEW
}
```

**5. Register Validator**

File: `Application/Vouchers/RuleEngine/VoucherRuleEngine.cs`

```csharp
private static Dictionary<string, IVoucherRuleValidator> InitializeValidators()
{
    var validators = new IVoucherRuleValidator[]
    {
        // ... existing
        new GenreValidator(),  // NEW
    };
    return validators.ToDictionary(v => v.RuleType);
}
```

**6. Update Validation**

File: `Application/Vouchers/VoucherService.cs`

```csharp
private static string? ValidateRuleConsistency(List<VoucherRuleDto> rules)
{
    var validRuleTypes = new[]
    {
        // ... existing types
        VoucherRuleTypes.Genre,  // NEW
    };
    // ... rest unchanged
}
```

**7. Update Tests (if applicable)**

- Test validator with valid/invalid values
- Test VoucherService rejects invalid rule types
- Test VoucherRuleEngine loads and executes validator

### Design Principles

**Keep validators stateless.** No dependencies, no side effects. Given same (rule, context), always same result.

**Return correct failure message.** User sees `result.ErrorMessage` in API response. Make it actionable.

```csharp
// ❌ Bad: too vague
return ValidationResult.Failure(RuleType, "Validation failed");

// ✅ Good: tells user what's required
return ValidationResult.Failure(RuleType, "Voucher is only valid for Action movies");
```

**Never query database.** All data must be in VoucherValidationContext.

```csharp
// ❌ Bad: queries database
var movie = await _movieRepository.GetByIdAsync(context.MovieId);
if (movie.Genre != requiredGenre) return Failure(...);

// ✅ Good: uses context
if (context.MovieGenre != requiredGenre) return Failure(...);
```

**Fail fast on first mismatch.** VoucherRuleEngine iterates and returns immediately on first failure. No need to collect all errors.

**Return ApplicableAmount=0 if not ApplyScope.** Only ApplyScope validator returns non-zero ApplicableAmount. Others return 0 (unused).

```csharp
// ✅ Correct
return ValidationResult.Success(0);  // Other validators
return ValidationResult.Success(applicableAmount);  // ApplyScope only
```

### Example: Add "LanguageSubtitle" Rule

**Use case:** Voucher only valid for screenings with specific subtitle language.

**Step 1:** Add constant
```csharp
public const string LanguageSubtitle = "LanguageSubtitle";
```

**Step 2:** Add context field
```csharp
public string? SubtitleLanguage { get; set; }
```

**Step 3:** Populate in BookingService
```csharp
SubtitleLanguage = showtime.Movie.SubtitleLanguages?.FirstOrDefault(),
```

**Step 4:** Create validator
```csharp
public sealed class LanguageSubtitleValidator : IVoucherRuleValidator
{
    public string RuleType => VoucherRuleTypes.LanguageSubtitle;
    
    public ValidationResult Validate(VoucherRule rule, VoucherValidationContext context)
    {
        var requiredLanguage = rule.RuleValue;  // e.g., "English"
        
        if (string.IsNullOrWhiteSpace(context.SubtitleLanguage) || 
            context.SubtitleLanguage != requiredLanguage)
            return ValidationResult.Failure(
                RuleType,
                $"Voucher is only valid for {requiredLanguage} subtitles.");
        
        return ValidationResult.Success(0);
    }
}
```

**Step 5:** Register + add to validation

Done. New vouchers can use `"LanguageSubtitle": "English"` rule immediately.

---

## Summary

The voucher system is **production-ready** with:

✅ **Public Vouchers** — Direct discounts, always available within time window  
✅ **Loyalty Vouchers** — Require point exchange, per-user redemption limits  
✅ **10 Built-in Rules** — Cinema, Movie, Room, Seat, Membership, PaymentMethod, DayOfWeek, Product, FoodCategory, ApplyScope  
✅ **Extensible Engine** — Add new rules via Strategy pattern, no core changes  
✅ **Correct Concurrency** — Pessimistic locking prevents UsedCount > MaxUses  
✅ **No N+1 Queries** — Flat validation context, validators never hit database  

**To add a new rule type:** Create validator, register it, add constant, update validation. ~30 mins, no core changes needed.

