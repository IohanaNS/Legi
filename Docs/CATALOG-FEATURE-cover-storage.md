# Book Cover Storage — Implementation Plan

> Status: **built (all 4 increments)**. Date: 2026-06-12; completed 2026-06-17.

## Implementation progress

- **Increment 1 — acquire-cover foundation DONE + verified (2026-06-17).**
  Infrastructure-only building blocks, not yet wired into import (behavior-neutral).
  - Application abstractions (`Legi.Catalog.Application/Common/Storage/`):
    `CoverImage` (validated bytes + content-type + extension), `IBookCoverSource`
    (fan-out fetch+validate → `CoverImage?`), `IBookCoverStorage` (upload → blob
    URL; delete-by-url), `IBookCoverAcquisition` (the one acquire-cover op:
    source→storage, never throws → blob URL or null).
  - Infrastructure (`Legi.Catalog.Infrastructure/Storage/`): `CatalogStorageOptions`
    (`Storage` section, separate `legi-covers` bucket, `/covers` public base),
    `CoverSourceOptions` (`CoverSource` section: host allowlist, per-fetch timeout,
    byte floor/ceiling, min dimension), `S3BookCoverStorage` (mirrors Social's
    `S3ObjectStorage`; keyed S3 client `catalog-covers`; key `covers/{isbn}/{guid}.ext`),
    `HttpBookCoverSource` (named HttpClient `book-cover-source`; SSRF allowlist +
    http(s)-only + ImageSharp decode/dimension/size validation; content-type &
    extension derived from the *decoded* format, not headers; all failures → null),
    `BookCoverAcquisition` (swallows MinIO/unexpected errors → null).
  - Wiring: csproj +AWSSDK.S3 +SixLabors.ImageSharp; DI registers options, keyed
    S3 client, named HttpClient, and the 3 services. Config: Catalog
    `appsettings.Development.json` `Storage` section (localhost MinIO); compose
    injects `Storage__*` into catalog-api + `depends_on minio`; minio-init creates
    + publicly-reads `legi-covers`; nginx proxies `/covers/ → minio/legi-covers/`.
  - Verified: full solution builds; 5 new `HttpBookCoverSourceTests` (fan-out picks
    first valid, skips too-small & falls through, rejects non-image 200, rejects
    non-allowlisted host *without* fetching, treats non-200 as no-cover) all green.
  - NOT yet done here: inline acquire in `BookImportService` (increment 2),
    durable retry job/worker (increment 3), manual upload endpoint (increment 4).

- **Increment 2 — inline acquire wired into import DONE + verified (2026-06-17).**
  `BookImportService` now owns covers instead of persisting external URLs.
  - Ctor gains `IBookCoverAcquisition`. New private `AcquireCoverAsync(preferred,
    isbn, ct)` builds the candidate fan-out `[Clean(preferred), resolver.ResolveByIsbn(isbn)]`
    and calls acquisition → owned blob URL or null. Replaces the old `ResolveCoverUrl`
    (which returned a raw/unvalidated URL — gone, per locked decision 2).
  - All three paths use it: `CreateManualAsync` (inline — user already waits on the
    provider lookup), `ImportCandidateAsync` (inside the external-search worker),
    `EnrichExistingBookAsync` (backfill a missing cover with an owned one; the
    existing `RaiseUpdatedEvent` republishes the blob URL to Library/Social snapshots).
  - Behavior: covers become `/covers/...` blob URLs; if acquisition returns null
    (no real cover / MinIO down / timeout) the book saves cover-less → frontend
    placeholder. Never blocks the book.
  - Tests: `CreateBookCommandHandlerTests` + `ProcessExternalBookSearchJobCommandHandlerTests`
    construct `BookImportService` with a mocked `IBookCoverAcquisition` (echo-first-
    candidate stand-in for "validate+store"); two cover assertions reworked to prove
    the preferred candidate flows in and the owned URL is persisted. Catalog
    Application 113 green; full solution builds; cover-source 5/5 green.
  - Honest caveat: the actual MinIO round-trip is exercised only through the mock —
    not yet against live MinIO. Fan-out is bounded by per-fetch timeout × 2 candidates
    (~6s worst case); a single overall cap (~5s) is a possible refinement.

- **Increment 3 — durable cover discovery DONE + verified (2026-06-17).**
  Bounded decaying retry for books imported cover-less.
  - Schema (3A): `CoverIngestionJobEntity` (BookId, Isbn, Status {Pending, Processing,
    Succeeded, Exhausted}, NoCoverAttempts, TransientFailures, NextRetryAt, timestamps,
    LastError) + config (partial unique index on BookId for active statuses + a
    (status, next_retry_at) index) + DbSet + migration `AddCoverIngestionJobs`. Applied
    to catalog-db (5433) and verified (columns, defaults, partial-unique index present).
  - `CoverRetryPolicy` (pure): budget = **5 confirmed no-cover probes**; decaying
    cadence 1h→6h→1d→3d→7d; transient (provider-unreachable) backoff 30m that does
    **not** consume budget (the "don't penalize outages" rule).
  - `ICoverIngestionQueue` (Application) + `CoverIngestionQueue` (Infra): adds one
    Pending job per book, deduped via the partial unique index (mirrors
    `ExternalBookSearchQueue`'s unique-violation swallow); first probe scheduled one
    cadence out (the inline acquire just failed).
  - `CoverIngestionWorker : BackgroundService`: claims due jobs (FOR UPDATE SKIP
    LOCKED), re-probes the provider (lookup throwing = transient → reschedule, no
    budget hit), runs acquire-cover; on a real cover updates the book +
    `RaiseUpdatedEvent` (snapshots get the blob URL) + backfills the work's default
    cover; on confirmed no-cover increments the budget → reschedule or Exhausted.
  - Enqueue hook: `BookImportService` enqueues discovery when a book ends up
    cover-less (both create paths + the enrich path), best-effort (never fails import).
  - Wiring: `ICoverIngestionQueue` scoped + worker hosted service in Catalog Infra DI;
    `BookImportService` ctor gains the queue (2 handler-test ctors updated).
  - Tests: 4 `CoverRetryPolicyTests` (cadence, saturation, exhaustion boundary,
    transient-doesn't-consume-budget) + 1 DB-backed `CoverIngestionQueueIntegrationTests`
    (one Pending job, scheduled ~1h out, repeat enqueue deduped). Catalog App 113,
    all 16 Catalog integration tests green, full solution builds.
  - Honest caveat: the worker's claim+discovery loop isn't directly automated-tested
    (it structurally mirrors the proven `ExternalBookSearchWorker`; the policy, queue,
    and partial-unique index are tested). A worker discovery test could be added.

- **Increment 4 — manual cover upload DONE + verified (2026-06-17).** The escape hatch.
  - Application: `IBookCoverImageProcessor` + `SetBookCoverCommand`/Handler/Response.
    Handler is **fill-only**: NotFound if the book's gone, **409 Conflict if a cover
    already exists** (no overwriting good covers), else store → persist blob URL +
    `RaiseUpdatedEvent` + backfill the work's default cover.
  - **Permission decision (deviates from the doc's "creator/admin"):** `Book.CreatedByUserId`
    is nullable and usually system/null for imported books (the actual cover-less long
    tail), so a creator-only rule would block fixing exactly those. Chose **any
    authenticated user may fill a *missing* cover, cannot overwrite**. Tradeoff: a light
    moderation surface (same as profile images); fine pre-production. Tighten later if needed.
  - Infra: `ImageSharpCoverProcessor` (decode, min-100px, downscale to ≤1000px wide
    preserving aspect, re-encode WebP → strips EXIF/neutralizes payloads). Registered.
  - Api: `POST /api/v1/catalog/books/{bookId}/cover` (multipart, `[Authorize]`, 5 MB
    cap, JPG/PNG/WebP allow-list); controller processes the file → `SetBookCoverCommand`.
  - Frontend: `catalogApi.uploadBookCover` + `useUploadBookCover` (optimistic cache
    patch + invalidate) + an "Add a cover" CTA on `BookDetailsPage` shown only when
    `!book.coverUrl` (hidden file input); i18n `bookDetails.cover.*` (en + pt-BR).
  - Verified: Catalog App 116 green (+3 `SetBookCoverCommandHandlerTests`: fills when
    missing, 409 when present, NotFound when gone), full solution builds, frontend builds.

## Feature complete

All four increments are built and verified. Covers are now owned (MinIO `legi-covers`),
validated-before-store, never block a book, self-heal via bounded discovery, and have a
manual escape hatch.

### Post-review fixes (2026-06-18)
- **Image bytes no longer flow through the mediator.** `SetBookCoverCommand` carries
  only the resulting URL; the controller now does the store (the `LoggingBehavior`
  `{@Request}` dump would otherwise log the whole byte array on every upload). The
  controller deletes the just-stored blob if the command rejects (404/410), so a
  rejected upload leaves no orphan. `IBookCoverStorage.StoreAsync` takes a generic
  `ownerKey` (ISBN on import, book id on manual upload).
- **Overall fan-out cap.** `CoverSourceOptions.OverallTimeoutSeconds` (5s) bounds the
  whole candidate fan-out, not just per-fetch — the inline manual-add path can't stack
  up to N×per-fetch.
- **Stale-`Processing` reclaim.** The cover worker's claim query also picks up
  `Processing` rows whose `updated_at` is older than 10 min, so a crashed worker can't
  permanently strand a job. (The analogous `ExternalBookSearchWorker` still has the
  original limitation — out of scope here.)

### Live end-to-end smoke test (2026-06-18)
Full Docker stack rebuilt (catalog/library/social/web) + `legi-covers` bucket created.
Verified through the web nginx + live RabbitMQ + MinIO:
- **Inline acquire → store → serve**: created a book with a real OL cover URL → response
  `coverUrl=/covers/{isbn}/{guid}.jpg`; blob present in `legi-covers`; **served 200 via
  nginx** `/covers/…`.
- **Cover-less path**: book with no real cover → `coverUrl=null` + a **Pending
  `cover_ingestion_jobs` row** scheduled +1h, `no_cover_attempts=0`; covered book → **no**
  job. Fan-out tried both the provider URL and the `?default=false` fallback.
- **Cross-context propagation (retires the standing caveat)**: Catalog `BookCreated`/
  `BookUpdated` → **Library `book_snapshots` AND Social `book_snapshots` both carry the
  owned cover URL** (and `work_id`), through the real outbox→RabbitMQ→inbox path.
- **Manual upload**: `POST /books/{id}/cover` with a JPEG → **200**, re-encoded to
  `/covers/{bookId}/{guid}.webp`, served via nginx; **re-upload → 409 fill-only**; the
  upload's `BookUpdated` propagated to the Library snapshot.
- **🐛 Bug caught + fixed**: the public URL was `/covers/covers/…` — the S3 key repeated
  the `covers/` segment the bucket+base-path already imply. Fixed `S3BookCoverStorage`
  (key is now `{ownerKey}/{guid}.ext`) + added `S3BookCoverStorageTests` regression guard.
  Catalog integration suite now 17 green. *(This one-line fix + test is uncommitted on
  top of `bcad5e5`.)*

### Remaining refinements (non-blocking)
A worker discovery automated test, live-MinIO round-trip test, coverage health metric
(Plan B §5), and tightening the upload permission model (currently any authed user may
fill a missing cover).

---

> Original plan (approved 2026-06-12) follows.

Move book covers from fragile third-party hotlinked URLs to **self-owned blobs in
MinIO**, the same way profile pictures and banners are already stored. The goal is
to stop depending on the external cover APIs at *display* time while keeping the
catalog complete (cover-less books still save) and the source swappable.

## Motivation

- Today `Book.CoverUrl` holds an external URL — either a real provider cover
  (OpenLibrary / Google Books) or a synthetic ISBN-addressable fallback
  (`covers.openlibrary.org/b/isbn/{isbn}-L.jpg?default=false`), resolved in
  [`OpenLibraryCoverUrlResolver`](../src/Legi.Catalog.Infrastructure/ExternalServices/OpenLibrary/OpenLibraryCoverUrlResolver.cs).
- These are open APIs. They can rate-limit, change format, or disappear. Anything
  hotlinked breaks with them.
- Profile pics / banners already live in MinIO (Social context). Covers should
  follow the same ownership model.

## Core model

**"Real cover" = a URL that, when fetched, yields a genuine image** (HTTP 200,
`Content-Type: image/*`, decodes, meets a minimum size). That single runtime check
collapses the real-vs-synthetic distinction — a synthetic ISBN URL that 404s or
returns a tiny placeholder is simply *not* a real cover.

- **`Book.CoverUrl`** = the **MinIO blob URL** once we own a real cover, otherwise
  **`null`**.
- **We only ever store real covers in blob.** Never store an invalid/placeholder
  image.
- **A cover-less book is a valid, complete book.** It saves normally and shows a
  frontend placeholder. "No cover" is a legitimate permanent state, not a failure.

## Locked decisions

1. **Never block a book on its cover.** A valid cover is *not* a precondition for
   saving a book. Book identity is ISBN + title + authors; the cover is cosmetic.
   Making it a domain invariant would couple display to the domain and reject
   legitimate (obscure / old / foreign) books. Cover work is always best-effort
   and must never fail or block an import.
2. **Validate before returning — no unvalidated URLs ever reach the user.** We do
   not hand the frontend "whatever URL the first provider gave." We fan out across
   providers, validate by fetching, and return either an owned blob URL or `null`.
3. **Validating ≈ downloading**, so we store inline. Once we've fetched the bytes
   to validate them, we've done ~90% of the work — so we upload to MinIO in the
   same step and return the blob URL. This removes the "optimistic external URL,
   swap later" two-phase dance on the synchronous path.
4. **Store only real covers; keep trying (cheaply) for the rest.** Cover-less
   books stay `null` and are eligible for a bounded background retry + free
   opportunistic re-checks (see Step 4).
5. **Catalog gets its own blob storage**, pointed at the same MinIO instance as
   Social but a **separate bucket** (e.g. `legi-covers`). Reuse the infra, not the
   code across bounded contexts.

## The "acquire-cover" operation

One operation, used by every path. The only difference between paths is whether a
user request waits on it.

```
acquire-cover(isbn, candidateUrls):
  for each source in [provider A, provider B, ISBN endpoint]:
     bytes = fetch(source.coverUrl)        # this IS the validation
     if valid(bytes):                      # see "valid" below
        url = upload(bytes -> MinIO legi-covers)
        return url                         # owned blob URL
  return null                              # -> placeholder + discovery job
```

**"valid"** =
- HTTP 200
- `Content-Type: image/*`
- decodes as a real image
- **minimum dimensions** (e.g. >= 100px) — catches OpenLibrary's tiny/blank
  placeholder
- a sane byte-size floor/ceiling

## Flow

1. **Import book** (`CreateManualAsync` / `ImportCandidateAsync` in
   [`BookImportService`](../src/Legi.Catalog.Application/Books/BookImportService.cs)).
2. **Acquire-cover**:
   - **Manual single add** (`CreateBookCommand`): run **inline**. The user is
     deliberately adding one book and already waits on `GetByIsbnAsync`; ~1–3s is
     acceptable.
   - **Bulk import** (`ImportCandidateAsync`): run **inside the existing background
     worker**. Same operation, but it's not blocking a user, so the per-job
     latency is tolerable. Do **not** make a user-facing request wait on N books ×
     M providers.
3. **Return the book** with the **blob URL** (valid + owned) or **`null`**. No
   frontend change needed — it just reads `CoverUrl`; `null` → placeholder.
4. **`null` → bounded discovery** (see below).

### Guards so synchronous acquire-cover never bites

- **Per-provider timeout** (~2–3s) and a **total cap** (~5s) on the whole fan-out.
  Hit the cap → return `null`.
- **MinIO failure → never fail the book.** Catch, return `null`, save the book.
- **Background safety net:** whenever the inline attempt ends `null` (no cover, or
  timeout, or MinIO hiccup), enqueue the durable discovery job so a transient
  failure isn't permanent.

## Step 4 — when the cover doesn't come (yet, or ever)

A real chunk of the catalog will never have a cover in any API. A forever-poll
loop is wrong (doesn't scale → ~3,300 probes/day per 100k cover-less books →
rate-limit risk). Policy, layered:

1. **Bounded active retry window.** Decaying cadence — roughly 1h → 6h → 1d → 1w,
   a handful of checks over ~30–60 days. Catches the common case: a freshly
   cataloged book that the provider adds a cover for shortly after.
2. **Then go quiet → status `Exhausted`.** Stop scheduled polling; the job drains
   out of the table. Zero perpetual cost. Placeholder is the resting state.
3. **Free opportunistic re-checks on natural touchpoints.** No poller needed to
   catch late covers because the book gets touched anyway:
   - `EnrichExistingBookAsync` already re-runs (and backfills a missing cover)
     whenever the book reappears in an external search
     ([BookImportService.cs:143](../src/Legi.Catalog.Application/Books/BookImportService.cs#L143)).
   - Optionally also re-attempt when a user adds it to their library / opens its
     detail page — i.e. look when a human would actually see it.
   - A book nobody ever touches doesn't get a cover, which is correct.
4. **Manual upload — the definitive escape hatch.** Let the book's creator (or an
   admin) **upload a cover** straight to MinIO, exactly like profile pics. The only
   guaranteed answer for the genuinely cover-less long tail; mostly reuse of the
   profile-image upload path.

### Critical nuance — don't penalize outages

The give-up budget is **"N confirmed no-cover results," not "N attempts."**

- **Transient failure** (provider down / 429 / timeout): reschedule with backoff,
  **does not** count toward `Exhausted`. Otherwise one outage burns every book's
  budget and they all give up prematurely.
- **Confirmed no-cover** (successful probe that returned nothing): counts down
  toward `Exhausted`.

## Plan B — provider resilience

The open APIs are the weak point. Strategy: **insulate what you own, fan out what
you don't, keep the source swappable.**

1. **Multi-provider fan-out behind the interface.** `IBookDataProvider` already
   has both OpenLibrary and Google Books mappers. Cover sourcing tries A → B →
   ISBN endpoint and takes the first real image. One provider dying shifts load;
   coverage degrades, nothing breaks.
2. **Owned covers are immune.** Once a cover is in MinIO, the provider can vanish
   and every already-imported book is fine. Exposure is only ever *new cover-less
   books*, and it shrinks over time. This is the real Plan B.
3. **Provider outage never fails import** — preserve this invariant hard.
4. **Circuit breaker + rate limiting** (Polly) around provider calls so a flaky /
   429-ing provider is backed off as a whole and the chain trips to the next
   source.
5. **Coverage as a monitored signal.** Track `% books with an owned cover` and
   per-provider error rate (reuse the health-check / `MessagingMetrics` infra). A
   climbing cover-less count or spiking error rate *is* the early warning that an
   API broke — wire it to a health check.
6. **Swapping a source is a one-class change** — a new provider is a config entry +
   one mapper, no pipeline changes. That's what makes Plan B executable.

### Honest caveat

The *discovery* path can never be fully third-party-free — sourcing a cover you
don't have means asking someone. What we buy: owned covers are insulated, the
source is swappable, and an outage degrades to "placeholder + retry" instead of
breaking.

## Downstream propagation — don't miss this

`Book.CoverUrl` flows to Library `BookSnapshot` and Social `ContentSnapshot` via
the `BookUpdated` integration event. So whenever the cover changes (inline store,
or a later discovery/backfill swap), the book **must** `RaiseUpdatedEvent()` so the
outbox republishes and downstream snapshots get the blob URL — otherwise
Library/Social keep showing a stale URL. `EnrichExistingBookAsync` already does
exactly this pattern
([BookImportService.cs:184](../src/Legi.Catalog.Application/Books/BookImportService.cs#L184)).

## Coverless UX — don't let it look like a bug

A genuinely cover-less book is a valid, permanent state — but a *missing* cover
currently renders as a broken/empty box, which reads as a bug. Fix the
*perception*, independent of (and ahead of) all the storage work above:

- **Generated placeholder.** Typeset the title + author onto a deterministic
  colored card (color hashed from ISBN/title), spine-style — the StoryGraph /
  Bookshop approach. A cover-less book then looks *intentional*, never broken. This
  is purely frontend and can ship first.
- **"Add a cover" CTA.** Surface the manual-upload escape hatch (above) on the
  placeholder, so cover-less becomes a soft, fixable state rather than an error.

This is orthogonal to ownership/discovery — ship it early to kill the
"looks-broken" perception while the rest lands. (Referenced from
[CATALOG-FEATURE-editions.md](./CATALOG-FEATURE-editions.md), where covers attach
at the **Edition** level.)

## Security

`CreateManualAsync` accepts `input.CoverUrl` **from the user** — so the fetcher is
an SSRF surface. Guard it:

- **Allowlist hosts** (covers.openlibrary.org, books.google.com, …).
- Enforce `Content-Type: image/*`, a size cap, and a timeout.
- No redirects to internal addresses.

## Components to build

- **`CoverIngestionJob`** entity + EF config + migration — durable retry rows with
  decaying `NextRetryAt`, separate transient-failure vs confirmed-no-cover
  counters, and an `Exhausted` terminal status. Mirror
  [`ExternalBookSearchJobEntity`](../src/Legi.Catalog.Infrastructure/Persistence/Entities/ExternalBookSearchJobEntity.cs).
- **`CoverIngestionWorker : BackgroundService`** — clone of
  [`ExternalBookSearchWorker`](../src/Legi.Catalog.Infrastructure/ExternalServices/ExternalBookSearchWorker.cs)
  (same `FOR UPDATE SKIP LOCKED` claim + backoff).
- **`IBookCoverSource`** — multi-provider fan-out that fetches + validates and
  returns bytes (or null). SSRF / size / content-type / dimension guards live here.
- **`IBookCoverStorage`** — upload bytes → MinIO `legi-covers`, return public URL.
- **Enqueue hook** in `BookImportService` (manual = inline acquire; bulk + `null`
  = enqueue discovery job), in the same transaction as the book insert.
- **Read-time resolution** — DTO mapping: `CoverUrl ?? resolveIsbnFallback(isbn)`
  so cover-less books still get a live (self-healing) *display* URL while owned.
- **Manual cover upload** endpoint + frontend (reuse profile-image upload path).
- **Coverage health metric** + per-provider error-rate signal.

## Open questions / deferred

- Exact retry cadence + the confirmed-no-cover budget (N) before `Exhausted`.
- Whether to also re-check on "added to library" / "detail page view", or rely
  solely on the external-search enrich path.
- Migration of existing rows: leave legacy external URLs as-is and let natural
  touchpoints upgrade them, or run a one-off backfill job.
</content>
</invoke>
