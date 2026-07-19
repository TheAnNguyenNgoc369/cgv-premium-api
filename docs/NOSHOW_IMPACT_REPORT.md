# NoShow Booking Status — Impact Report

Inspection-only report. No source files were modified.

Repository root: `D:\CGV_SU26\cgvp`

---

## 1. Booking Status

### Location of constants
`src/CinemaBooking.Shared/Constants/BookingStatus.cs` — `public static class BookingStatus` with string constants (snake_case values).

### Existing statuses (8 total)

| Constant | Value |
|---|---|
| `Pending` | `"pending"` |
| `Paid` | `"paid"` |
| `Cancelled` | `"cancelled"` |
| `Refunded` | `"refunded"` |
| `Used` | `"used"` |
| `Expired` | `"expired"` |
| `PaymentFailed` | `"payment_failed"` |
| `PartiallyRefunded` | `"partially_refunded"` |

Mirror list at `src/CinemaBooking.Application/Common/Enums/DatabaseEnumMappings.cs` lines 25–27 (`BookingStatuses` dictionary).

### DB / EF constraints on `Booking.Status`
- **EF (`BookingConfiguration.cs`)**
  - Line 21: `HasMaxLength(30).IsRequired().HasDefaultValue("pending")`
  - Line 30: `IX_Booking_Status`
  - Line 52: `CK_Booking_Status` check constraint
    ```
    [Status] IN ('pending','paid','cancelled','refunded','used','expired','payment_failed','partially_refunded')
    ```
- **SQL (`database.sql` lines 269, 279)** — same `VARCHAR(30)` + CHECK constraint.
- **Migration** `20260610150201_InitialCreate.cs` line 486. Model snapshot `CinemaBookingDbContextModelSnapshot.cs` line 178. No later migration alters `CK_Booking_Status`.

---

## 2. Booking Entity

`src/CinemaBooking.Domain/Entities/Booking.cs`

| Field | Type | Relevance to NoShow |
|---|---|---|
| `BookingID` | int | — |
| `BookingCode` | string | — |
| `UserID` | int? | — |
| `ShowtimeID` | int | **Needed to know show end-time (candidate for NoShow)** |
| `CreatedByStaffID` | int? | — |
| `SubTotal` / `DiscountAmount` / `FinalAmount` | decimal | Revenue math (see §7) |
| `PointsEarned` / `PointsRedeemed` | int? | Loyalty logic (see §6/§9) |
| `Status` | string, default `"pending"` | **Target field** |
| `QRCode` | string? | Used by check-in |
| `BookingDate` | DateTime | — |
| `UpdatedAt` | DateTime | Written together with status |

### Related fields on other entities
- **Payment status** lives on `Payment` navigation (no `PaymentStatus`/`PaidAt` column on Booking). `Payment.Status` values: `Pending`, `Completed="success"`, `Failed`, `Expired`, `Refunded`, `Cancelled`; timestamp is `Payment.PaidAt`.
- **Check-in fields** live on `Ticket` (`CheckedInAt`, `CheckedInByID`). Prior columns on Booking (`CheckedInAt`, `CheckedInByUserId`) were removed by migration `20260708042718_RemoveObsoleteBookingCheckInFields`.
- **Showtime** navigation exposes `StartTime` and `EndTime` — required to decide NoShow.

---

## 3. Booking Status Transitions

Central setter: `BookingRepository.UpdateBookingStatusAsync` (`Infrastructure/Repositories/BookingRepository.cs:333–347`; line 344 writes `booking.Status = status`).

| # | File : line | Method | Old | New | Reason |
|---|---|---|---|---|---|
| 1 | `Application/Bookings/BookingService.cs:215` | Create booking (`new Booking{...}`) | (new) | `Pending` | Initial state |
| 2 | `Application/Payments/PaymentService.cs:440` | `ProcessWalletPaymentAsync` retry | `PaymentFailed` | `Pending` | Wallet retry reset |
| 3 | `Application/Payments/PaymentService.cs:502` | `ProcessCashPaymentAsync` retry | `PaymentFailed` | `Pending` | Cash retry reset |
| 4 | `Application/Payments/PaymentService.cs:553` | `ProcessPayOSPaymentAsync` retry | `PaymentFailed` | `Pending` | PayOS retry reset |
| 5 | `Application/Payments/PaymentService.cs:625` | `FinalizeSuccessfulBookingAsync` | `Pending` | `Paid` | Payment completed |
| 6 | `Application/Payments/PaymentService.cs:375` | `SynchronizePendingPayOSPaymentAsync` (EXPIRED) | `Pending` / `PaymentFailed` | `Expired` | PayOS link expired |
| 7 | `Application/Payments/PaymentService.cs:396` | `SynchronizePendingPayOSPaymentAsync` (CANCELLED) | `Pending` / `PaymentFailed` | `Cancelled` | PayOS cancelled by user |
| 8 | `Infrastructure/BackgroundJobs/SeatHoldExpirationJob.cs:99` | `ExpireHoldsAsync` | `Pending` / `PaymentFailed` | `Expired` | Seat-hold sweep (1 min) |
| 9 | `Application/Refunds/RefundService.cs:159` | `ProcessRefundAsync` | `Paid` | `Refunded` | Wallet refund |
| 10 | `Infrastructure/Repositories/TicketRepository.cs:188` | `PerformTicketCheckInAsync` (when **all** tickets used) | `Paid` | `Used` | Full check-in |

> No writer currently produces `PaymentFailed` or `PartiallyRefunded` in-repo — they exist as constants and are only read as guards.

---

## 4. Check-in Flow

**Layers:** `CheckInsController` → `CheckInService` → `TicketRepository`. No FluentValidation; input via DataAnnotations on `CheckInRequest.QRCode`.

### Controller
`src/CinemaBooking.API/Controllers/CheckInsController.cs`
- `POST /api/checkins/lookup` (line 21) — staff verification.
- `POST /api/checkins` (line 101) — actual check-in, `[Authorize(Roles = Roles.Staff)]`.
- `GET /api/checkins/history` (line 156).

### Business validation in `CheckInService.CheckInAsync`
File: `Application/CheckIns/CheckInService.cs:101–172` — in this order:

1. Line 109 — ticket exists
2. Line 112 — booking graph loaded
3. Lines 117–120 — staff cinema resolved
4. Lines 122–128 — staff cinema == showtime cinema
5. **Line 130** — `booking.Payment?.Status == PaymentStatus.Completed`
6. **Line 133** — `booking.Status != BookingStatus.Cancelled`
7. Line 136 — no completed refund
8. Lines 140–147 — ticket not Used/Cancelled/Refunded
9. **Lines 149–158 — Time window:** `earliestCheckIn = showtime.StartTime - 15 min` (inline literal; doc says 30 min); `latestCheckIn = showtime.EndTime`
10. Line 160 — delegates to `TicketRepository.PerformTicketCheckInAsync`

### Where `Used` is assigned
`Infrastructure/Repositories/TicketRepository.PerformTicketCheckInAsync` (lines 153–218), inside a serializable transaction:

- Lines 175–177 — `ticket.Status = TicketStatus.Used; ticket.CheckedInAt = now; ticket.CheckedInByID = staffId`
- Line 188 — if `AreAllTicketsUsedInBookingAsync == true` → `booking.Status = BookingStatus.Used`
- Lines 193–204 — inserts `AdminActionLog` with `ActionType = AdminActionTypes.CheckIn`

### Doc / code discrepancies
- `docs/CHECKIN_MODULE.md` says 30-minute pre-window and "doesn't change booking status" — code contradicts both.
- Controller-to-service error string `"Ticket has already been checked in."` mismatch — the 409 mapping is dead.

---

## 5. Background Jobs

**Framework:** only built-in `.NET BackgroundService` + `PeriodicTimer`.
**No Quartz, no Hangfire, no Coravel, no cron libraries.**

Registered in `Infrastructure/DependencyInjection.cs:76–84` via `AddHostedService<T>()`, with `BackgroundServiceExceptionBehavior.Ignore`.

| Job | Interval | Writes `Booking.Status`? |
|---|---|---|
| `SeatHoldExpirationJob` | 1 min | Yes → `Expired` |
| `PayOSReconciliationJob` | 30 sec | Indirect (via `PaymentService`) |
| `ShowtimeCompletionJob` | 5 min | No (reads `Paid` for loyalty) |
| `EmailDeliveryJob` | 10 sec | No |
| `NotificationOutboxJob` | 10 sec | No |

> No existing job scans past-showtime bookings for NoShow — a new job (or extension of `ShowtimeCompletionJob`) is required.

---

## 6. Booking Status Filter Queries

### Seat availability / booked-seat queries
- `BookingRepository.GetUnavailableSeatIdsAsync` (38–63) — raw strings `"pending" | "paid" | "used"` (omits `PartiallyRefunded`).
- `ShowtimeRepository.HasActiveBookingOrHoldAsync` (91–101) — `Pending | Paid | Used | PartiallyRefunded`.
- `ShowtimeRepository.HasSuccessfulBookingAsync` (103–108) — `Paid | Used | PartiallyRefunded`.
- `ShowtimeRepository.GetSoldOutShowtimeIdsAsync` (146–192) — `Pending | Paid | Used | PartiallyRefunded`.
- `ShowtimeRepository.GetBookedSeatIdsAsync` (255–261) — same four.

### Payment / booking lifecycle
- `PaymentService.InitiatePaymentAsync` lines 84–86 — `PaymentFailed` retry detection.
- `PaymentService.ValidateBookingForPaymentAsync` line 665 — must be `Pending` or `PaymentFailed`.
- `PaymentService.SynchronizePendingPayOSPaymentAsync` — reads before writing.
- `BookingController.SynchronizePendingPayOSBookingAsync` line 207 — only re-syncs `Pending`.

### Check-in / refund
- `CheckInService.CheckInAsync` line 133 — rejects `Cancelled`.
- `RefundService.ProcessRefundAsync` line 94 — rejects `Cancelled`; relies on payment/ticket checks for others.

### Background jobs
- `SeatHoldExpirationJob.ExpireHoldsAsync` lines 73–75 and 92–93 — targets `Pending` / `PaymentFailed`.
- `ShowtimeCompletionJob` lines 73–77 — awards loyalty only for `Paid`.

### Test file
- `tests/CinemaBooking.API.Tests/CheckInServiceTests.cs` — sets fixtures to `Paid` (275, 342) and `Cancelled` (465).

---

## 7. Reports / Dashboard

`src/CinemaBooking.Infrastructure/Reports/ReportService.cs`

- **`Payments(...)` (lines 20–27)** — base query used by every report:
  ```csharp
  p.Status == Completed
  && p.Booking.Status != Cancelled
  && p.Booking.Status != Refunded
  ```
  A **negative** filter. It currently includes `Paid`, `Used`, `Expired`, `PaymentFailed`, `PartiallyRefunded` — and would silently include `NoShow` unless updated.

  Consumers: `RevenueSummaryAsync`, `MoviePerformanceAsync`, `TopSellingAsync`, `RevenueAsync`, `ExportAsync`, `RevenueRows`, `FnbRows`, `OccupancyRows`.

- **`RevenueAsync` (lines 128–130)** — stricter: `Paid || Used`. NoShow would be silently excluded here (probably correct, but must be intentional).

`src/CinemaBooking.Infrastructure/Repositories/MovieRepository.GetMovieTicketSalesAsync` (84–105) — same negative-filter shape as `Payments(...)`.

`src/CinemaBooking.Infrastructure/Repositories/LoyaltyRepository.GetUserTotalSpentAsync` (30–39) — counts only `Paid`.

---

## 8. Refund

`RefundService.ProcessRefundAsync` — the sole refund path — gates on:

1. `booking.Payment.Status == Completed` (else reject "not paid")
2. `booking.Payment.Status != Refunded` (else reject already-refunded)
3. No ticket with `Status == "used"` (else reject; blocks refund after check-in)
4. `booking.Status != BookingStatus.Cancelled`

**Currently refundable statuses (implicitly):** `Paid` bookings whose payment is `Completed` and no ticket has been checked in.

**Not refundable (implicitly):** `Cancelled`, `Refunded`, `Used`, `Expired`, `PaymentFailed`, `PartiallyRefunded`, plus any booking whose ticket is already `Used`.

`PartiallyRefunded` is never explicitly rejected — it can only stop being refundable if payment status changes, but no writer produces `PartiallyRefunded` in-repo.

---

## 9. Impact of Introducing `NoShow`

Every module that touches booking status is affected. Highlights:

1. **DB CHECK constraint** — `NoShow` will be rejected by SQL Server until `CK_Booking_Status` is updated. Requires migration + `database.sql` update + `BookingConfiguration.cs` update + `DatabaseEnumMappings.cs` update.
2. **Seat availability** — decide whether NoShow releases seats:
   - If NoShow means "show already passed", seats are trivially free regardless. `ShowtimeRepository.HasActiveBookingOrHoldAsync` / `HasSuccessfulBookingAsync` may still need it (block showtime deletion because the sale happened).
   - `BookingRepository.GetUnavailableSeatIdsAsync` — decide inclusion.
3. **Revenue reports** — `ReportService.Payments(...)` will keep counting NoShow revenue by default. `RevenueAsync` will drop it. Decision needed: is NoShow revenue recognized?
4. **`MovieRepository.GetMovieTicketSalesAsync`** — same decision.
5. **Loyalty** — `LoyaltyRepository.GetUserTotalSpentAsync` and `ShowtimeCompletionJob` currently key on `Paid`. Decide whether NoShow should still earn points / count toward spend.
6. **Refund** — decide whether NoShow bookings are refundable. Current implicit rules only reject `Cancelled`; a NoShow booking would pass the current gate and be refundable unless explicitly blocked.
7. **Check-in** — irrelevant if NoShow is set AFTER `EndTime` and check-in cutoff is `EndTime`; but `CheckInService` should probably reject NoShow explicitly for defense-in-depth.
8. **Payment retries** — `ValidateBookingForPaymentAsync` (line 665) rejects everything except `Pending`/`PaymentFailed`, so NoShow will naturally not accept new payments (good).
9. **Background job to produce NoShow** — none exists. A new hosted service (or extending `ShowtimeCompletionJob`) is required to scan `Paid` bookings whose showtime has ended and no ticket is `Used`, then flip to NoShow.
10. **Front-end / API response DTOs** — any DTO surfacing `BookingStatus` (e.g. `booking.Status` returned from `BookingController` GET, `CheckInLookup` responses) will now sometimes carry `"no_show"`. Consumers need to render it.
11. **Manager dashboards / exports** — Excel/PDF export uses `Payments(...)` and will include NoShow rows.
12. **Historical migration** — no back-fill of NoShow for existing rows unless explicitly desired.

---

## 10. Recommendation

### A. Is adding `NoShow` safe?

**Yes, with care.** The change is not disruptive because the codebase already tolerates additional statuses through constants/mappings, but the DB CHECK constraint and several critical reads MUST be updated in the same PR — otherwise:

- Any attempt to write `"no_show"` will throw `SqlException` at commit time.
- Revenue reports will silently include NoShow at full value.
- Refund service will treat NoShow bookings as refundable.

### B. Files that MUST be modified

**Constants / mappings (2)**
- `src/CinemaBooking.Shared/Constants/BookingStatus.cs` — add `NoShow = "no_show"`
- `src/CinemaBooking.Application/Common/Enums/DatabaseEnumMappings.cs` — add `"no_show"` to `BookingStatuses`

**DB / EF (3)**
- `src/CinemaBooking.Infrastructure/Persistence/Configurations/BookingConfiguration.cs` — extend `CK_Booking_Status` value list
- New EF migration under `src/CinemaBooking.Infrastructure/Migrations/` that DROPs and re-adds `CK_Booking_Status`
- `database.sql` — same constraint update for fresh installs

**Refund gate (1)**
- `src/CinemaBooking.Application/Refunds/RefundService.cs` — explicit decision (accept or reject NoShow)

**Reports (2)** — decide inclusion:
- `src/CinemaBooking.Infrastructure/Reports/ReportService.cs` — `Payments(...)` (lines 20–27) and possibly `RevenueAsync` (128–130)
- `src/CinemaBooking.Infrastructure/Repositories/MovieRepository.cs` — `GetMovieTicketSalesAsync`

**Producer of the new state (1)** — either:
- New hosted service `Infrastructure/BackgroundJobs/NoShowMarkingJob.cs` registered in `Infrastructure/DependencyInjection.cs`, OR
- Extend `ShowtimeCompletionJob.cs` to flip eligible `Paid` bookings to `NoShow` when the showtime is complete and no ticket is `Used`.

**Defense-in-depth (2)**
- `src/CinemaBooking.Application/CheckIns/CheckInService.cs` — reject NoShow explicitly
- `src/CinemaBooking.Application/Payments/PaymentService.cs` — `ValidateBookingForPaymentAsync` already blocks it via the `!= Pending && != PaymentFailed` check; no change strictly needed

**Loyalty (decision, 2)** — if NoShow should NOT earn points or count toward spend, no code change needed (both already require `Paid`); if it should, update:
- `src/CinemaBooking.Infrastructure/BackgroundJobs/ShowtimeCompletionJob.cs`
- `src/CinemaBooking.Infrastructure/Repositories/LoyaltyRepository.cs`

**Test coverage** — extend `tests/CinemaBooking.API.Tests/CheckInServiceTests.cs` and add new tests for the NoShow producer.

### C. Files that should NOT be modified

- **Booking creation** (`BookingService.cs`) — initial state stays `Pending`.
- **PayOS flow assignments** (`PaymentService.cs` retry / finalize / sync branches) — unrelated to NoShow.
- **`SeatHoldExpirationJob`** — targets `Pending`/`PaymentFailed`, not `Paid`; unrelated.
- **`TicketRepository.PerformTicketCheckInAsync`** — only reachable if check-in passes; unrelated.
- **`BookingController`, `CheckInsController` display code** — status is passed through as a string; no change needed unless localized labels are wanted.
- **All other migrations** — do NOT edit past migrations; only add a new one.
- **Model snapshot** (`CinemaBookingDbContextModelSnapshot.cs`) — regenerated automatically by `dotnet ef migrations add`; do not hand-edit.
- **Domain `Booking` entity** — `Status` remains `string`; no field additions needed.
- **Seat availability queries** in `ShowtimeRepository` — showtime has already ended when NoShow is set, so seat availability is moot. Leave alone (or add for consistency only after product decision).

### D. Recommended implementation strategy

1. **Product decisions first** (before writing code):
   a. Does NoShow count as revenue?
   b. Is NoShow refundable?
   c. Does NoShow earn loyalty?
   d. Trigger rule — e.g., `Booking.Status == Paid` AND `Showtime.EndTime < now` AND no ticket has `Status == "used"`.
2. **Add constant + mapping** — trivial, non-breaking.
3. **DB migration** — drop and re-add `CK_Booking_Status` with the extra value. Ship this migration standalone so it can be rolled back cleanly if needed. Also update `database.sql`.
4. **Producer** — add a `NoShowMarkingJob` (5-min interval, mirror `ShowtimeCompletionJob` structure and its `Serializable` isolation) rather than shoehorning into an existing job — separation of concerns and easier to disable. Follow the `ExecuteUpdateAsync` pattern for atomicity, or per-row loop matching `SeatHoldExpirationJob` if you also want to write audit info.
5. **Gate updates** based on step 1 decisions — Refund service, Report service, Movie ticket sales, Loyalty.
6. **Explicit check-in rejection** for NoShow (belt-and-braces even though showtime is already past).
7. **Tests** — cover both the producer job (candidate selection under concurrent check-in) and the negative gates.
8. **Deploy order** — migration + code together; the CHECK constraint is the failure mode if migration lags.
