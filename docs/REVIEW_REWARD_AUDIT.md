# Review Reward Audit — Findings

## Business rules under audit

### Review
- One Booking can only have ONE review.
- A customer may review the same Movie multiple times if they have multiple different Bookings for that Movie.

Examples:

| Action | Result |
|---|---|
| Booking A → Superman → Review | ✅ |
| Booking B → Superman → Review | ✅ |
| Booking A → Review again | ❌ |

### Review Reward
Reward is based on the **user's review history**, NOT the movie.

- The FIRST review ever created by a user receives **FirstReviewPoints**.
- Every review after that receives **NextReviewPoints**.
- Each Booking can only receive a reward once because each Booking can only have one Review.

Example:

| Booking | Points |
|---|---|
| Booking A (first review ever) | FirstReviewPoints |
| Booking B (same movie) | NextReviewPoints |
| Booking C (different movie) | NextReviewPoints |
| Booking D | NextReviewPoints |

Movie does NOT affect reward.

---

## 1. ReviewService reward determination

**File:** `src/CinemaBooking.Application/Reviews/ReviewService.cs`, line **104**.

```csharp
var isFirstReview = !await _reviewRepository.UserHasAnyReviewAsync(userId, cancellationToken);
```

- Uses `UserHasAnyReviewAsync(userId)` — **user-scoped only, no movie parameter**.
- Does NOT call `UserHasReviewedMovieAsync`.
- Does NOT reference `movieId` when picking the reward tier (`movieId` on line 68 is only used later, at line 92, to populate `MovieReview.MovieId`).
- Does NOT reference `Booking.Movie` for reward selection.

**Match:** ✅ Reward is determined by user's review history, not by movie.

---

## 2. Reward calculation

**File:** `src/CinemaBooking.Application/Reviews/ReviewService.cs`, lines **108–111**.

```csharp
var settings = await _rewardSettingsRepository.GetAsync(cancellationToken);
var pointsToAward = isFirstReview
    ? (settings?.FirstReviewPoints ?? 0)
    : (settings?.NextReviewPoints ?? 0);
```

Effectively:

```
if (user has never created any review) reward = FirstReviewPoints;
else                                    reward = NextReviewPoints;
```

- No movie condition.
- No booking condition (booking uniqueness is already gated upstream at line 63).
- The description string at lines 121–123 is text-only ("first movie review" vs "movie review") and does not affect points.

**Match:** ✅ Reward calculation matches spec exactly.

**Ordering note (correct, worth calling out):** `isFirstReview` is computed on line 104 **before** `AddAsync` on line 106. That's the correct order — if it were computed after inserting the new row, `UserHasAnyReviewAsync` would always return true and `FirstReviewPoints` would never be awarded. Current code is correct.

---

## 3. Booking uniqueness

**Enforced at two independent layers:**

| Layer | File | Line | Mechanism |
|---|---|---|---|
| Service | `src/CinemaBooking.Application/Reviews/ReviewService.cs` | 63–66 | `if (await _reviewRepository.BookingHasReviewAsync(bookingId, ...)) return 409 "You have already reviewed this booking."` |
| Fluent config | `src/CinemaBooking.Infrastructure/Persistence/Configurations/MovieReviewConfiguration.cs` | 47 | `builder.HasIndex(r => r.BookingId).IsUnique().HasDatabaseName("UQ_MovieReviews_BookingId")` |
| Applied migration | `src/CinemaBooking.Infrastructure/Migrations/20260718173219_AddMovieReview.cs` | 76–80 | `CreateIndex ... UQ_MovieReviews_BookingId ... unique: true` |
| Snapshot | `src/CinemaBooking.Infrastructure/Migrations/CinemaBookingDbContextModelSnapshot.cs` | 842–844 | `b.HasIndex("BookingId").IsUnique().HasDatabaseName("UQ_MovieReviews_BookingId")` |
| Fluent 1:1 | `MovieReviewConfiguration.cs` | 35–38 | `HasOne(Booking).WithOne(Review).HasForeignKey<MovieReview>(BookingId)` |

**Match:** ✅ Duplicate review per Booking is impossible — DB-enforced and service-enforced.

---

## 4. `GET /api/bookings/my` preview

**File:** `src/CinemaBooking.API/Controllers/BookingController.cs`, lines **289–309**.

```csharp
var rewardSettings = await _reviewRewardSettingsRepository.GetAsync(cancellationToken);
var firstReviewPoints = rewardSettings?.FirstReviewPoints ?? 0;
var nextReviewPoints = rewardSettings?.NextReviewPoints ?? 0;

var userHasAnyReview = await _movieReviewRepository.UserHasAnyReviewAsync(
    userId, cancellationToken);
var reviewPoints = userHasAnyReview ? nextReviewPoints : firstReviewPoints;

return Ok(synchronized.Select(b =>
{
    ...
    var reviewReward = new ReviewRewardResponse(
        Eligible: isUsed,
        Earned: reviewId.HasValue,
        Points: reviewPoints);
    ...
}));
```

- `reviewPoints` is computed **once per request**, based purely on `UserHasAnyReviewAsync(userId)` — user-scoped, not movie-scoped.
- Every booking in the response gets that same `reviewPoints` value.
- No `MovieId` / `Booking.Movie` participates in reward preview.

**Behaviour vs spec:**

| Situation | Preview value |
|---|---|
| User has **never** reviewed → any booking row | `firstReviewPoints` ✅ |
| User has **at least one** review (anywhere) → any booking row | `nextReviewPoints` ✅ |

**Match:** ✅ Exactly matches spec — including the case in Scenario 5 where Booking A and Booking B both preview `FirstReviewPoints` before the user has any review, then both flip to `NextReviewPoints` after Booking A is reviewed.

**Consistency note:** the value is stable across the whole `/my` list because it's computed once. That's the intended behavior per the spec ("after the user already has one review anywhere, every remaining booking previews NextReviewPoints") — not a bug.

---

## 5. Repository — `UserHasReviewedMovieAsync`

**Still present:**

| File | Line |
|---|---|
| `src/CinemaBooking.Application/Common/Interfaces/IMovieReviewRepository.cs` | 13 |
| `src/CinemaBooking.Infrastructure/Repositories/MovieReviewRepository.cs` | 44–48 |

**Usage:** grep results show **zero callers** in `src/**` after the earlier edit removed the check in `ReviewService.cs`.

**Safety:** dead code, no side effects, no orphaned references. Safe to leave in place per instruction.

---

## 6. Database

**Composite unique `UNIQUE(UserId, MovieId)` removed?** ✅

- Not present in fluent config (`MovieReviewConfiguration.cs` — lines 45–48 are the only `HasIndex` calls, none for `(UserId, MovieId)`).
- New migration `20260720185618_RemoveMovieReviewUserMovieUniqueIndex.cs` drops `UQ_MovieReviews_UserId_MovieId` in `Up` (lines 13–15).
- `CinemaBookingDbContextModelSnapshot.cs` — grep for `UQ_MovieReviews_UserId_MovieId` returns no matches.

**Booking-unique `UNIQUE(BookingId)` still exists?** ✅

- Fluent config: `MovieReviewConfiguration.cs:47`.
- Present in original migration `20260718173219_AddMovieReview.cs:76-80` (applied).
- Present in current snapshot `CinemaBookingDbContextModelSnapshot.cs:842-844`.
- Not touched by the new migration.

**Match:** ✅

---

## 7. Regression walkthrough (code trace only, no execution)

| # | Setup | Code path | Result |
|---|---|---|---|
| 1 | Fresh user, POST review on Booking A (Superman) | `ReviewService.CreateAsync` → checks 1–6 pass (nothing yet) → `isFirstReview = !UserHasAnyReview = true` → `pointsToAward = FirstReviewPoints` | ✅ Review created, reward = FirstReviewPoints |
| 2 | After #1, POST review on Booking B (Superman) | Checks 1–6 pass (no review for Booking B yet; composite unique gone) → `isFirstReview = !UserHasAnyReview = false` (one review exists) → `pointsToAward = NextReviewPoints` | ✅ Review created, reward = NextReviewPoints |
| 3 | After #2, POST review on Booking C (Batman) | Same as #2 | ✅ Review created, reward = NextReviewPoints |
| 4 | POST review on Booking A again | Line 63 `BookingHasReviewAsync(A)` returns true → 409 "You have already reviewed this booking." Even if service ever slipped, DB `UQ_MovieReviews_BookingId` would throw on insert. | ✅ Rejected |
| 5a | GET `/api/bookings/my` before any review — Booking A row | `userHasAnyReview = false` → `reviewPoints = firstReviewPoints` | ✅ Preview = FirstReviewPoints |
| 5b | Same request, Booking B row | Same `reviewPoints` value (computed once) | ✅ Preview = FirstReviewPoints |
| 5c | After Booking A reviewed, GET `/api/bookings/my` — Booking B row | `userHasAnyReview = true` → `reviewPoints = nextReviewPoints` | ✅ Preview = NextReviewPoints |
| 5d | Same request, Booking C row | Same `reviewPoints = nextReviewPoints` | ✅ Preview = NextReviewPoints |

**All five scenarios match spec.**

---

## Summary

| # | Question | Answer |
|---|---|---|
| 1 | Audit result | Implementation is aligned with the current spec on every checklist item |
| 2 | Does current implementation match business rules? | **Yes, fully.** |
| 3 | Mismatches found? | **None.** |
| 4 | Exact files/lines involved | `src/CinemaBooking.Application/Reviews/ReviewService.cs:63-66, 104-111`; `src/CinemaBooking.API/Controllers/BookingController.cs:289-309`; `src/CinemaBooking.Infrastructure/Persistence/Configurations/MovieReviewConfiguration.cs:35-38, 47`; `src/CinemaBooking.Infrastructure/Migrations/20260720185618_RemoveMovieReviewUserMovieUniqueIndex.cs:13-15`; snapshot `CinemaBookingDbContextModelSnapshot.cs:842-844` |
| 5 | Are code changes required? | **No.** No changes needed. Leaving `UserHasReviewedMovieAsync` in place per instruction is fine — it's inert dead code. |

**No code was modified.** Audit only.
