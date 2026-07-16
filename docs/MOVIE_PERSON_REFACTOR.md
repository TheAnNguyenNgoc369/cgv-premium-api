# Movie–Person Refactor — Technical Documentation

**Audience:** Backend, Frontend, QA
**Status:** Shipped
**Scope:** Normalizes Movie cast/crew from free-text fields into a first-class `Person` model with reusable references, autocomplete, and a full CRUD API.

---

## 1. Background

### The old design

Historically the `Movie` table stored people as two free-text columns:

- `Director` — `NVARCHAR(100)`, one name.
- `Cast` — `NVARCHAR(MAX)`, comma-separated string of actor names.

Creating and editing a movie meant the frontend sent raw strings; the backend stored them verbatim.

### Why it did not scale

| Problem | Concrete symptom |
|---|---|
| **No identity for a person** | "Christopher Nolan" appearing in 5 movies was five unrelated strings. Renaming him meant updating every row. |
| **Spelling drift** | `"Tom Hardy"`, `"Tom  Hardy"`, `"tom hardy"`, `"Tom Hard"` all coexisted; queries and filters could not join them. |
| **No structured search** | "Show me every movie with Tom Hardy" required a `LIKE '%Tom Hardy%'` scan — slow and false-positive-prone (matches `"Tom Hardy Jr."`, `"Not Tom Hardy"`, etc.). |
| **No autocomplete** | Frontend had no source of truth to suggest names as the user typed. |
| **One role type only** | The schema hard-coded exactly two shapes: one director + a cast blob. Adding Writer, Producer, or Composer would mean more free-text columns. |
| **No referential integrity** | Deleting a person was impossible — there was nothing to delete. Correcting a typo was a global find-and-replace. |
| **Comma ambiguity** | `"Smith, Jr., Tom"` broke naive splits. Some rows in production contained the placeholder `"Đang cập nhật"` ("updating…") instead of actual data. |

The refactor replaces both string columns with a normalized many-to-many relationship rooted in a `Person` entity.

---

## 2. Architecture — Before vs After

### Before

```
┌───────────────────────────────┐
│           Movie               │
├───────────────────────────────┤
│ MovieID   PK                  │
│ Title                         │
│ Director   NVARCHAR(100)  ◄── free text
│ Cast       NVARCHAR(MAX)  ◄── comma-separated free text
│ ...                           │
└───────────────────────────────┘
```

One movie owned its own copy of every name.

### After

```
┌──────────────────┐        ┌───────────────────────────┐        ┌──────────────────┐
│      Movie       │        │       MoviePerson         │        │      Person      │
├──────────────────┤        ├───────────────────────────┤        ├──────────────────┤
│ MovieID  PK      │◄───────┤ MovieId       FK          │───────►│ PersonId  PK     │
│ Title            │  1..*  │ PersonId      FK          │  *..1  │ Name  UNIQUE     │
│ AgeRating        │        │ Role  ('Director'│'Actor')│        │ CreatedAt        │
│ ...              │        │ DisplayOrder              │        │ UpdatedAt        │
│ (no Director)    │        │ PK(MovieId,PersonId,Role) │        │                  │
│ (no Cast)        │        └───────────────────────────┘        └──────────────────┘
└──────────────────┘
```

- **`Person`** is the single source of truth for a human being (`Christopher Nolan` exists once).
- **`Movie`** owns no person data anymore.
- **`MoviePerson`** is the join table. Its `Role` column tags each link as `"Director"` or `"Actor"`, and `DisplayOrder` preserves the original ordering (billing order for actors, primary director first).

This is a classic **many-to-many with payload**: a person can appear on many movies, a movie can list many people, and the join row itself carries metadata (`Role`, `DisplayOrder`).

### Why the `Role` column is on the join, not the person

A person is not "a director" or "an actor" globally — Ben Affleck directs some films and acts in others. `Role` describes the relationship *within a specific movie*, so it lives on `MoviePerson`. This also means adding `Writer`, `Producer`, `Composer` later only requires extending the check constraint, not creating parallel tables.

---

## 3. Database Changes

Two new tables. **`Movie` schema loses `Director` and `Cast` columns** (the migration drops them only after data is copied — see §7).

### `Person`

| Column | Type | Constraint |
|---|---|---|
| `PersonId` | `INT IDENTITY(1,1)` | `PK` |
| `Name` | `NVARCHAR(200)` | `NOT NULL`, `UNIQUE` (`UQ_Person_Name`) |
| `CreatedAt` | `DATETIME2` | `NOT NULL`, default `GETDATE()` |
| `UpdatedAt` | `DATETIME2` | `NOT NULL`, default `GETDATE()` |

`Name` is unique, which is what makes reuse safe: two movies referencing "Tom Hardy" both point to the same `PersonId`.

### `MoviePerson`

| Column | Type | Constraint |
|---|---|---|
| `MovieId` | `INT` | `NOT NULL`, FK → `Movie.MovieID` (`FK_MoviePerson_Movie`) |
| `PersonId` | `INT` | `NOT NULL`, FK → `Person.PersonId` (`FK_MoviePerson_Person`) |
| `Role` | `NVARCHAR(50)` | `NOT NULL`, `CK_MoviePerson_Role: Role IN ('Director', 'Actor')` |
| `DisplayOrder` | `INT` | `NOT NULL`, default `0` |

- **Composite PK:** `(MovieId, PersonId, Role)` — the *same* person may appear as **both** Director and Actor on the same movie (e.g., a director cameo), but never twice in the same role.
- **Indexes:**
  - `IX_MoviePerson_PersonId` — supports "which movies is this person in?" lookups, and speeds the FK check.
  - `IX_MoviePerson_MovieId_Role_DisplayOrder` — supports the natural read pattern: "give me all directors (or actors) of movie X, in billing order."

### `DisplayOrder`

An `INT` per row, 0-based, unique per `(MovieId, Role)` in practice. It preserves the meaningful order that the old comma-separated string had implicitly. Every read that renders a cast list orders by this column.

### Why `Person` is reusable across movies

Because `Name` is `UNIQUE` and lookups happen by `PersonId`, one `Person` row represents one human globally. A single `Person(Id=42, Name="Tom Hardy")` can be referenced by unlimited `MoviePerson` rows — one per movie he appears in. Updating his name updates every movie's rendered cast automatically.

### What the schema does **not** enforce

- `MoviePerson` deletion cascade is **not** ON — deletes are handled explicitly by the repository inside a transaction. This is deliberate: it makes accidental data loss harder.
- There is no per-movie "must have at least one director" DB constraint — that's a **service-layer** rule (see §6).

---

## 4. API Changes

### 4.1 Movie API

The three affected endpoints:

| Endpoint | Change |
|---|---|
| `POST /api/movie` | Request body: `director`/`cast` strings → `directorIds`/`actorIds` int arrays |
| `PUT /api/movie/{id}` | Same as create |
| `GET /api/movie/{id}` | Response body: `director`/`cast` strings → `directors`/`actors` object arrays |

Endpoints **unchanged in shape**:
- `GET /api/movie` (list) — never carried director/cast anyway.
- `GET /api/movie/search`
- `PUT /api/movie/{id}/poster`
- `DELETE /api/movie/{id}`

#### `POST /api/movie` — Create Movie

**Old request**
```json
{
  "title": "Inception",
  "genres": ["Sci-Fi", "Thriller"],
  "ageRating": "C13",
  "director": "Christopher Nolan",
  "cast": "Leonardo DiCaprio, Tom Hardy, Elliot Page",
  "synopsis": "…",
  "durationMinutes": 148,
  "showingFromDate": "2026-08-01",
  "showingToDate": "2026-09-30",
  "posterUrl": null,
  "posterPublicId": null,
  "trailerUrl": null
}
```

**New request**
```json
{
  "title": "Inception",
  "genres": ["Sci-Fi", "Thriller"],
  "ageRating": "C13",
  "directorIds": [1],
  "actorIds": [5, 8, 12],
  "synopsis": "…",
  "durationMinutes": 148,
  "showingFromDate": "2026-08-01",
  "showingToDate": "2026-09-30",
  "posterUrl": null,
  "posterPublicId": null,
  "trailerUrl": null
}
```

**Status codes**

| Code | When |
|---|---|
| `201 Created` | Success. `Location: /api/movie/{id}` header set. |
| `400 Bad Request` | Validation failure — see §6. Also returned when a supplied `directorIds`/`actorIds` value does not exist (`"Person(s) not found: 42, 99"`). |
| `401` / `403` | Unauthenticated / not `Admin`. |

#### `PUT /api/movie/{id}` — Update Movie

Same request shape as Create, plus optional `status`. Same status-code semantics, plus `404` if the movie ID does not exist.

The update **fully replaces** the director/actor sets — the repository removes existing `MoviePerson` rows for the movie and inserts new ones. If you send `"actorIds": []`, all actors are removed.

#### `GET /api/movie/{id}` — Movie Detail

**Old response** (relevant fields only)
```json
{
  "movieId": 7,
  "title": "Inception",
  "director": "Christopher Nolan",
  "cast": "Leonardo DiCaprio, Tom Hardy, Elliot Page",
  "...": "..."
}
```

**New response**
```json
{
  "movieId": 7,
  "title": "Inception",
  "genres": ["Sci-Fi", "Thriller"],
  "ageRating": "C13",
  "directors": [
    { "id": 1, "name": "Christopher Nolan" }
  ],
  "actors": [
    { "id": 5, "name": "Leonardo DiCaprio" },
    { "id": 8, "name": "Tom Hardy" },
    { "id": 12, "name": "Elliot Page" }
  ],
  "synopsis": "…",
  "durationMinutes": 148,
  "showingFromDate": "2026-08-01",
  "showingToDate": "2026-09-30",
  "posterUrl": null,
  "posterPublicId": null,
  "trailerUrl": null,
  "status": "coming_soon",
  "is_new": false
}
```

Ordering in `directors[]` and `actors[]` reflects `DisplayOrder` — first entry is the primary director, actors are in billing order.

### 4.2 Person API

Five new endpoints under `/api/persons`.

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `GET` | `/api/persons` | Anonymous | List all persons (sorted by name) |
| `GET` | `/api/persons?search=<term>` | Anonymous | Autocomplete search (LIKE `%term%` on Name) |
| `GET` | `/api/persons/{id}` | Anonymous | Fetch one person |
| `POST` | `/api/persons` | Admin | Create a person |
| `PUT` | `/api/persons/{id}` | Admin | Rename a person |
| `DELETE` | `/api/persons/{id}` | Admin | Delete a person (blocked if in use) |

#### `GET /api/persons` and `GET /api/persons?search=<term>`

Both use the same endpoint; the `search` query parameter is optional.

- **Purpose:** list persons, or filter for autocomplete.
- **Request:** query string `?search=chris` (optional).
- **Behavior:** if `search` is blank/omitted, returns all persons. Otherwise `WHERE Name LIKE '%chris%'` (case-insensitive per default collation), sorted by name.
- **Response 200:**
```json
[
  { "id": 1, "name": "Christopher Nolan" },
  { "id": 42, "name": "Chris Pratt" }
]
```

Optimized for autocomplete: fields are exactly `id` + `name`, nothing else.

#### `GET /api/persons/{id}`

- **Purpose:** fetch full record for a person (e.g. an admin detail page).
- **Response 200:**
```json
{
  "id": 1,
  "name": "Christopher Nolan",
  "createdAt": "2026-07-10T04:12:33.4210000Z",
  "updatedAt": "2026-07-14T09:01:11.0090000Z"
}
```
- **404:** unknown id.

#### `POST /api/persons`

- **Purpose:** create a new person.
- **Request:**
```json
{ "name": "Denis Villeneuve" }
```
- **Response 201:** `PersonResponse` (same shape as `GET /api/persons/{id}`). `Location: /api/persons/{id}`.
- **400:** `{ "success": false, "message": "Name is required" | "Name must be unique" | "Name must be at most 200 characters" }`.
- **401 / 403:** not Admin.

#### `PUT /api/persons/{id}`

- **Purpose:** rename a person. Changes propagate automatically to every Movie referencing this `PersonId`.
- **Request:** `{ "name": "New Name" }`.
- **Response 200:** updated `PersonResponse`.
- **400 / 404 / 401 / 403:** same rules as create, plus `404 Person not found`.

#### `DELETE /api/persons/{id}`

- **Purpose:** delete a person permanently.
- **Response 204:** deleted.
- **404:** unknown id.
- **409 Conflict:** `{ "success": false, "message": "Person is assigned to a movie" }` — the person is referenced by at least one `MoviePerson` row. Must be removed from every movie first.

---

## 5. Frontend Integration Guide

### The mental model

1. `Person` is a **thing** with a stable `id`. Never send a person's `name` to Movie APIs — only the `id`.
2. `Movie` create/update lets the user *pick* people. The picker is powered by the Person search endpoint.
3. `Movie` detail returns `directors[]`/`actors[]` — render both `id` (for edit / linking) and `name` (for display).

### Create/Edit Movie — recommended flow

```
┌─────────────────────────────────────────────────────────────────┐
│  New Movie                                                       │
│                                                                  │
│  Title       [ Inception                              ]          │
│                                                                  │
│  Director(s)                                                     │
│  [ ✕ Christopher Nolan ]  [ chris_______|  ← user typing        │
│                            ┌────────────────────────┐            │
│                            │ Christopher Nolan      │ ← 200 OK   │
│                            │ Chris Pratt            │   from     │
│                            │ Chris Evans            │  /persons  │
│                            │ + Create "chris_______"│  ?search=  │
│                            └────────────────────────┘            │
│                                                                  │
│  Actor(s)                                                        │
│  [ ✕ Leonardo DiCaprio ] [ ✕ Tom Hardy ] [ type to add… ]        │
│                                                                  │
│  [ Save ]                                                        │
└─────────────────────────────────────────────────────────────────┘
```

#### Step-by-step

1. **On input change** (debounced ~200 ms), call:
   ```
   GET /api/persons?search=<user_input>
   ```
   Render the response as a dropdown.
2. **On item click**, add a chip to the field. **Store the `id`, not the `name`**, in your form state. Keep the `name` only for rendering the chip label.
3. **"+ Create" affordance** (Admin only): if the typed text has no exact match, offer a "Create *typed text*" option that calls `POST /api/persons`, and on `201` immediately adds the returned `id` as a chip. On `400 "Name must be unique"`, fall back to a fresh search — someone else may have just created that person.
4. **On Save**, submit the movie payload with `directorIds` and `actorIds` extracted from the chips:
   ```json
   { "title": "...", "directorIds": [1], "actorIds": [5, 8, 12], "...": "..." }
   ```
   Do **not** send names.
5. **On the Movie Detail page**, render from the new response arrays:
   ```jsx
   Directors: {movie.directors.map(p => p.name).join(", ")}
   Actors:    <ul>{movie.actors.map(p => <li key={p.id}>{p.name}</li>)}</ul>
   ```
   Use `p.id` to link into a person profile page or admin edit.

#### Editing an existing movie

Fetch `GET /api/movie/{id}`, pre-populate the chips from `directors[]` and `actors[]`, and use each chip's `id` when submitting. The order the user sees in chips will be the order sent back — and the backend preserves that order via `DisplayOrder`.

#### Error handling

| Server response | UI response |
|---|---|
| `400 "Person(s) not found: 42"` | Show inline error. Refresh the picker options — the ID may have been deleted. |
| `400 "At least one director is required"` | Highlight the director field. |
| `400 "Name must be unique"` (on Person create) | Search again and pick the existing person. |
| `409 "Person is assigned to a movie"` (on Person delete) | Confirm intent; instruct user to remove the person from movies first. |

---

## 6. Validation Rules

All rules are enforced in the service layer and return `400 Bad Request` with `{ "success": false, "message": "…" }`.

### Movie (create + update)

| Rule | Message | Notes |
|---|---|---|
| Title required | `"Title is required"` | Trimmed. |
| At least one director | `"At least one director is required"` | `directorIds` must contain at least one positive int after normalization. |
| Age rating in `P / C13 / C16 / C18` | `"AgeRating must be P, C13, C16, or C18"` | Case-insensitive input, stored uppercase. |
| Duration > 0 | `"DurationMinutes must be greater than 0"` | |
| Showing dates required, from ≤ to | `"ShowingFromDate is required"` / `"ShowingToDate is required"` / `"ShowingFromDate must be before or equal to ShowingToDate"` | |
| Poster URL & PublicId together or neither | `"PosterUrl and PosterPublicId must be provided together"` | |
| Status (update only) in allowed set | `"Status must be coming_soon, now_showing, or ended"` | Optional — omit to preserve existing. |
| Every ID in `directorIds`/`actorIds` exists in `Person` | `"Person(s) not found: <ids>"` | Checked before any DB write, inside the transaction path. |

### Actor is **optional**
`actorIds: []` is valid. Only `directorIds` requires at least one entry.

### Automatic normalization

The service applies these silently — the frontend does not need to do them, but doing so avoids surprise:

- **Duplicate IDs removed** — `[1, 1, 5]` becomes `[1, 5]`.
- **Non-positive IDs stripped** — `0` and negative numbers are dropped before validation.
- **Order preserved** — the position of the first occurrence of each ID determines `DisplayOrder`.

### Person

| Rule | Message |
|---|---|
| Name required | `"Name is required"` |
| Name ≤ 200 chars | `"Name must be at most 200 characters"` |
| Name unique | `"Name must be unique"` |
| Delete blocked if referenced by any `MoviePerson` row | `"Person is assigned to a movie"` (returned `409 Conflict`, not 400) |

Uniqueness is enforced both in the service (pre-check for a friendly error) *and* by the `UQ_Person_Name` unique index (defense in depth).

---

## 7. Migration

### One-shot data migration

Migration `20260716035426_AddPersonAndMoviePerson` runs in four ordered, idempotent steps:

1. Create the `Person` table + `UQ_Person_Name` index.
2. Create the `MoviePerson` table + indexes + `CK_MoviePerson_Role` check constraint.
3. **Copy data from `Movie.Director` and `Movie.Cast` into the new tables** (details below).
4. Only after step 3 succeeds, drop `Movie.Director` and `Movie.Cast`.

The whole file is guarded by `IF EXISTS` / `IF NOT EXISTS` checks, so re-running against a partially-migrated database is safe.

### How the old strings were parsed

| Source | Rule |
|---|---|
| `Movie.Director` | If not `NULL` and not blank after trim → **one** `Person` row + one `MoviePerson(Role='Director', DisplayOrder=0)`. |
| `Movie.Cast` | Split on **comma only**. Trim each fragment. Drop empty fragments. **Ignore rows where the whole `Cast` value is `NULL`, empty, whitespace, or the literal `"Đang cập nhật"` (case-insensitive)**. |

The `"Đang cập nhật"` filter exists because production had rows storing this Vietnamese placeholder ("updating…") instead of real data. The migration would otherwise have created a garbage `Person` named `"Đang cập nhật"`.

Formally the `Cast` filter is:

```sql
WHERE m.Cast IS NOT NULL
  AND LTRIM(RTRIM(m.Cast)) <> ''
  AND LOWER(LTRIM(RTRIM(m.Cast))) <> LOWER(N'Đang cập nhật')
```

### Person reuse, no duplicates

Person insertion:
```sql
INSERT INTO Person (Name, CreatedAt, UpdatedAt)
SELECT DISTINCT s.Name, GETDATE(), GETDATE()
FROM #Src s
WHERE NOT EXISTS (SELECT 1 FROM Person p WHERE p.Name = s.Name);
```

Because `Name` is unique-indexed and the insert is `WHERE NOT EXISTS`, running the migration against a DB that already has some `Person` rows (or re-running after a partial failure) will never create duplicates. The subsequent `INSERT INTO MoviePerson` performs the same `WHERE NOT EXISTS` check against the composite PK.

### Malformed data — `MovieId=31`

Some rows in production had cast strings that could not be parsed by pure comma-split (embedded commas inside names, unbalanced separators, etc.). The migration deliberately **does not** attempt regex, name-guessing, or any heuristic recovery — it simply skips or partially imports based on the rules above.

**`MovieId=31` was corrected by hand in SQL before the migration ran.** This is the sanctioned pattern: for anything the comma-split contract cannot express, fix the source data manually, then let the migration do its deterministic pass.

### What the migration does **not** touch

- No changes to `Booking`, `Showtime`, `Voucher`, or any other module.
- No changes to `AdminActionLog` — Person operations are currently **not** audit-logged, because doing so would require extending the `CK_AdminActionLog_ActionType` check constraint (a schema change out of scope). See §9 for the followup implication.

---

## 8. Advantages

- **Normalized schema.** One row per person. Renames propagate for free.
- **Reusable `Person`.** Christopher Nolan exists once; every movie he touches points to `PersonId=1`.
- **Autocomplete built in.** `GET /api/persons?search=` powers a fast type-ahead everywhere a person is picked.
- **No spelling drift.** The picker forces the user to select a canonical row (or explicitly create a new one). "Tom Hardy" and "Tom  Hardy" cannot coexist.
- **Cleaner APIs.** Requests carry stable numeric IDs; responses carry typed objects `{id, name}`. Frontends can link into person pages without re-parsing free text.
- **Easier maintenance.** Editing a person's name is one `PUT /api/persons/{id}` call, not a global search-and-replace.
- **Referential integrity.** FKs guarantee no orphaned actor entries.
- **Future-proof.** Adding `Writer`, `Producer`, `Composer`, `Cinematographer`, etc. requires only:
  1. adding a constant to `MoviePersonRoles`,
  2. extending the `CK_MoviePerson_Role` check constraint via migration,
  3. optionally exposing new arrays on the Movie DTO.

  No new tables, no new person entities, no schema fan-out.
- **Query-friendly.** "All movies by Nolan" is now `JOIN MoviePerson ON PersonId = 1 WHERE Role = 'Director'` — indexed, fast, exact.

---

## 9. Breaking Changes

Every frontend that calls the Movie API **must** update. Old and new shapes are mutually exclusive — there is no compatibility shim.

### Request-side breaks

| Endpoint | Removed | Added |
|---|---|---|
| `POST /api/movie` | `director: string`, `cast: string` | `directorIds: int[]`, `actorIds: int[]` |
| `PUT /api/movie/{id}` | `director: string`, `cast: string` | `directorIds: int[]`, `actorIds: int[]` |

Sending the old fields is silently ignored (they'll be dropped by binding), and validation will then fail with `"At least one director is required"`.

### Response-side breaks

| Endpoint | Removed | Added |
|---|---|---|
| `GET /api/movie/{id}` (`MovieDetailResponse`) | `director: string`, `cast: string` | `directors: [{id, name}]`, `actors: [{id, name}]` |

Frontends parsing `movie.director` / `movie.cast` will see `undefined`.

### What FE must do, concretely

1. **Movie create/edit form:**
   - Remove the two free-text inputs.
   - Add two multi-select autocomplete fields backed by `GET /api/persons?search=`.
   - Change form state from `director: string`, `cast: string` → `directorIds: number[]`, `actorIds: number[]`.
   - Change submit payload accordingly.
2. **Movie detail page:**
   - Replace `movie.director` → `movie.directors.map(p => p.name).join(", ")` (or preferred rendering).
   - Replace `movie.cast.split(", ")` → `movie.actors` directly.
   - Optionally wire each `{id, name}` to a person profile link.
3. **New Person admin UI:** implement CRUD screens against `/api/persons` (list + search, detail, create, edit, delete-with-confirmation).
4. **Error strings** referenced by translation catalogs:
   - `"Director is required"` → `"At least one director is required"`.
   - New: `"Person(s) not found: <ids>"`, `"Name must be unique"`, `"Name must be at most 200 characters"`, `"Person is assigned to a movie"`.

### Non-breaking / same as before

- Movie list endpoints (`GET /api/movie`, `GET /api/movie/search`) never included director/cast — no change.
- All non-Movie modules (Booking, Showtime, Voucher, Cinema, Room, Seat, Ticket, Product, Refund, Wallet, User, Auth, Notification, Report, Genre) are untouched.

### One current limitation to be aware of

Person create/update/delete operations are **not audit-logged** in `AdminActionLog`. Adding logging requires a schema migration to extend `CK_AdminActionLog_ActionType`. This is a followup, not a regression (there was nothing to log before either — persons did not exist).

---

## 10. Testing Checklist

### Person CRUD

- [ ] `GET /api/persons` returns all persons, sorted by name.
- [ ] `GET /api/persons?search=` with blank/omitted parameter returns all persons.
- [ ] `GET /api/persons?search=chris` returns only persons whose name contains "chris" (case-insensitive).
- [ ] `GET /api/persons/{id}` returns full record with `createdAt` and `updatedAt`.
- [ ] `GET /api/persons/{id}` returns `404` for unknown ID.
- [ ] `POST /api/persons` with valid name returns `201` and correct `Location` header.
- [ ] `POST /api/persons` with empty/whitespace name returns `400 "Name is required"`.
- [ ] `POST /api/persons` with 201-character name returns `400 "Name must be at most 200 characters"`.
- [ ] `POST /api/persons` with duplicate name returns `400 "Name must be unique"`.
- [ ] `POST /api/persons` without Admin role returns `401` / `403`.
- [ ] `PUT /api/persons/{id}` renames; subsequent `GET` on any movie containing that person shows the new name.
- [ ] `PUT /api/persons/{id}` returns `404` for unknown ID.
- [ ] `PUT /api/persons/{id}` returns `400` on rename-to-existing-name collision.
- [ ] `DELETE /api/persons/{id}` returns `204` when the person is not referenced.
- [ ] `DELETE /api/persons/{id}` returns `409 "Person is assigned to a movie"` when they are.
- [ ] `DELETE /api/persons/{id}` returns `404` for unknown ID.

### Movie CRUD

- [ ] `POST /api/movie` with valid `directorIds` and `actorIds` returns `201`; subsequent `GET` shows the persons in the sent order.
- [ ] `POST /api/movie` with `directorIds: []` returns `400 "At least one director is required"`.
- [ ] `POST /api/movie` with `actorIds: []` succeeds — actors are optional.
- [ ] `POST /api/movie` with a non-existent person ID returns `400 "Person(s) not found: <ids>"`; no movie row is created.
- [ ] `POST /api/movie` with `directorIds: [1, 1, 2]` succeeds; only `[1, 2]` are stored.
- [ ] `PUT /api/movie/{id}` fully replaces director and actor sets; previously-linked people are unlinked (but not deleted from `Person`).
- [ ] `PUT /api/movie/{id}` with `actorIds: []` removes all actors.
- [ ] `GET /api/movie/{id}` returns `directors[]` and `actors[]` ordered by `DisplayOrder`; each entry has `id` and `name`.
- [ ] `DELETE /api/movie/{id}` removes all associated `MoviePerson` rows but leaves `Person` rows intact.
- [ ] Movie list endpoints still return their existing shape (no `directors`/`actors` fields).

### Autocomplete

- [ ] Typing "chris" fires `GET /api/persons?search=chris` and populates the dropdown.
- [ ] Selecting an entry stores the `id` in form state (verify via devtools or form snapshot).
- [ ] Submitting the form sends `directorIds`/`actorIds` — not names — in the network tab.
- [ ] Debounce prevents a request on every keystroke.
- [ ] "+ Create *new name*" affordance creates and immediately selects on `201`, gracefully searches again on `400 "Name must be unique"`.

### Validation edge cases

- [ ] Case-insensitive name matching for the `"Đang cập nhật"` filter — `"đang cập nhật"` and `"ĐANG CẬP NHẬT"` are also skipped by the migration.
- [ ] Whitespace-only name on `POST /api/persons` is rejected as required (Trim + IsNullOrWhiteSpace).
- [ ] `directorIds: [0, -1]` — both dropped as invalid, then rejected with `"At least one director is required"`.
- [ ] Sending `director`/`cast` (old shape) on `POST /api/movie` results in `400 "At least one director is required"` — proves old FE calls fail loudly.

### Migration verification (production dry-run)

- [ ] Row count: `SELECT COUNT(*) FROM Person` equals the number of distinct non-null, non-empty, non-`"Đang cập nhật"` director names + distinct cleaned actor names in the pre-migration `Movie.Director` + `Movie.Cast`.
- [ ] For a hand-picked movie (not MovieId=31), the ordered `MoviePerson` rows reproduce the original comma order of its `Cast` string.
- [ ] `MovieId=31` matches the manually-corrected expected data.
- [ ] Every `Person.Name` is unique (`SELECT Name, COUNT(*) FROM Person GROUP BY Name HAVING COUNT(*) > 1` returns zero rows).
- [ ] Every `MoviePerson.MovieId` and `MoviePerson.PersonId` resolves to a real row (no orphans — FKs guarantee this, but verify).
- [ ] `Movie.Director` and `Movie.Cast` columns no longer exist.
- [ ] No row in `MoviePerson` has `Role` outside `('Director', 'Actor')` — enforced by `CK_MoviePerson_Role`.
- [ ] Running the migration again is a no-op — all steps are idempotent.

### Regression checks (out-of-scope but sanity)

- [ ] Booking flow works end-to-end for a migrated movie.
- [ ] Showtime creation for a migrated movie works.
- [ ] Voucher application to a booking for a migrated movie works.
- [ ] Reports that filter by movie still work.

---

*End of report.*
