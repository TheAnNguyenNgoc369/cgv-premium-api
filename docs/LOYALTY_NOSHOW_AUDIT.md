# Loyalty Point Awarding — Compatibility Audit for `BookingStatus.NoShow`

Audit-only report. No source files were modified.

Repository root: `D:\CGV_SU26\cgvp`

---

## 1. Loyalty flow

Loyalty grants are **fully decoupled** from payment. The flow is:

```
Payment webhook / wallet / cash confirm
    ↓
PaymentService.FinalizeSuccessfulBookingAsync
    ↓
Booking.Status = "paid"
Tickets created, QR generated
    ↓                        (no loyalty write here)
    ⋮
(time passes; Showtime.EndTime is reached)
    ↓
ShowtimeCompletionJob (every 5 min, Serializable tx)
    ├─ Flip Showtime.Status: "scheduled" → "completed"
    ├─ Select bookings WHERE
    │       Status == BookingStatus.Paid
    │    && UserID.HasValue
    │    && Showtime.Status == "completed"
    │    && !LoyaltyPoints.Any(p => p.TransactionType == "earn")
    ├─ For each booking:
    │       pointsEarned = (int)(FinalAmount * 0.001m)
    │       User.TotalPoints += pointsEarned
    │       Recompute User.LoyaltyTierID
    │       Booking.PointsEarned = pointsEarned
    │       Insert LoyaltyPoints { TransactionType = "earn", BookingID, PointsDelta, ... }
    ├─ Set SESSION_CONTEXT('SkipLoyaltyPointTrigger') = 1  → SaveChanges → clear
    └─ Commit
```

**DB safety net:** unique filtered index `UQ_LoyaltyPoints_BookingID_Earned` on `BookingID` where `TransactionType = 'earn'` — a second earn insert for the same booking would raise a unique-key violation.

**DB trigger `TR_Users_TotalPoints_Adjustment`** fires AFTER UPDATE on `Users`. It writes an `adjust` `LoyaltyPoints` row for any `TotalPoints` delta unless `SESSION_CONTEXT('SkipLoyaltyPointTrigger') = 1`. The job suppresses it during its own earn write.

---

## 2. Files involved

### Producers / earn writers
- `src/CinemaBooking.Infrastructure/BackgroundJobs/ShowtimeCompletionJob.cs` — the **only** production earn path (lines 47–130; predicate lines 73–77; insert lines 103–111).
- `src/CinemaBooking.Application/Membership/MembershipService.cs:131–163` — `AddPointsAfterPaymentSuccessAsync` is **DEAD** (declared in `IMembershipService.cs:25`; `PaymentService` does not inject `IMembershipService`).

### Configuration / constants
- `src/CinemaBooking.Shared/Constants/MembershipTiers.cs` — `PointsPerVnd = 0.001m`, `LoyaltyTransactionTypes.Earned = "earn"`.
- `src/CinemaBooking.Infrastructure/Persistence/Configurations/LoyaltyPointsConfiguration.cs` — unique filtered index on `earn`.
- `src/CinemaBooking.Infrastructure/Migrations/20260703060834_AddTotalPointsAdjustmentTrigger.cs` — trigger DDL.

### Readers / reversers (relevant to NoShow)
- `src/CinemaBooking.Infrastructure/Repositories/LoyaltyRepository.cs:30–39` — `GetUserTotalSpentAsync` counts only `Status == Paid`.
- `src/CinemaBooking.Application/Refunds/RefundService.cs` — does **NOT** touch loyalty.
- `src/CinemaBooking.Application/Payments/PaymentService.cs:765` — `CalculatePointsEarned` is a wallet-response preview only (never persisted).

### Related consumers (not writers)
- `src/CinemaBooking.Application/Membership/MembershipService.cs` — `GetMyMembershipAsync`.
- `src/CinemaBooking.Infrastructure/Repositories/UserVoucherRepository.cs` — voucher redemption spends (negative `exchange` row).
- `src/CinemaBooking.Application/Vouchers/VoucherService.cs` — calls `RedeemVoucherAsync`.

---

## 3. Current behavior

- **Points are granted after Showtime ends, NOT after payment or check-in.**
- The trigger is the `ShowtimeCompletionJob` polling every 5 minutes — not a webhook, not a DB trigger.
- Eligibility requires all four: `Status == Paid`, non-null `UserID`, `Showtime.Status == "completed"`, no prior `earn` row.
- Once granted, points are **never reversed** — not on refund, not on cancellation, not on any subsequent status change.

### Exact eligibility predicate (verbatim `ShowtimeCompletionJob.cs:71–78`)

```csharp
var eligibleBookings = await dbContext.Bookings
    .Include(booking => booking.User)
    .Where(booking => booking.Status == BookingStatus.Paid
        && booking.UserID.HasValue
        && booking.Showtime.Status == "completed"
        && !booking.LoyaltyPoints.Any(point =>
            point.TransactionType == LoyaltyTransactionTypes.Earned))
    .ToListAsync(cancellationToken);
```

---

## 4. Is NoShow compatible?

**Answer: NO** — functionally compatible (no crash, no runtime error), but **semantically wrong** for the intended business rule.

Two distinct reasons:

### 4.1. Pre-existing bug, independent of NoShow
The predicate is `Status == Paid`. `TicketRepository.PerformTicketCheckInAsync:188` flips `Paid → Used` at last check-in. So:

- A customer who **actually shows up and checks in every ticket** stops matching the predicate → **earns zero points**.
- A customer who **never checks in** stays in `Paid` at showtime end → **earns full points**.

### 4.2. What NoShow does to this
Once a NoShow producer exists, it will flip `Paid → NoShow` for the very users who currently DO earn points (paid but absent). So NoShow **turns off loyalty rewards for exactly the wrong group** — the "reward the absentee" bug quietly gets fixed as a side effect of introducing NoShow, but the "penalize the check-in" bug is unchanged.

### Additional NoShow-specific concerns
1. **Race with the 5-minute job.** If a future NoShow producer runs BEFORE the job's next tick, the booking flips to NoShow and is silently excluded from the earn query — user gets no points (probably intended).
2. **Race the other way.** If the job runs FIRST (which it likely will if it runs on `EndTime <= now` and the NoShow producer runs on a longer poll), points are awarded and NoShow later cannot revoke them — because there is no reversal path anywhere in the codebase.
3. **`GetUserTotalSpentAsync` already excludes NoShow** because it only counts `Paid`. So "total spent" naturally drops when a booking flips to NoShow, but the previously-awarded `TotalPoints` and `earn` row stay. Result: `TotalPoints > sum(TotalSpent × PointsPerVnd)` — reconciliation off.

---

## 5. Potential issues

| # | Issue | Location |
|---|---|---|
| 1 | Points are awarded for **absent customers** and **not** for **checked-in customers** (predicate looks for `Paid`, not `Paid \|\| Used`) | `ShowtimeCompletionJob.cs:73–77` |
| 2 | No loyalty reversal on any `Booking.Status` change — refund, cancel, NoShow all leave earned points intact | `RefundService.cs` (no touch), everywhere else (no touch) |
| 3 | `GetUserTotalSpentAsync` excludes `Used` bookings — checked-in customers appear to spend less than they actually did | `LoyaltyRepository.cs:30–39` |
| 4 | Race window between the future NoShow producer and the 5-min `ShowtimeCompletionJob` — result depends on which runs first | future producer vs. `ShowtimeCompletionJob.cs:14` |
| 5 | Dead code path `MembershipService.AddPointsAfterPaymentSuccessAsync` still declared — misleading to readers who think loyalty is granted at payment | `MembershipService.cs:131–163` |
| 6 | Two divergent points formulas: job uses `MembershipTiers.PointsPerVnd (0.001m)`, `PaymentService` preview uses `amount / 1000m` — cosmetic today but drift-prone | `MembershipTiers.cs` vs `PaymentService.cs:765` |
| 7 | `database.sql:489` CK constraint on `LoyaltyPoints.TransactionType` lists `('earn', 'redeem', 'expire', 'adjust')` and is missing `'exchange'` — fresh installs will reject voucher redemption; EF migration path has it | `database.sql:489` vs `LoyaltyPointsConfiguration.cs:42` |
| 8 | Unique filtered index `UQ_LoyaltyPoints_BookingID_Earned` protects only the DB from double-earn; the LINQ predicate does the same in-tx guard, so idempotency is solid — but neither addresses reversal semantics | `LoyaltyPointsConfiguration.cs:21–24` |

---

## 6. Recommendation

Do **NOT** ship a NoShow producer without deciding these three business rules and updating the loyalty code accordingly.

### A. When are points earned?
- Current implicit rule (predicate `Status == Paid` at showtime end) rewards **absence, not attendance**. Almost certainly unintended.
- Options: change the predicate to `Status == Paid || Status == Used`, or move the earn to check-in time, or move it to payment time (matching the dead `AddPointsAfterPaymentSuccessAsync`).

### B. Does NoShow earn points?
- If NoShow means "paid but absent → penalty," they should NOT earn. Under the recommended fix in (A) — `Paid || Used` — NoShow is naturally excluded.
- If the rule is "customer already paid, they get points regardless," add `NoShow` to the predicate. But then the whole point of NoShow-as-penalty disappears.

### C. Sequencing / race safety
- Whichever job produces NoShow must run **after** `ShowtimeCompletionJob` has had a chance to award, OR must be interlocked via a shared unique index / same transaction. Otherwise the outcome depends on cron drift.
- Cleaner design: have `ShowtimeCompletionJob` do both — mark showtime complete, award points to attendees (`Paid || Used`), and flip remaining `Paid` (never checked in) to `NoShow` — in one Serializable transaction. This removes the race entirely and matches how NoShow is semantically defined (paid AND showtime ended AND not checked in).

### Immediate suggested follow-ups (audit only — do not implement yet)
1. Decide business rules A, B, C above.
2. Fix predicate to include `Used` OR move earn to check-in (independent of NoShow — this is a pre-existing bug).
3. Delete the dead `MembershipService.AddPointsAfterPaymentSuccessAsync` and its supporting `LoyaltyRepository` methods (`AddLoyaltyPointAsync`, `UpdateUserTotalPointsAsync`, `HasPointsForBookingAsync`) if no plan exists to wire them.
4. Add `'no_show'` to `GetUserTotalSpentAsync`'s filter only if NoShow should count as spend (probably not — but confirm).
5. Fix `database.sql:489` CK constraint to include `'exchange'` so fresh installs don't break voucher redemption (orthogonal to NoShow, but the same file is being touched).
6. Document whether refund should reverse loyalty (also orthogonal, but same defect class).

---

**No code was changed. This is audit-only.**
