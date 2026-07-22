# Complete F&B-Only Voucher Validation Audit

**Scope:** Full audit of voucher validation for F&B-only bookings (no showtime, no seats).
**Sample request analyzed:**

```json
{
  "customerId": 19,
  "fnbItems": [
    { "itemId": 2, "quantity": 2 }
  ],
  "voucherCode": "CINEMABINHDUONG"
}
```

- No `showtimeId`
- No `seatIds`
- Staff creates booking for customer
- F&B-only flow

---

## 1. Execution flow (full call chain)

For a request with no `showtimeId` and no `seatIds` (F&B-only):

```
HTTP: POST /api/bookings/calculate-pricing         (or POST /api/bookings)
 ├─ BookingController.CalculatePricing             BookingController.cs:92-117
 │   or BookingController.CreateBooking            BookingController.cs:119-161
 │      - reads userId via User.FindFirst("userId")    (no cinema claim)
 │      - userId = isStaff ? request.CustomerId : currentUserId
 │      - maps FnbItems -> BookingFnBItemDto
 │      - calls the service
 │
 ├─ BookingService.CreateBookingAsync              BookingService.cs:120-339
 │      - isFnbOnly = !showtimeId.HasValue && seatIds.Count == 0   (:130) -> TRUE
 │      - skips showtime lookup                    (:136 guarded)
 │      - skips seat-hold validation               (:158 guarded)
 │      - delegates pricing / voucher checks ->
 │
 ├─ BookingService.CalculatePricingAsync           BookingService.cs:355-585
 │      - isFnbOnly = TRUE                         (:363)
 │      - showtime stays null                      (:368-383 guarded)
 │      - loads F&B products, computes fnbSubTotal (:411-449)
 │      - loads customer, membership discount      (:457-469)
 │      - voucher pre-checks (active/dates/min/redeemable) (:474-508)
 │      - builds VoucherValidationContext          (:510-542)
 │           CinemaId          = showtime?.Room.CinemaID ?? 0        -> 0
 │           MovieId           = showtime?.MovieID       ?? 0        -> 0
 │           RoomId            = showtime?.RoomID        ?? 0        -> 0
 │           ShowtimeDateTime  = showtime?.StartTime ?? MinValue     -> 0001-01-01 (Monday)
 │           Seats             = empty list
 │           Products          = from normalizedFnbItems
 │           PaymentMethod     = string.Empty
 │      - invokes rule engine                      (:544)
 │
 ├─ VoucherRuleEngine.ValidateAsync                VoucherRuleEngine.cs:21-66
 │      - loads VoucherRule[] via IVoucherRuleRepository.GetByVoucherIdAsync
 │      - foreach rule -> _validators[ruleType].Validate(rule, context)
 │      - first Failure short-circuits and returns
 │
 └─ Validator (concrete):
       CinemaValidator.Validate                    CinemaValidator.cs:13-26
        - requiredCinemaId = rule.RuleValue          e.g. "3"
        - bookingCinemaId  = context.CinemaId.ToString()  = "0"
          "3" != "0"  ->  Failure("Voucher is not valid for this cinema.")
```

Return path: `BookingService.CalculatePricingAsync` propagates `(false, "Voucher is not valid for this cinema.", null)` at line 546-547 → controller returns `400 { success:false, message:"Voucher is not valid for this cinema." }` (`BookingController.cs:113-114` / `:156-157`).

**DI:** `VoucherRuleEngine` is registered `AddScoped<IVoucherRuleEngine, VoucherRuleEngine>()` at `DependencyInjection.cs:18`. Validators are not DI-registered individually — they are constructed inside `VoucherRuleEngine.InitializeValidators()` (`VoucherRuleEngine.cs:68-85`).

Registered validators: `ApplyScope, Cinema, Movie, Room, SeatType, Membership, PaymentMethod, DayOfWeek, Product, FoodCategory`. No others.

---

## 2. VoucherValidationContext audit

Type: `VoucherValidationContext` at `Application/Vouchers/RuleEngine/ValidationResult.cs:34-51`. Every property, its source, and the effective value under each flow (source line numbers in `BookingService.cs`):

| Property | Source | Ticket booking value | F&B-only value (sample request) |
|---|---|---|---|
| `BookingId` | line 512 — literal `0` | `0` (pricing runs pre-persistence) | `0` |
| `CustomerId` | line 513 — passed-in `userId` | e.g. `19` | `19` |
| `CinemaId` | line 514 — `showtime?.Room.CinemaID ?? 0` | e.g. `3` (Bình Dương) | **`0`** |
| `MovieId` | line 515 — `showtime?.MovieID ?? 0` | e.g. `42` | **`0`** |
| `RoomId` | line 516 — `showtime?.RoomID ?? 0` | e.g. `7` | **`0`** |
| `ShowtimeDateTime` | line 517 — `showtime?.StartTime ?? DateTime.MinValue` | real UTC start | **`0001-01-01T00:00:00Z` (Monday)** |
| `MembershipTier` | line 518 — `user?.LoyaltyTier?.TierName` | tier or `null` | same (source is customer, not showtime) |
| `Seats` | lines 519-524 — `seatDetails.Select(...)` when `!isFnbOnly`, else `new List<...>()` | populated | **empty list** |
| `Products` | lines 525-535 — from `normalizedFnbItems` | populated iff F&B added | `[{ ProductID=2, Category=<product.ItemType>, Quantity=2, Price=... }]` |
| `PaymentMethod` | line 536 — hard-coded `string.Empty` | `""` (**always**) | `""` (**always**) |
| `BookingTotal` | line 537 — `seatsSubTotal + fnbSubTotal` | full total | `fnbSubTotal` only |
| `TicketTotal` | line 538 — `seatsSubTotal` | > 0 | **`0`** |
| `FoodTotal` | line 539 — `fnbSubTotal` | 0 or > 0 | > 0 |
| `Voucher` | line 540 — resolved `Voucher` entity | the voucher | `CINEMABINHDUONG` |
| `ValidationTime` | line 541 — `DateTime.UtcNow` | now | now |

There is no `ShowtimeId`, `StaffCinemaId`, or `SeatIds` field in the context.

---

## 3. Rule compatibility matrix

| Rule | Ticket booking | F&B-only booking | Broken? | Expected? | Root cause |
|---|---|---|---|---|---|
| **ApplyScope=Order** | Works (`BookingTotal` > 0) | Works (`BookingTotal = fnbSubTotal > 0`) | No | Yes — F&B alone is valid order | — |
| **ApplyScope=Ticket** | Works | Fails with "no applicable amount in Ticket" | Semantically OK, message poor | By design — no tickets | `TicketTotal = 0` in F&B-only |
| **ApplyScope=Food** | Works iff F&B added | Works | No | Yes | `FoodTotal > 0` |
| **Cinema** | Works (from `showtime.Room.CinemaID`) | **BROKEN — false negative** | **Yes** | Should work using staff's cinema | `CinemaId = showtime?.Room.CinemaID ?? 0`; staff cinema never propagated (`BookingService.cs:514`) |
| **Movie** | Works | Fails ("Voucher is not valid for this movie.") | Semantically OK, message misleading | By design — no movie in F&B | `MovieId = 0` → comparison fails; error string is confusing |
| **Room** | Works | Fails ("Voucher is not valid for this room.") | Semantically OK, message misleading | By design — no room in F&B | `RoomId = 0`; same as Movie |
| **SeatType** | Works | Fails ("Booking has no seats.") | Semantically OK, message misleading | By design — no seats in F&B | `Seats` is empty (`SeatTypeValidator.cs:18-23`) |
| **Membership** | Works | Works | No | Yes | `MembershipTier` sourced from customer, not showtime |
| **PaymentMethod** | **BROKEN (both flows)** | **BROKEN (both flows)** | **Yes — latent bug** | Should work | `context.PaymentMethod = ""` hard-coded at `BookingService.cs:536`; payment isn't chosen yet at pricing time |
| **DayOfWeek** | Works | **BROKEN — silently wrong** | **Yes** | Should work using current UTC day for counter sales | `ShowtimeDateTime = MinValue`; `MinValue.DayOfWeek = Monday`, so a Monday-only voucher passes on any day and every other DoW voucher fails |
| **Product** | Works iff F&B added | Works | No | Yes | `Products` populated from `normalizedFnbItems` regardless of `isFnbOnly` |
| **FoodCategory** | Works iff F&B added | Works | No | Yes | Same source as Product |

- **Broken by defect (should work but don't):** `Cinema`, `DayOfWeek`, `PaymentMethod`.
- **Working:** `ApplyScope=Order`, `ApplyScope=Food`, `Membership`, `Product`, `FoodCategory`.
- **Correctly failing but with misleading messages:** `ApplyScope=Ticket`, `Movie`, `Room`, `SeatType`.

---

## 4. Hidden showtime assumptions in voucher validation

Every place voucher validation implicitly depends on a showtime:

**Context construction (`BookingService.cs`):**
- `:514` `CinemaId = showtime?.Room.CinemaID ?? 0` — assumes showtime present.
- `:515` `MovieId = showtime?.MovieID ?? 0` — assumes showtime present.
- `:516` `RoomId = showtime?.RoomID ?? 0` — assumes showtime present.
- `:517` `ShowtimeDateTime = showtime?.StartTime ?? DateTime.MinValue` — assumes showtime present; fallback silently biases DayOfWeek to Monday.
- `:519-524` `Seats = !isFnbOnly ? seatDetails... : new List<...>()` — empty list is a valid failure signal but leaks into `SeatType` validator messaging.

**Validator side (`Application/Vouchers/RuleEngine/Validators/`):**
- `CinemaValidator.cs:15-22` — string compare `rule.RuleValue` vs `context.CinemaId.ToString()`. No null/zero guard.
- `MovieValidator.cs:15-22` — same shape.
- `RoomValidator.cs:15-22` — same shape.
- `DayOfWeekValidator.cs:15-22` — reads `context.ShowtimeDateTime.DayOfWeek`; unaware showtime may not exist.
- `SeatTypeValidator.cs:18-23` — bails if `Seats` empty ("Booking has no seats.").

**Other showtime usages verified NOT part of voucher validation:**
- `RefundController.cs:112`, `CheckInService.cs:260`, `NotificationOutboxJob.cs:254/274/421` — notification/history payloads.
- `ShowtimeController.cs:170/224`, `ShowtimeService.cs:124/293`, `ShowtimeRepository.cs:135` — showtime/manager scope authz and queries.

**Conclusion:** `BookingService.CalculatePricingAsync` (lines 510-542) is the single injection point that leaks the showtime assumption into voucher validation.

---

## 5. Existing staff-cinema sources (reusable)

- **Column:** `User.CinemaID` (`int?`) — `Domain/Entities/User.cs:14`.
- **Interface:** `IBookingRepository.GetStaffCinemaIdAsync(int staffId, CancellationToken)` — `Application/Common/Interfaces/IBookingRepository.cs:75`.
- **Implementation:** `BookingRepository.GetStaffCinemaIdAsync` — `Infrastructure/Repositories/BookingRepository.cs:222-232` (single scalar query on `Users.CinemaID`).
- **Current callers (already wired in DI):**
  - `BookingController.GetBookingById` — `BookingController.cs:234` (view-authorization).
  - `CheckInService` — `CheckInService.cs:39, 123, 208, 213, 313, 318`.
  - `PaymentService.ValidateStaffCinemaAccessAsync` — `PaymentService.cs:705-731`.
- **Manager scope helper:** `IManagerCinemaScopeService` / `ManagerCinemaScopeService` — `Application/Common/Security/ManagerCinemaScopeService.cs` (used by showtime/room/product management).

Not present in this project: `ICurrentUser` / `IUserContext` abstractions; no cinema claim on `ClaimsPrincipal`. All cinema resolution goes through `GetStaffCinemaIdAsync` today.

---

## 6. Existing F&B-only branches

`isFnbOnly = !showtimeId.HasValue && seatIds.Count == 0` — a local `var` defined in both service methods. All references:

`BookingService.cs`:
- `:130` (declare in `CreateBookingAsync`)
- `:132` guard: reject F&B-only-shaped-but-with-empty-seats mistakes
- `:136` skip showtime lookup + activation checks
- `:158` skip seat-hold pre-check
- `:177` skip building `bookingSeats`
- `:243` skip locking seat holds inside the transaction
- `:312` skip marking holds as confirmed
- `:363` (declare in `CalculatePricingAsync`)
- `:365` guard
- `:369` skip showtime load + status/time/room/cinema-active checks
- `:390` skip seat existence + room-match check
- `:452` require at least one F&B item when F&B-only
- `:519` `Seats = !isFnbOnly ? ... : new List<...>()` in voucher context

Data-model support:
- Migration `20260717091907_MakeShowtimeIDNullableForFnbOnly` made `Booking.ShowtimeID` nullable expressly to support F&B-only orders.

Places touching `showtimeId == null` outside voucher validation are consistent with F&B-only semantics. Only the voucher-context construction (`BookingService.cs:514-517`) and the four downstream validators still assume a showtime.

**Verdict:** Voucher validation is the last remaining place in the pipeline that silently assumes a showtime exists.

---

## 7. Rules grouped by expected behavior

### A. Should work for F&B-only
- `ApplyScope=Order` — order total is well-defined.
- `ApplyScope=Food` — food total is well-defined.
- `Membership` — customer-attached.
- `Product` — evaluates F&B items.
- `FoodCategory` — evaluates F&B items.
- **`Cinema`** — the counter sale happens at a real, known cinema (the staff's).
- **`DayOfWeek`** — a counter sale has a real date (today).
- **`PaymentMethod`** — orthogonal to F&B vs ticket.

### B. Should fail by design for F&B-only
- `ApplyScope=Ticket` — voucher only discounts tickets; there is no ticket total. Reject with a clear "voucher applies only to tickets" message.
- `Movie` — voucher restricts to a specific movie; F&B-only has no movie. Reject with "voucher requires a movie" message.
- `Room` — voucher restricts to a specific room; F&B-only has no room. Reject with "voucher requires a room".
- `SeatType` — voucher restricts to a seat type; F&B-only has no seats. Reject with "voucher requires seats".

### C. Should work but currently fail
- **`Cinema`** — root defect. `CinemaId` is `0` because staff's `User.CinemaID` isn't propagated. Message shown to the user: "Voucher is not valid for this cinema." (`CinemaValidator.cs:22`).
- **`DayOfWeek`** — hidden defect. `ShowtimeDateTime = DateTime.MinValue` when showtime is null; `MinValue.DayOfWeek == Monday` → Monday vouchers wrongly pass every day, others wrongly fail. (`BookingService.cs:517`, `DayOfWeekValidator.cs:16`.)
- **`PaymentMethod`** — pre-existing latent defect (not F&B-specific but exposed identically). `context.PaymentMethod = string.Empty` at `BookingService.cs:536`; every `PaymentMethod` rule fails "Voucher is only valid for X payments." regardless of flow, because the payment method isn't yet known at pricing time.

---

## 8. Final summary

### Root causes
1. **`BookingService.CalculatePricingAsync` does not know the staff's cinema.** For F&B-only orders the only cinema source (`showtime.Room.CinemaID`) is null, so `context.CinemaId` collapses to `0`. `User.CinemaID` / `GetStaffCinemaIdAsync` is not consumed here despite being available and used elsewhere.
2. **`ShowtimeDateTime` falls back to `DateTime.MinValue`** — silently biases `DayOfWeek` validation to Monday. For a counter F&B sale the correct semantic is "purchase time" (now).
3. **`PaymentMethod` is hard-coded to `""`** during pricing. Latent bug affecting both flows.
4. **Cinema/Movie/Room/SeatType validators lack a "context is empty" guard** — they produce user-facing messages that misrepresent the underlying cause when the field is unset.

### Impact
- Any voucher with a `Cinema` rule is **unusable on F&B-only orders**, in every cinema, for every staff member (blocks the reported use case).
- Any voucher with a `DayOfWeek` rule behaves incorrectly on F&B-only: Monday vouchers always pass, others always fail.
- Any voucher with a `PaymentMethod` rule is unusable in both flows (separate latent bug).
- The user-facing messages for `Movie`/`Room`/`SeatType` on F&B-only orders read as authorization errors when they should read as configuration errors ("this voucher isn't compatible with F&B orders").

### Files involved
- `src/CinemaBooking.API/Controllers/BookingController.cs` — endpoints; source for staff identity.
- `src/CinemaBooking.Application/Bookings/BookingService.cs` — `CalculatePricingAsync` / `CreateBookingAsync`; `VoucherValidationContext` construction (lines 510-542) — **primary fix site**.
- `src/CinemaBooking.Application/Vouchers/RuleEngine/VoucherRuleEngine.cs` — dispatcher (no change needed).
- `src/CinemaBooking.Application/Vouchers/RuleEngine/ValidationResult.cs` — `VoucherValidationContext` shape.
- `src/CinemaBooking.Application/Vouchers/RuleEngine/Validators/CinemaValidator.cs` — comparison logic; optional guard.
- `src/CinemaBooking.Application/Vouchers/RuleEngine/Validators/DayOfWeekValidator.cs` — reads `ShowtimeDateTime`.
- `src/CinemaBooking.Application/Vouchers/RuleEngine/Validators/MovieValidator.cs`, `RoomValidator.cs`, `SeatTypeValidator.cs` — optional message improvements.
- `src/CinemaBooking.Application/Common/Interfaces/IBookingRepository.cs` + `src/CinemaBooking.Infrastructure/Repositories/BookingRepository.cs` — existing `GetStaffCinemaIdAsync` to reuse.
- `src/CinemaBooking.Domain/Entities/User.cs` — `CinemaID` column already exists.

### Recommended fixes (high level, no code)
1. **Propagate staff cinema into `VoucherValidationContext`.** Have `BookingController` look up the staff's `CinemaID` via the existing `GetStaffCinemaIdAsync` when `isStaff` and pass it into `BookingService`. In `BookingService`, resolve `CinemaId` as `showtime?.Room.CinemaID ?? staffCinemaId ?? 0`. This is the minimum fix that unblocks the reported use case.
2. **Fix the DayOfWeek fallback.** Use `showtime?.StartTime ?? DateTime.UtcNow` for `ShowtimeDateTime` so counter sales evaluate against the actual purchase day.
3. **Add explicit "context missing" guards** in `CinemaValidator`, `MovieValidator`, `RoomValidator`, `SeatTypeValidator` — return a clearer error when the field is `0`/empty (e.g. "This voucher requires a ticket / movie / room / seat and cannot be applied to F&B-only orders"). Keeps behavior strict while telling the user *why*.
4. **Optional hardening:** parse `rule.RuleValue` to `int` in Cinema/Movie/Room validators before comparing, so whitespace/format drift can't create silent mismatches.
5. **Separate follow-up (out of scope for this bug):** address the hard-coded `PaymentMethod = ""` — either populate it once payment method is chosen (validation at payment time) or scope `PaymentMethod` rules to run in `PaymentService` instead of `BookingService`.
