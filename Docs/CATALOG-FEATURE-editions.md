# Work / Edition Modeling — Design & Plan

> Status: design, not yet building. Date: 2026-06-12.
> Companion to [CATALOG-FEATURE-cover-storage.md](./CATALOG-FEATURE-cover-storage.md).

Split the catalog's single `Book` into a two-level bibliographic model — **`Work`**
(the abstract book) and **`Edition`** (a specific manifestation) — so users can find
and track *the exact version they're reading*, and so ratings/reviews aggregate at
the work where readers expect them.

## Why now (and why this isn't premature)

- **The version gap is the biggest functional hole.** External APIs return "the
  book in general," not the specific edition a user is holding — different ISBN,
  cover, page count, publisher, translation. Today `Book` is keyed by ISBN, so it's
  *conceptually already an edition*, but it's sourced and displayed like a work.
  That mismatch is exactly why covers / page counts feel wrong and "my version"
  can't be found.
- **Work-level aggregation is a real domain requirement, not speculation.** Readers
  rate *Dune*, not the 2015 Penguin printing (Goodreads/StoryGraph both aggregate
  at the work). So this is the correct *and* the needed model.
- **No production, no users yet.** The dominant cost of this change — migrating live
  `UserBook`s / reviews / feed items / snapshots and not breaking anyone — is
  **~zero today**. Doing it greenfield is dramatically cheaper than retrofitting a
  Work/Edition split *after* everything points at flat `Book`s. Doing it later is
  strictly worse than doing it now.
- **It powers the flywheel.** Every time a user adds/confirms *their* edition and
  keeps/uploads a cover, the DB captures an edition the providers may not index
  well. Over time the catalog answers "book + versions + cover" without a provider
  call — providers become a cold-start seed, not a runtime dependency.

## Decision

**Do the full two-level model now** (previously "Option A"), with discipline:
**get the bones structurally correct, keep behavior thin.** Model `Work` + `Edition`
as real entities with correct FKs threaded through all contexts so we never
retrofit — but defer fancy edition tooling (no edition-merge UI, translation
graphs, "combine works" admin flows yet). A `Work` is a lean aggregate: identity +
aggregated read fields.

## Provider reality (validated against the code)

- **OpenLibrary exposes the work key for free.**
  [`OpenLibraryEdition.Works`](../src/Legi.Catalog.Infrastructure/ExternalServices/OpenLibrary/OpenLibraryEdition.cs#L47-L53)
  already deserializes `works[].key` (`/works/OL…W`); search docs' `key` *is* the
  work key. It's currently dropped — used only to fetch a description fallback,
  never surfaced into `ExternalBookData` / `ExternalBookCandidate`.
- **Google Books has no work concept.** Volumes *are* editions (`volume.Id` is a
  volume id); no native grouping.
- **Therefore:** add a `WorkKey` to the provider DTOs
  ([`IBookDataProvider`](../src/Legi.Catalog.Application/Common/Interfaces/IBookDataProvider.cs)).
  Populate from OpenLibrary's `works[0].key`; for Google (and OL gaps) fall back to
  a synthesized key — normalized `title + primary-author` hash — and/or
  cross-reference by ISBN.

## Identity & grouping

- **Edition identity = ISBN** (as `Book` is keyed today). Each printing is one
  edition.
- **Work identity = `WorkKey`**: the provider work key (OpenLibrary) when present,
  else a deterministic synthesized key from normalized title + primary author.
- **Linking:** an edition belongs to exactly one work. On import, resolve/create
  the work by `WorkKey`, then attach the edition. Two editions sharing a `WorkKey`
  are siblings under one work.

### Merge policy — LOCKED: bias to under-merge (2026-06-12)

Over-merge **corrupts** (unrelated books collapse → wrong aggregate rating shown);
under-merge only **fragments** (duplicate Works, recoverable via merge). So when in
doubt, do **not** merge. Linking precedence, authoritative-signal first:

1. **OpenLibrary work key** directly (OL-sourced editions) — trust it.
2. **ISBN → OL work lookup** — resolve the authoritative work key by ISBN for
   Google-sourced (and any ISBN-bearing) editions instead of guessing. The main
   lever that keeps Google books on real work keys.
3. **Synthesized key** only as last resort — normalized `title + primary author`
   (lowercase, strip diacritics/punctuation, normalize author "Last, First" →
   "First Last" + initials). **Always include the author** (never title alone);
   **keep subtitles** (dropping them over-merges series volumes).

Keep the work-link **reassignable**. Auto-reconciliation (a synthesized Work later
discovering its OL key and merging) + merge tooling are **deferred** — accept some
duplicate Works initially; the mutable link makes later correction safe.

## The attachment map (the spine)

The rule: **social/aggregate concerns → Work; physical/reading concerns → Edition;
`UserBook` is the join carrying both.**

| Concept | Level | Why |
|---|---|---|
| Title, authors, synopsis, tags | **Work** | The abstract book |
| Aggregate rating, reviews count | **Work** | Readers rate the work, not a printing |
| ISBN, page count, publisher, language, format, year | **Edition** | A specific manifestation |
| **Cover** | **Edition** | Edition-specific; Work carries a denormalized default |
| Default/representative cover | **Work** (denormalized from an edition) | Search/list/feed need *a* cover |
| `UserBook` | **WorkId (required) + EditionId (nullable)** | Edition = the copy they read (optional); Work = identity for aggregation |
| Rating / review (the act) | **Work** (review may *note* the edition) | A review of *Dune* shows on every edition |
| Reading progress, page-based completion | **Edition** | Needs *that* edition's page count |
| List entries | **Work** | Lists are edition-agnostic; display a representative cover |

> Cover storage ([companion doc](./CATALOG-FEATURE-cover-storage.md)) is unchanged,
> just retargeted: **covers live on `Edition`**, and `Work` holds a denormalized
> default cover for edition-agnostic display.

## Per-context impact

### Catalog (the split itself)
- `Book` → `Work` aggregate (title, authors, synopsis, tags, aggregate rating,
  reviews count, `WorkKey`, denormalized default cover) + `Edition` entity (ISBN,
  cover, page count, publisher, language, format, year, FK → Work).
- **Search returns works** (with a representative edition for the cover); the detail
  page shows the work + its **Editions (N)** so the user can pick/add theirs.
- Rating recompute aggregates editions' `UserBook` ratings up to the **Work**.
- Import (`BookImportService`) resolves/creates the Work by `WorkKey`, then attaches
  the Edition. `EnrichExistingBookAsync` becomes "enrich edition + maybe work."

### Library
- `UserBook` gains a **dual reference**: `WorkId` (required, identity for
  aggregation) + `EditionId` (**nullable**, the copy they read). **LOCKED
  (2026-06-12): edition optional.** Add-to-library defaults to **work-level**
  (frictionless, never a dead-end); user refines to a specific edition later
  (progressive disclosure). Auto-attach a representative edition when obvious, but
  never require it. Re-add of a *different edition* of the same work is still a new
  reading cycle.
- Page-based `Progress` reads **`Edition.PageCount`** when an edition is set; with
  no edition, fall back to percentage (existing `ProgressForm` behavior). UI nudges
  "pick your edition to track by page."
- `BookSnapshot` splits/extends into work-level + edition-level fields (or
  `WorkSnapshot` + `EditionSnapshot`) — projected from Catalog events.
- Lists reference **Work**; display the work's default cover.

### Social
- `ContentSnapshot` for a book → **work-level** (reviews/feed are about the work);
  cover shown comes from the work default (or the user's edition).
- Feed "X is reading <book>" / "rated <book>" → **Work** identity, edition cover for
  display.
- `GetBookReviewsQuery` / reviews-by-book → **reviews-by-work**.
- `FeedItem.BookId` → carries the **WorkId** (rename/clarify).

### Contracts / Messaging
- Integration events that carry `BookId` (`BookCreated`, `BookUpdated`,
  `BookAddedToLibrary`, `ReadingStatusChanged`, `UserBookRated`, `ReviewCreated`,
  `ReadingPostCreated/Deleted`) need to carry **both `WorkId` and `EditionId`** (or
  be split), so downstream projectors can attach data at the right level.
- This is the widest blast radius — every cross-context consumer touches these.

## UX: coverless must not look like a bug (Problem 1)

Independent of the model split, ship this early — it removes the "looks broken"
perception now. See the placeholder section in
[CATALOG-FEATURE-cover-storage.md](./CATALOG-FEATURE-cover-storage.md):

- **Generated placeholder** — title + author typeset on a deterministic colored
  card (color hashed from ISBN/title), spine-style. A coverless edition then looks
  *intentional*, not failed.
- **"Add a cover" CTA** — the manual-upload escape hatch (reuses the profile-image
  upload path). Coverless becomes a soft, fixable state.

## Implementation progress

- **DONE (2026-06-16) — WorkKey foundation:** `WorkKey` value object (FromProvider /
  Synthesize / Resolve), persisted on `Book` (`work_key` col + index), surfaced
  through provider DTOs + OL mappers, resolved on import. Migration applied;
  legacy rows backfilled with unique `syn:legacy-{id}` keys.
- **DONE (2026-06-16) — Work aggregate + Book→Work link:** lean `Work` entity
  (Id, WorkKey unique, Title, DefaultCoverUrl), `IWorkRepository` /
  `WorkRepository`, `Book.WorkId` FK. `BookImportService.AssignWorkAsync`
  resolves-or-creates the work by the edition's `WorkKey` on both create paths
  (reusing `Book.WorkKey`, no double-compute). Migration `SplitWorkFromBook`
  applied + integrity-verified (5800 books → 5800 works, 0 null/orphan).
- **DONE (2026-06-16) — editions on book detail:** `GetBookDetails` now returns
  `WorkId` + `Editions[]` (the work's editions, oldest-first) via
  `IBookReadRepository.GetEditionsByWorkIdAsync` + `EditionSummaryDto`. Additive —
  search and ratings untouched. NOTE: all legacy books are singleton works, so the
  list shows 1 edition until books sharing a real work key are imported. Frontend
  not yet wired (defer to read rework; only render when >1 edition).
- **DONE (2026-06-16) — search collapses editions to one representative per work:**
  `BookReadRepository.SearchAsync` appends a self-referencing "representative per
  work" predicate (most-rated edition, tie-break oldest, chosen among filtered
  editions). Keyed by the representative edition → **no frontend change**, existing
  `/books/{id}` navigation intact. Verified: 5800 reps = 5800 works on real data;
  a real-DB integration test (`SearchDedupIntegrationTests`) confirms 2 editions →
  1 result = most-rated edition (validates EF translation).
  - **Not yet:** rating/reviews still on `Book`; authors still on `Book`; search
    sort/display still uses the representative edition's own rating (no work-level
    aggregate); `Book` not yet renamed to `Edition`; no dedicated work page;
    Contracts events still carry only `BookId`.

- **ORDERING FINDING (2026-06-16):** work-level **rating aggregation is gated behind
  Library.** The recompute stages per-(book,user) rows in the change tracker and
  recomputes in-memory including the un-saved change; aggregating across a work's
  editions is risky there AND semantically premature — correct per-user-per-work
  dedup needs Library's `UserBook` to carry `WorkId` first. So the next major step
  is **Phase 2 (Library + Contracts)**, not more Catalog read-rework.

## Phasing

0. **Coverless UX** (generated placeholder + upload CTA) — orthogonal, immediate.
1. **Catalog split** — `Work` + `Edition` entities, configs, `WorkKey` resolution
   in providers + import; search-returns-works; detail shows editions.
2. **Library rewire** — `UserBook` dual-ref, progress off `Edition.PageCount`,
   snapshot split.
3. **Contracts + Social rewire** — events carry `WorkId` + `EditionId`; Social
   snapshots/feed/reviews go work-level.
4. **Cover storage** ([companion](./CATALOG-FEATURE-cover-storage.md)) — targeted at
   `Edition`, work default denormalized.

## Open questions / deferred

- `BookSnapshot` — extend in place vs split into `WorkSnapshot` + `EditionSnapshot`.
- Whether to split the `BookId`-carrying integration events or widen them in place.
- Edition-merge / reassign tooling — deferred, but keep the work-link mutable.

**Resolved 2026-06-12** (see Merge policy + Library sections above): WorkKey merge
tolerance → **bias to under-merge**, OL/ISBN-first linking. UserBook edition
requirement → **edition optional, work-level default**.
</content>
