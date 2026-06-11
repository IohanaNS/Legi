# Create Lists + List Detail — Implementation Plan (v2)

> Status: approved, building. Date: 2026-06-10.

Users can create custom book lists (name, optional description, public/private,
selected books via Catalog search), view list details, edit their own lists, and
interact with others' **public** lists via like, comment, and a list-specific
follow.

## Key findings (validated against the code)

- **Decoupling `UserListItem` from `UserBook` is mandatory.** Today
  `AddBookToListCommandHandler` rejects any book the user doesn't own in their
  library. Lists must reference `bookId` and validate against the existing
  `BookSnapshot` (Catalog projection already present in Library).
- **List likes/comments already require a `ContentSnapshot`** (both
  `LikeContentCommandHandler` and `CreateCommentCommandHandler` throw NotFound if
  missing), and **no list snapshot is ever created today**, so the existing
  `ListInteractionsController` is effectively dead. Comment in code confirms
  "Lists are non-interactable in v1 (Option A)".
- **Safe visibility design:** *snapshot existence == list is public &
  interactable.* Social creates the snapshot when a list is public; deletes it
  when it goes private or is deleted. The **existing generic like/comment
  handlers then auto-reject private lists (NotFound) with zero new branching in
  shared code.**
- **Library list like/comment counters stay 0 (unchanged).** The detail page
  reads live counts from Social. Cards show only `booksCount`, so we don't wire
  Library counters for lists — less surface, less drift.
- Frontend `socialApi` **already** has generic `like`/`unlike`/`getComments`/
  `addComment` for the `"lists"` resource. Only list-follow and a social-state
  fetch are missing.

## Locked decisions

- Public/private is a real feature. **Create defaults to public** so the AC
  interaction flow works; with a toggle.
- Private lists are visible/interactable to the **owner only**, enforced in
  Library (`IUserListVisibilityPolicy`, exists) and in Social via
  snapshot-existence.
- List follow is a **new aggregate** (`ListFollow`), separate from user-follow;
  stored now, not surfaced on profile yet.
- Adding a book to a list does **not** add it to the library.
- **Card design (per mockup):** 2×2 cover mosaic, name + visibility badge at top,
  2-line description, footer `N livros` + owner-only **edit + delete** icons.
  The redundant footer visibility glyph from the mockup is dropped (badge at top
  is the single source of the public/private indicator).

## Phase 0 — Contracts (`Legi.Contracts/Library/`)

- `UserListCreatedIntegrationEvent(ListId, OwnerId, Name, IsPublic, OccurredOn)`
- `UserListUpdatedIntegrationEvent(ListId, OwnerId, Name, IsPublic, OccurredOn)`
- `UserListDeletedIntegrationEvent(ListId, OwnerId, OccurredOn)`

## Phase 1 — Library domain + migration

- `UserListItem`: `UserBookId` → `BookId`; `Create(bookId, order)`.
- `UserList`:
  - `AddBook`/`RemoveBook`/`RemoveBookIfExists`/`ReorderBooks` operate on `bookId`.
  - New `SyncBooks(IReadOnlyList<Guid> bookIdsInOrder)` — reconciles to target set
    (preserve `AddedAt` for retained items, assign `Order` by position, reject
    duplicates). Used by create + update.
  - `Create(...)` raises `UserListCreatedDomainEvent`; `UpdateDetails(...)` raises
    `UserListUpdatedDomainEvent` (both carry `IsPublic`). `Delete()` keeps
    `UserListDeletedDomainEvent`.
  - Trim `Name` in `Create` too.
- New domain events `UserListCreatedDomainEvent`, `UserListUpdatedDomainEvent`.
- `UserListItemConfiguration`: map `book_id`; replace `user_book_id` index with
  unique `(user_list_id, book_id)`.
- Migration (additive + backfill, safe):
  1. add `book_id uuid null`
  2. backfill `UPDATE user_list_items i SET book_id = ub.book_id FROM user_books ub WHERE i.user_book_id = ub.id`
  3. set NOT NULL
  4. drop FK + index + `user_book_id` column
  5. unique `(user_list_id, book_id)`
- `UserDeletedIntegrationEventHandler` (Library): list cleanup re-points to
  `bookId`. **Decision: lists keep the book when a UserBook is soft-deleted**
  (lists are now library-independent).

## Phase 2 — Library application

- `CreateUserListCommand` + handler: add `IReadOnlyList<Guid> BookIds`; validate
  each `BookSnapshot` exists; `SyncBooks`. **Fix `listId = Guid.Empty` response.**
- `UpdateUserListCommand` + handler: add `BookIds`; ownership check; `UpdateDetails`
  + `SyncBooks` in one transaction.
- `AddBookToList`/`RemoveBookFromList`: retarget to `bookId`, validate snapshot,
  fix responses. Not used by create/edit flow but kept.
- DTOs:
  - `UserListBookDto` → `(BookId, Order, BookSnapshotDto Book, AddedAt)` (drop
    `Status`/`RatingStars`).
  - `UserListSummaryDto` → add `PreviewBooks: BookSnapshotDto[]` (≤4) and `OwnerId`.
  - `UserListDetailDto` → add `OwnerId`/`IsOwner`.
- `UserListReadRepository`: `GetListBooksAsync` joins `UserListItems → BookSnapshots`
  on `book_id`; summary queries subquery first 4 covers.
- Domain→integration handlers (copy `ReviewCreatedDomainEventHandler`):
  `UserListCreated/Updated/DeletedDomainEventHandler` publish via `IEventBus`.

## Phase 3 — Social

- Integration event handlers (copy `ReviewCreatedIntegrationEventHandler`,
  `StageAddOrUpdate`, **no SaveChanges**):
  - `UserListCreated/UpdatedIntegrationEventHandler`: public → stage add/update
    `ContentSnapshot(List, listId, owner..., contentPreview: Name)`; not public →
    stage delete. Owner resolved from `UserProfile` (throw-to-redeliver).
  - `UserListDeletedIntegrationEventHandler`: delete snapshot + likes + comments +
    follows for `(List, listId)`.
- `ListFollow` aggregate `(UserId, ListId)` unique; reject owner; repo +
  configuration + Social migration.
- Commands `FollowListCommand`/`UnfollowListCommand` — require existing list
  snapshot (public), reject owner, idempotent unfollow.
- Query `GetListSocialStateQuery` → `GET /api/v1/social/lists/{listId}`:
  `{ likesCount, commentsCount, followsCount, isLikedByMe, isFollowedByMe, isOwner }`,
  counts read live.
- Controller: extend `ListInteractionsController` with `POST/DELETE /follows` and
  `GET /lists/{listId}`.
- Repos: `DeleteByTargetAsync` on Like/Comment; `ListFollowRepository`.
- DI + messaging consumer wiring.

## Phase 4 — Frontend

- API: `libraryApi.{createList,updateList,deleteList,getListDetail,getListBooks}`;
  `socialApi.{followList,unfollowList,getListSocialState}`. Extend types
  (`previewBooks`, `ownerId`, `isOwner`).
- Routes: `lists/new`, `lists/:listId`, `lists/:listId/edit`.
- Pages: `ListsPage` (+ "Nova lista" button, 3-col grid, cards navigate to detail);
  `ListCard` (2×2 mosaic + fallback tile for null covers; owner-only edit/delete
  footer icons); `ListEditorPage` (create/edit, reuse `useSearchBooks`, selected
  books w/ remove + duplicate prevention); `ListDetailPage` (title/desc/visibility/
  books/social — non-owner like/comment/follow; owner edit/delete, no follow).
- Hooks: `useListDetail`, `useListBooks`, `useCreateList`, `useUpdateList`,
  `useDeleteList`, `useListSocial` + query keys. Optimistic like/follow.
- i18n keys in all locales.

## Phase 5 — Tests

- Library domain: `SyncBooks` (add/remove/reorder/duplicate/AddedAt preserved);
  Create/UpdateDetails raise events w/ `IsPublic`; whitespace name rejected.
- Library application: blank name fails; optional description; duplicate `bookIds`
  fail; missing snapshot → NotFound; non-owner update → Forbidden; private hidden
  from non-owner; create returns real `listId`.
- Social application: snapshot created on public / deleted on private toggle;
  follow create/idempotent/owner-rejected/unfollow; like/comment on private list →
  NotFound; delete event cleans snapshot+likes+comments+follows.
- Integration: migration backfill — existing items resolve to `book_id`, reload
  with covers.
- Frontend: `yarn build` + `yarn lint`; smoke for create/search/duplicate/detail/
  edit/like/comment/follow, owner vs non-owner UI.

## Sequencing / safety

- Build order: Contracts → Library (domain+migration+app+events) → Social
  (consumers+follow+state) → DI/messaging wiring → frontend → tests. Compiles at
  each phase boundary.
- Migration additive-then-drop with backfill — no data loss. Test backfill on a
  seeded DB before dropping `user_book_id`.
- Visibility enforced server-side twice (Library + Social snapshot-existence).
  Eventual-consistency window on public→private toggle is acceptable.
- No change to shared like/comment handlers — snapshot-existence guard does the
  gating.
