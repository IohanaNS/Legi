# Library Feature — Reading Dates (Finished / Started)

Feature design for capturing **when** a user finished (and optionally started) reading a book, so the data can power future statistics: books read per month/year, yearly summaries, and reading streaks.

**Status:** 📋 Proposed (design only — not yet implemented)
**Bounded context:** Library
**Author of decision:** design discussion, 2026-06-09

---

## 1. Problem

`UserBook` currently has only `CreatedAt` and `UpdatedAt` ([UserBook.cs](../src/Legi.Library.Domain/Entities/UserBook.cs)). Neither can represent "the day this book was finished":

- `UpdatedAt` changes on **any** later edit (rating, note, status, progress), so it drifts away from the finish moment.
- `CreatedAt` is when the book entered the library, not when it was read.

We need a **stable** finish date that survives unrelated edits, plus the ability to **back-date** it (a user adding a book on the Explore page that they actually read last year) and to **edit** it later if they got it wrong.

### Terminology mapping

The UI/product language ("Read", "Want to read", "Dropped") maps onto the existing [`ReadingStatus`](../src/Legi.Library.Domain/Enums/ReadingStatus.cs) enum:

| Product term | `ReadingStatus` |
|---|---|
| Read | `Finished` |
| Want to read | `NotStarted` (+ `WishList = true`) |
| Reading | `Reading` |
| Paused | `Paused` |
| Dropped | `Abandoned` |

Below, the real enum names are used.

---

## 2. What we already have (and don't need to rebuild)

Two existing facts make this feature smaller than it first appears:

1. **One `UserBook` = one reading cycle.** Per [§2.5 of the Library decisions doc](LIBRARY-ARCHITECTURE-decisions.md), re-adding the same book creates a **new** `UserBook` (a new cycle). So a re-read is already a separate row — finish dates never need to overwrite each other across reads.
2. **`ReadingProgress` already has a dated timeline.** [`ReadingProgress.ReadingDate`](../src/Legi.Library.Domain/Entities/ReadingProgress.cs) is a `DateOnly`, and its `Create` factory already accepts a `readingDate`. This is the per-day reading activity log.

Consequence: **reading streaks** (consecutive days you read *something*) come from distinct `ReadingProgress.ReadingDate` values per user — **not** from the finish date. The finish date and the streak source are correctly different things.

---

## 3. Core design decision: columns on `UserBook`, not a history table

**Decision:** add nullable date fields directly to the `UserBook` aggregate. Do **not** introduce a separate `ReadingHistory` / `ReadingSession` table.

**Rationale.** A separate table earns its keep only when *one library entry can have many reads*. That's been designed away — a re-read is a new `UserBook`. A history table would therefore be a 1:1 satellite of `UserBook` (a smell): extra aggregate, extra repository, extra join, for no invariant.

The three target statistics are all covered without a table:

| Statistic | Source | Needs a table? |
|---|---|---|
| Books read per month/year | `GROUP BY` over `UserBook.FinishedReadingAt` where `Status = Finished` | No |
| Yearly summary ("42 books in 2025") | Same | No |
| Reading streaks | Distinct `ReadingProgress.ReadingDate` per user (already exists) | No |

**Future-proofing.** If multiple reads inside one `UserBook` ever become a requirement, adding a `ReadingSession` table later is **additive**: the `FinishedReadingAt` column becomes a denormalized "latest finish" cache. No painful migration is being deferred.

**Anti-pattern avoided.** Do **not** derive the finish date by writing a `ReadingProgress` row with `Progress.Completed()` and reading it back. That conflates "I posted an update" with "this cycle's outcome" and turns stats into a post-scan. The finish date is an attribute of the *cycle* → it lives on the `UserBook` aggregate. `ReadingProgress` stays the social/activity timeline.

---

## 4. The key modeling move: decouple "is finished" from "when finished"

These are two **independent** facts and must not be coupled:

1. **"Is this book finished?"** — a status. Always known. → `Status == Finished`.
2. **"When did I finish it?"** — a date. May be known, approximate, or **genuinely unknown**. → `FinishedReadingAt` (nullable).

A user can legitimately know (1) without knowing (2): "I read this years ago, I just don't remember when." Forcing a date (e.g. defaulting to today) in that case **corrupts statistics** — an old read lands in the current year's bucket.

Therefore `FinishedReadingAt` is **nullable even while `Finished`**, and `null` means *"finished, date unknown."*

### Invariant (one-way only)

> `FinishedReadingAt != null` ⟹ `Status == Finished`

You cannot hold a finish date while not finished (so it is **cleared on un-finish**). The reverse — "Finished must have a date" — is intentionally **not** an invariant.

### Why nullable-while-finished is correct (not a "trap")

`null` is the semantically correct value, and excluding it from a time series is right, not a hack:

```text
Total finished (all time):   COUNT WHERE Status = Finished
Books read in 2025:          COUNT WHERE Status = Finished
                                   AND FinishedReadingAt IS NOT NULL
                                   AND year(FinishedReadingAt) = 2025
```

Undated finishes are **counted in the all-time total** but **excluded from time buckets** — never mis-attributed. A "2025 wrapped" reads "42 books," optionally footnoted "+3 finished, date not recorded."

---

## 5. Behavior across status transitions

Mirror the existing progress-reset pattern in [`ChangeReadingStatus`](../src/Legi.Library.Domain/Entities/UserBook.cs) (which already nulls `CurrentProgress` when reverting from `Finished`).

| Transition | `FinishedReadingAt` |
|---|---|
| → **Finished** | Set to the supplied date (may be `null` = unknown). Also sets progress to `Completed()` (already done today). |
| **Finished → anything** (un-finish) | **Clear to `null`** (mirrors the existing `CurrentProgress = null` reset). |
| → **Reading** (first time) | Optionally set `StartedReadingAt` if null. Do **not** touch the finish date. |
| → **Abandoned** / **Paused** | Do **not** set a finish date. Abandoned ≠ read. |
| Editing rating / note / progress while already Finished | **Never touch `FinishedReadingAt`** — this is the entire point; it fixes the `UpdatedAt` drift. |

**Auto-finish path:** [`UpdateProgress`](../src/Legi.Library.Domain/Entities/UserBook.cs) calls `ChangeReadingStatus(Finished)` when progress reaches 100%. That path passes **today** explicitly — hitting 100% now means you finished now, date known.

Only the `Finished` status ever carries a date. `Abandoned` must never count as "read" in any statistic.

---

## 6. Required when Finished?

**No** — not as a domain invariant. The date is nullable while finished (see §4).

The **default** (today) is applied in the application/UI layer, **not** in the domain. The domain method faithfully stores what it is handed, including `null`. This keeps "default today" next to the context that justifies it (see §7) and keeps the domain testable without a clock dependency for this field.

---

## 7. The default is context-dependent (the real UX decision)

Defaulting *globally* to today invites mindless confirmation that pollutes stats; defaulting *globally* to empty leaves time-series stats sparse because users won't bother filling it. The fix is to use the **transition's context** as the signal:

| Entry point | Default | Why |
|---|---|---|
| **Reading → Finished** (library; was actively reading) | **Today** | You almost certainly just finished it. |
| **Finished from NotStarted / not-in-library** (Explore, "read it years ago") | **Empty — make them pick** | No signal that you finished today; defaulting today is exactly the corruption to avoid. |

In **both** cases the date control also offers an explicit **"I don't remember the date"** option that sets `null`. One affordance covers all three cases:

- finished today → one click,
- finished on a known past date → pick it,
- finished, date unknown → toggle "don't remember".

### "I know the year but not the day"

Already handled by a full `DateOnly`: the user picks any day in that year and it buckets correctly into the yearly summary. Day-level fuzziness doesn't hurt yearly/monthly stats. A precision enum (`Day`/`Month`/`Year`/`Unknown`) is **out of scope** for v1 — add it only if the UI must later distinguish "exact" from "approximate."

---

## 8. Where the user edits it later

Reuse the existing update path — no new endpoint. Add an optional `FinishedReadingAt` to [`UpdateUserBookCommand`](../src/Legi.Library.Application/UserBooks/Commands/UpdateUserBook/UpdateUserBookCommand.cs). Surface an editable finish-date field on the **book details / UserBook panel**, shown only when `Status == Finished`, including the option to reset it to unknown.

---

## 9. UX per entry point

Unifying principle: **choosing "Finished" reveals a date control** — pre-filled per §7, one click to accept, trivial to change or clear.

- **Explore page** (book not in library): "Mark as Read" → popover with a date picker (default **empty**, "don't remember" available) → confirm. Behind it: add to library, then set Finished + date.
- **Book details page:** if not in library, same as Explore. If already in library, the status control → choosing Finished reveals the same date step (default **today** when coming from Reading).
- **Library** ([BookLifecycleActions.tsx](../web/legi-web/src/features/library/components/BookLifecycleActions.tsx)): today, selecting "Finished" fires immediately with no date. Change it so picking **Finished** swaps the menu into a small "Finished on [date] — Confirm" step (default today). Every other status keeps firing instantly.

Keep it inline — not a heavy modal on every finish.

---

## 10. Edge cases

1. **Timezone / where "today" is computed.** Server-side `DateTime.UtcNow.Date` can push an 11pm-Dec-31 finish (UTC−5) into the next year's bucket. Compute the default "today" on the **client** (local date) and send it. Protects yearly summaries.
2. **Back-dated finish is before `CreatedAt`.** A book added today but finished last year. Legitimate. **Do not** validate `FinishedReadingAt >= CreatedAt`. Only validate `<= today`.
3. **Future dates.** Reject finish dates in the future.
4. **`StartedReadingAt` vs `FinishedReadingAt` ordering.** Enforce `finished >= started` only when **both** are known (back-dated Explore reads have a null start).
5. **Un-finishing.** Reverting `Finished → Reading` must clear the date, or stale finish dates pollute stats.
6. **Re-reads count twice.** Two `UserBook` rows for the same title finished the same year count as 2. Usually desired for a reading tracker — decide consciously and document.
7. **Soft-deleted UserBooks.** A removed-but-finished book is hidden by the global query filter ([§2.5](LIBRARY-ARCHITECTURE-decisions.md)). If it should still count as "read in 2025," the stats query needs `IgnoreQueryFilters()` (or a dedicated read model). Easy to forget and silently wrong.
8. **Social feed dimension of back-dating.** Marking a book read last year still raises [`ReadingStatusChangedDomainEvent`](../src/Legi.Library.Domain/Events/ReadingStatusChangedDomainEvent.cs), which fans out a `BookFinished` activity. Decide: suppress the feed item when the finish date is materially in the past, **or** carry the real date in the event so the feed reads "finished on [date]." Deliberate choice, not an accident.
9. **Backfill.** Existing `Finished` rows have no date. Best-effort backfill with `UpdatedAt`, or leave `null` (treated as "date unknown / pre-feature"). Low-stakes pre-release.

---

## 11. Implementation plan by layer

### Domain — [UserBook.cs](../src/Legi.Library.Domain/Entities/UserBook.cs)

- Add `public DateOnly? FinishedReadingAt { get; private set; }` (and optionally `DateOnly? StartedReadingAt`).
- Change signature: `ChangeReadingStatus(ReadingStatus status, DateOnly? finishedOn = null)`:
  - on → `Finished`: `FinishedReadingAt = finishedOn;` (may be `null`),
  - on revert from `Finished`: `FinishedReadingAt = null;` (next to the existing `CurrentProgress = null`).
- `UpdateProgress` auto-finish passes `DateOnly.FromDateTime(DateTime.UtcNow)` explicitly.
- Add `SetFinishedReadingDate(DateOnly? date)` for later edits (incl. reset to unknown). Guard `Status == Finished`; reject future dates.
- (Optional) extend `ReadingStatusChangedDomainEvent` with the finish date for the Social feed (edge case 8).

### Application

- [`UpdateUserBookCommand`](../src/Legi.Library.Application/UserBooks/Commands/UpdateUserBook/UpdateUserBookCommand.cs): add `DateOnly? FinishedReadingAt = null`. Handler routes it through `ChangeReadingStatus` when the status is changing to `Finished`; if status is already `Finished` and only the date changed, call `SetFinishedReadingDate`.
- Validator: `FinishedReadingAt` not in the future.
- DTO: add `FinishedReadingAt` (and `StartedReadingAt`) to [`UserBookDto`](../src/Legi.Library.Application/Common/DTOs/UserBookDto.cs) and the relevant responses.
- **Explore one-click:** either a dedicated `AddAndFinishBookCommand` (add + Finished + date in one transaction — cleanest) **or** the frontend calling `AddBookToLibrary` then `UpdateUserBook` (cheapest). Recommend deferring the dedicated command to a later pass.

### Infrastructure

- Npgsql maps `DateOnly` → `date` natively. Map the new columns in `UserBookConfiguration`.
- Migration: two nullable `date` columns. No data loss, no rewrite.

### Frontend

- Date control in [BookLifecycleActions.tsx](../web/legi-web/src/features/library/components/BookLifecycleActions.tsx) when choosing **Finished** (default per §7, "don't remember" option).
- Explore / book-details "Mark as Read" flow with the same control.
- Editable finish-date field on the book details / UserBook panel (`Status == Finished`).
- Compute the default "today" on the **client** (edge case 1).
- Add `finishedReadingAt` to the relevant types/mappers.

### Read side (later)

- `GetReadingStatsQuery` grouping `Status == Finished` UserBooks by `FinishedReadingAt` year/month — remembering `IgnoreQueryFilters()` (edge case 7).

---

## 12. v1 scope

**Ship now:**
- `FinishedReadingAt` (nullable `DateOnly`) on `UserBook`.
- `ChangeReadingStatus(status, finishedOn?)` with clear-on-revert.
- `SetFinishedReadingDate` for later edits.
- `UpdateUserBookCommand.FinishedReadingAt` + validator (no future dates).
- DTO field + the three frontend entry points with context-dependent default and "don't remember" option.

**Defer (all additive, no migration pain):**
- `StartedReadingAt`.
- Dedicated `AddAndFinishBookCommand` (use two calls for v1).
- `GetReadingStatsQuery` and the stats UI.
- Date-precision enum.
- Social feed-event change for back-dated finishes.

The v1 slice makes finish dates **reliable** today and leaves every richer option open.
