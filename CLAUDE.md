# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Building and Running

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Legi.Identity.Api/Legi.Identity.Api.csproj
dotnet build src/Legi.Catalog.Api/Legi.Catalog.Api.csproj
dotnet build src/Legi.Library.Api/Legi.Library.Api.csproj
dotnet build src/Legi.Social.Api/Legi.Social.Api.csproj

# Run the Identity API
dotnet run --project src/Legi.Identity.Api/Legi.Identity.Api.csproj

# Run the Catalog API
dotnet run --project src/Legi.Catalog.Api/Legi.Catalog.Api.csproj

# Run the Library API
dotnet run --project src/Legi.Library.Api/Legi.Library.Api.csproj

# Run the Social API
dotnet run --project src/Legi.Social.Api/Legi.Social.Api.csproj

# Start all services (4 databases + RabbitMQ + 4 APIs + web frontend)
docker compose up -d --build

# Stop all services
docker-compose down

# Rebuild and start only the web frontend
docker-compose up -d --build web

# Run the web frontend locally (dev mode)
cd web/legi-web && npm run dev
```

### Testing

```bash
# Run all tests with the shared coverage settings (line 75% / branch 65%)
dotnet test Legi.sln --settings tests/.runsettings

# Run all tests (integration tests auto-skip when their DB env vars are unset)
dotnet test

# Run tests for a specific project
dotnet test tests/Legi.Identity.Domain.Tests/Legi.Identity.Domain.Tests.csproj
dotnet test tests/Legi.Identity.Application.Tests/Legi.Identity.Application.Tests.csproj

# Run a single test class
dotnet test --filter "FullyQualifiedName~ClassName"

# Run tests with coverage
dotnet test /p:CollectCoverage=true
```

#### Integration tests (skippable)

The `*.Integration.Tests` projects use `Xunit.SkippableFact` and only run when pointed at a Postgres instance via env vars (otherwise they skip). Start the infra and export the connection strings:

```bash
docker compose up -d catalog-db library-db social-db rabbitmq
export CATALOG_TEST_DB="Host=localhost;Port=5433;Database=legi_catalog_dev;Username=postgres;Password=postgres"
export LIBRARY_TEST_DB="Host=localhost;Port=5434;Database=legi_library_dev;Username=postgres;Password=postgres"
export SOCIAL_TEST_DB="Host=localhost;Port=5435;Database=legi_social_dev;Username=postgres;Password=postgres"
dotnet test Legi.sln --settings tests/.runsettings
```

### Database Migrations

```bash
# Identity service
dotnet ef migrations add MigrationName --project src/Legi.Identity.Infrastructure --startup-project src/Legi.Identity.Api
dotnet ef database update --project src/Legi.Identity.Infrastructure --startup-project src/Legi.Identity.Api
dotnet ef migrations remove --project src/Legi.Identity.Infrastructure --startup-project src/Legi.Identity.Api

# Catalog service
dotnet ef migrations add MigrationName --project src/Legi.Catalog.Infrastructure --startup-project src/Legi.Catalog.Api
dotnet ef database update --project src/Legi.Catalog.Infrastructure --startup-project src/Legi.Catalog.Api
dotnet ef migrations remove --project src/Legi.Catalog.Infrastructure --startup-project src/Legi.Catalog.Api

# Library service
dotnet ef migrations add MigrationName --project src/Legi.Library.Infrastructure --startup-project src/Legi.Library.Api
dotnet ef database update --project src/Legi.Library.Infrastructure --startup-project src/Legi.Library.Api
dotnet ef migrations remove --project src/Legi.Library.Infrastructure --startup-project src/Legi.Library.Api

# Social service
dotnet ef migrations add MigrationName --project src/Legi.Social.Infrastructure --startup-project src/Legi.Social.Api
dotnet ef database update --project src/Legi.Social.Infrastructure --startup-project src/Legi.Social.Api
dotnet ef migrations remove --project src/Legi.Social.Infrastructure --startup-project src/Legi.Social.Api
```

> APIs run `Database.Migrate()` on startup (idempotent), so manual `database update` is only needed for local-without-Docker workflows.

## Architecture

This is a **Clean Architecture** solution with **Domain-Driven Design (DDD)** principles, organized as multiple bounded contexts (services) sharing a common kernel. Dependencies flow inward toward the Domain.

### Solution Structure

```
Legi.SharedKernel              (shared base classes + mediator)
Legi.Contracts                 (integration event records shared across contexts)
Legi.Messaging                 (outbox/inbox + RabbitMQ transport, shared by all services)
├── Legi.Identity.*            (Identity bounded context)
│   ├── Domain
│   ├── Application
│   ├── Infrastructure
│   └── Api
├── Legi.Catalog.*             (Catalog bounded context)
│   ├── Domain
│   ├── Application
│   ├── Infrastructure
│   └── Api
├── Legi.Library.*             (Library bounded context)
│   ├── Domain
│   ├── Application
│   ├── Infrastructure
│   └── Api
├── Legi.Social.*              (Social bounded context)
│   ├── Domain
│   ├── Application
│   ├── Infrastructure
│   └── Api
├── web/legi-web/              (React frontend — Vite + Tailwind CSS v4)
└── tests/
    ├── Legi.Identity.Domain.Tests
    ├── Legi.Identity.Application.Tests
    ├── Legi.Catalog.Domain.Tests
    ├── Legi.Catalog.Application.Tests
    ├── Legi.Library.Domain.Tests
    ├── Legi.Library.Application.Tests
    ├── Legi.Social.Application.Tests
    ├── Legi.Messaging.Tests
    ├── Legi.Catalog.Integration.Tests   (skippable — needs CATALOG_TEST_DB)
    ├── Legi.Library.Integration.Tests   (skippable — needs LIBRARY_TEST_DB)
    └── Legi.Social.Integration.Tests    (skippable — needs SOCIAL_TEST_DB)
```

### Layer Structure (per bounded context)

```
SharedKernel (Base classes, Mediator - No Dependencies)
  ↑
Domain (Entities, Value Objects, Repository interfaces - Depends on SharedKernel only)
  ↑
Application (Commands, Queries, Behaviors - Depends on Domain only)
  ↑
Infrastructure (EF Core, Repositories, External services - Implements Application/Domain interfaces)
  ↑
API (Controllers, Middleware - Orchestrates everything)
```

### Key Patterns

- **CQRS**: Separate read/write repositories and command/query handlers
- **Custom Mediator**: Lightweight mediator in `Legi.SharedKernel.Mediator` (no MediatR dependency) dispatches requests through a pipeline of behaviors
- **Repository Pattern**: Write repositories (`IBookRepository`, `IUserRepository`) and read repositories (`IBookReadRepository`, `IAuthorReadRepository`, `ITagReadRepository`)
- **Aggregate Roots**: `User` manages `RefreshToken` collection; `Book` manages `Author` and `Tag` collections; `UserBook`, `ReadingPost`, `UserList` (Library)
- **Value Objects**: `Email`, `Username`, `Isbn`, `Author`, `Tag`, `Rating`, `Progress` — each with factory methods and validation
- **Domain Events**: Entities raise events (`BookCreatedDomainEvent`, `UserRegisteredDomainEvent`, `BookAddedToLibraryDomainEvent`, etc.)
- **Pipeline Behaviors**: `ValidationBehavior` (FluentValidation), `LoggingBehavior`, `UnhandledExceptionBehavior`
- **Soft Delete**: `UserBook` uses `DeletedAt` for soft delete with EF Core Global Query Filter

### Legi.SharedKernel

Shared abstractions with zero external dependencies:
- `BaseEntity`: Id (Guid), domain event collection
- `BaseAuditableEntity`: Adds `CreatedAt`, `UpdatedAt`
- `ValueObject`: Abstract base with component-based equality
- `IDomainEvent`: Marker interface with `OccurredOn`
- `DomainException`: Base domain exception
- `Mediator/`: Full mediator implementation — `IMediator`, `Mediator`, `IRequest`, `IRequestHandler`, `IPipelineBehavior`, `RequestHandlerDelegate`, `Unit`

### Legi.Contracts

Integration event records shared between bounded contexts (the only cross-context coupling allowed). All implement `IIntegrationEvent`. Organized by source context:
- Identity: `UserRegisteredIntegrationEvent`, `UserDeletedIntegrationEvent`
- Catalog: `BookCreatedIntegrationEvent`, `BookUpdatedIntegrationEvent`
- Library: `BookAddedToLibraryIntegrationEvent`, `ReadingStatusChangedIntegrationEvent`, `ReadingPostCreatedIntegrationEvent`, `ReadingPostDeletedIntegrationEvent`, `UserBookRatedIntegrationEvent`, `UserBookRatingRemovedIntegrationEvent`
- Social: `ContentLikedIntegrationEvent`, `ContentUnlikedIntegrationEvent`, `ContentCommentedIntegrationEvent`, `CommentDeletedIntegrationEvent`
- Diagnostics: `PingIntegrationEvent`

### Legi.Messaging

Transactional messaging infrastructure (Outbox/Inbox + RabbitMQ), referenced by every service's Infrastructure layer:
- **Outbox**: `OutboxMessage` + `OutboxMessageConfiguration` (persisted in each service DB), `OutboxEventBus` (enqueues events in the same transaction as the domain change), `OutboxDispatcherWorker` (background publisher), `RetentionCleaner` + `RetentionCleanupWorker` (prunes dispatched rows), `OutboxOptions`
- **Inbox**: `InboxMessage` + `InboxMessageConfiguration` (idempotent consumption — dedupes by message id), `IntegrationEventDispatcher` (routes a consumed event to its `IIntegrationEventHandler`s via the local mediator)
- **RabbitMq**: `RabbitMqPublisher`/`IRabbitMqPublisher`, `RabbitMqTopology`, `RabbitMqConsumerHost`, `RabbitMqConnectionFactory`, `ConsumerRetryPolicy` (retry + parking/dead-letter), `RabbitMqSettings`, `MessagingHostingOptions`
- **Serialization**: `IntegrationEventSerializer`
- **HealthChecks**: `OutboxBacklogHealthCheck`, `RabbitMqHealthCheck`
- **Diagnostics**: `MessagingMetrics` (observability counters)
- **DependencyInjection**: `MessagingExtensions` (wires outbox/inbox/consumers), `ModelBuilderExtensions` (adds outbox/inbox tables to a `DbContext`)

**Pattern**: services publish via the outbox (write-side, same transaction as the aggregate), and consume via RabbitMQ into the inbox (idempotent). Integration event handlers live in each service's Application layer under `*/IntegrationEventHandlers/`. Cross-context consumers currently wired:
- Catalog ← `UserBookRated` (recompute book average rating)
- Library ← `BookCreated`/`BookUpdated` (maintain `BookSnapshot`), `ContentLiked`/`ContentUnliked`/`ContentCommented`/`CommentDeleted` (post interaction counters)
- Social ← `UserRegistered` (create `UserProfile`), `BookCreated`/`BookUpdated` (maintain `ContentSnapshot`), `BookAddedToLibrary` (feed fan-out)

### Identity Service

**Legi.Identity.Domain**
- Entities: `User` (aggregate root), `RefreshToken`
- Value Objects: `Email`, `Username`
- Repository interfaces: `IUserRepository`
- Domain events: `UserRegisteredDomainEvent`, `UserProfileUpdatedDomainEvent`, `UserDeletedDomainEvent`

**Legi.Identity.Application**
- Auth Commands: `RegisterCommand`, `LoginCommand`, `RefreshTokenCommand`, `LogoutCommand`
- User Commands: `UpdateProfileCommand`, `DeleteAccountCommand`
- User Queries: `GetCurrentUserQuery`, `GetPublicProfileQuery`
- Each command/query has: Handler, Validator (FluentValidation), Request/Response DTOs
- Interfaces: `IJwtTokenService`, `IPasswordHasher`
- Behaviors: `ValidationBehavior`, `LoggingBehavior`, `UnhandledExceptionBehavior`
- Exceptions: `ConflictException`, `NotFoundException`, `UnauthorizedException`

**Legi.Identity.Infrastructure**
- `IdentityDbContext`: EF Core with PostgreSQL (port 5432)
- Entity configurations using Fluent API (maps Value Objects, relationships)
- `UserRepository`: Implements `IUserRepository`
- `JwtTokenService`: JWT access token generation + refresh token generation
- `PasswordHasher`: BCrypt-based password hashing
- `JwtSettings`: Configuration via Options pattern

**Legi.Identity.Api**
- `AuthController`: `/api/v1/identity/auth` — register, login, refresh, logout
- `UsersController`: `/api/v1/identity/users` — profile management
- JWT Bearer authentication, rate limiting (AspNetCoreRateLimit)
- `ExceptionHandlingMiddleware`: Maps exceptions to ProblemDetails
- Swagger/OpenAPI at `/swagger`

### Catalog Service

**Legi.Catalog.Domain**
- Entities: `Book` (aggregate root with max 10 authors, max 30 tags)
- Value Objects: `Isbn` (ISBN-10/13 with checksum), `Author` (name + slug), `Tag` (name + slug)
- Repository interfaces: `IBookRepository` (write), `IBookReadRepository`, `IAuthorReadRepository`, `ITagReadRepository`
- Enums: `BookSortBy` (Relevance, Title, AverageRating, RatingsCount, CreatedAt)
- Domain events: `BookCreatedDomainEvent`, `BookRatingRecalculatedDomainEvent`, `BookTagsUpdatedDomainEvent`
- DTOs: `BookSearchResult`, `BookDetailsResult` (in Domain for read repository contracts)

**Legi.Catalog.Application**
- Book Commands: `CreateBookCommand`, `UpdateBookCommand`, `DeleteBookCommand`
- Book Queries: `SearchBooksQuery` (with pagination, filtering, sorting), `GetBookDetailsQuery`
- Author Queries: `SearchAuthorsQuery` (autocomplete), `GetPopularAuthorsQuery`
- Tag Queries: `SearchTagsQuery` (autocomplete), `GetPopularTagsQuery`
- DTOs: `BookSummaryDto`, `AuthorDto`, `TagDto`, `PaginationMetadata`, `AuthorResult`, `TagResult`
- Integration Event Handlers: `UserBookRatedIntegrationEventHandler` (idempotent recompute of book average rating via `IBookRatingRepository`)
- Behaviors: `ValidationBehavior`, `LoggingBehavior`
- Exceptions: `ConflictException`, `NotFoundException`

**Legi.Catalog.Infrastructure**
- `CatalogDbContext`: EF Core with PostgreSQL (port 5433)
- Persistence entities (separate from domain): `AuthorEntity`, `TagEntity`, `BookAuthorEntity`, `BookTagEntity`
- Entity configurations: `BookConfiguration`, `AuthorConfiguration`, `TagConfiguration`, `BookAuthorConfiguration`, `BookTagConfiguration`
- `BookRepository`: Write operations, syncs authors/tags via junction tables, uses reflection to populate domain collections
- `BookReadRepository`: Read operations with LINQ joins for search, filtering, sorting, pagination
- `AuthorReadRepository`, `TagReadRepository`: Read-only repositories for search and popularity queries

**Legi.Catalog.Api**
- `BooksController`: `/api/v1/catalog/books` — CRUD (create, read, update, delete, search). Write endpoints require JWT auth (`[Authorize]`), read endpoints are public
- `AuthorsController`: `/api/v1/catalog/authors` — search (autocomplete) and popular authors
- `TagsController`: `/api/v1/catalog/tags` — search (autocomplete) and popular tags
- JWT Bearer authentication (shared `JwtSettings` from Identity Infrastructure)
- `ExceptionHandlingMiddleware`: Maps exceptions to ProblemDetails (ValidationException → 422, NotFoundException → 404, ConflictException → 409, DomainException → 400, UnauthorizedAccessException → 401)

### Library Service

**Legi.Library.Domain**
- Entities: `UserBook` (aggregate root, soft delete via `DeletedAt`), `ReadingPost` (aggregate root), `UserList` (aggregate root), `UserListItem` (child entity), `BookSnapshot` (read model)
- Value Objects: `Rating` (half-stars 1-10, `Stars` property for 0.5-5.0 display), `Progress` (value + type with `Completed()` factory)
- Enums: `ReadingStatus` (NotStarted, Reading, Finished, Abandoned, Paused), `ProgressType` (Page, Percentage)
- Repository interfaces: `IUserBookRepository`, `IReadingPostRepository`, `IUserListRepository`, `IBookSnapshotRepository`
- Domain events (8 total): `BookAddedToLibraryDomainEvent`, `BookRemovedFromLibraryDomainEvent`, `ReadingStatusChangedDomainEvent`, `UserBookRatedDomainEvent`, `UserBookRatingRemovedDomainEvent`, `ReadingPostCreatedDomainEvent`, `ReadingPostDeletedDomainEvent`, `UserListDeletedDomainEvent`

**Legi.Library.Application**
- UserBook Commands: `AddBookToLibraryCommand` (requires an existing `BookSnapshot` — populated from Catalog `BookCreated`/`BookUpdated` integration events; throws `NotFoundException` if the book hasn't been projected yet), `UpdateUserBookCommand`, `RemoveBookFromLibraryCommand`, `RateUserBookCommand`, `RemoveUserBookRatingCommand`
- Integration Event Handlers: `BookCreated`/`BookUpdatedIntegrationEventHandler` (maintain `BookSnapshot`), `ContentLiked`/`ContentUnliked`/`ContentCommented`/`CommentDeletedIntegrationEventHandler` (reading post interaction counters)
- ReadingPost Commands: `CreateReadingPostCommand`, `UpdateReadingPostCommand`, `DeleteReadingPostCommand`
- UserList Commands: `CreateUserListCommand`, `UpdateUserListCommand`, `DeleteUserListCommand`, `AddBookToListCommand`, `RemoveBookFromListCommand`
- Queries: `GetMyLibraryQuery` (with status/wishlist/search filters + pagination), `GetUserBookPostsQuery`, `GetMyListsQuery`, `GetListDetailsQuery`, `GetListBooksQuery`, `SearchPublicListsQuery`
- Read Repository Interfaces: `IUserBookReadRepository`, `IReadingPostReadRepository`, `IUserListReadRepository`
- DTOs: `UserBookDto`, `BookSnapshotDto`, `ReadingPostDto`, `UserListDetailDto`, `UserListSummaryDto`, `UserListBookDto`, `PaginatedList<T>`
- Behaviors: `ValidationBehavior`, `LoggingBehavior`
- Exceptions: `ConflictException`, `NotFoundException`, `ForbiddenException`

**Legi.Library.Infrastructure**
- `LibraryDbContext`: EF Core with PostgreSQL (connection string key: `LibraryDatabase`), domain event dispatch on SaveChanges
- Entity configurations: `UserBookConfiguration`, `ReadingPostConfiguration`, `UserListConfiguration`, `UserListItemConfiguration`, `BookSnapshotConfiguration`
- Write repositories: `UserBookRepository`, `ReadingPostRepository`, `UserListRepository`, `BookSnapshotRepository`
- Read repositories: `UserBookReadRepository`, `ReadingPostReadRepository`, `UserListReadRepository`

**Legi.Library.Api**
- `UserBooksController`: `/api/v1/library` — library CRUD, rating management
- `ReadingPostsController`: `/api/v1/library` — reading posts CRUD
- `UserListsController`: `/api/v1/library/lists` — list management, book-to-list operations, public search
- JWT Bearer authentication (shared `JwtSettings` from Identity Infrastructure)
- `ExceptionHandlingMiddleware`: Maps exceptions to ProblemDetails (ValidationException → 400, NotFoundException → 404, ConflictException → 409, ForbiddenException → 403, DomainException → 400)

### Social Service

**Legi.Social.Domain**
- Entities: `Follow` (aggregate root), `Like` (aggregate root), `Comment` (aggregate root), `UserProfile` (aggregate root, UserId as PK), `FeedItem` (read model), `ContentSnapshot` (read model)
- Enums: `InteractableType` (Post, Review, List), `ActivityType` (ProgressPosted, BookFinished, BookStarted, BookRated, ReviewCreated, ListCreated)
- Repository interfaces: `IFollowRepository`, `IUserProfileRepository`, `ILikeRepository`, `ICommentRepository`, `IContentSnapshotRepository`, `IFeedItemRepository`
- Domain events (6): `FollowCreatedDomainEvent`, `FollowRemovedDomainEvent`, `ContentLikedDomainEvent`, `ContentUnlikedDomainEvent`, `CommentCreatedDomainEvent`, `CommentDeletedDomainEvent`
- Key design: Like and Comment use polymorphic `TargetType + TargetId` (InteractableType) for unified interactions. FeedItem is a fully denormalized feed read model (fan-out on read). ContentSnapshot holds OwnerId for authorization checks.

**Legi.Social.Application**
- Follow Commands: `FollowUserCommand`, `UnfollowUserCommand`
- Comment Commands: `CreateCommentCommand` (with validator), `DeleteCommentCommand`
- Like Commands: `LikeContentCommand`, `UnlikeContentCommand`
- Profile Commands: `UpdateProfileCommand` (with validator)
- Follow Queries: `GetFollowersQuery`, `GetFollowingQuery`
- Comment Queries: `GetContentCommentsQuery`
- Like Queries: `GetContentLikesQuery`
- Feed Queries: `GetFeedQuery`, `GetUserActivityQuery`
- Profile Queries: `GetUserProfileQuery` (with IsFollowing contextual flag)
- Content Queries: `GetContentContextQuery` (header for comments/likes pages)
- Domain Event Handlers: `FollowCreated/RemovedDomainEventHandler` (UserProfile counters), `CommentCreated/DeletedDomainEventHandler`, `ContentLiked/UnlikedDomainEventHandler`
- Integration Event Handlers: `UserRegisteredIntegrationEventHandler` (create `UserProfile`), `BookCreated`/`BookUpdatedIntegrationEventHandler` (maintain `ContentSnapshot`), `BookAddedToLibraryIntegrationEventHandler` (feed fan-out)
- Read Repository Interfaces: `IFollowReadRepository`, `ICommentReadRepository`, `ILikeReadRepository`, `IFeedItemReadRepository`
- DTOs: `FollowUserDto`, `CommentDto`, `FeedItemDto`, `UserProfileDto`, `ContentContextDto`, `LikeUserDto`, `PaginatedList<T>`, response DTOs
- Behaviors: `ValidationBehavior`, `LoggingBehavior`
- Exceptions: `ConflictException`, `NotFoundException`, `ForbiddenException`

**Legi.Social.Infrastructure**
- `SocialDbContext`: EF Core with PostgreSQL (connection string key: `SocialDatabase`), domain event dispatch on SaveChanges
- Entity configurations: `FollowConfiguration`, `LikeConfiguration`, `CommentConfiguration`, `UserProfileConfiguration`, `FeedItemConfiguration`, `ContentSnapshotConfiguration`
- Write repositories: `FollowRepository`, `LikeRepository`, `CommentRepository`, `UserProfileRepository`, `ContentSnapshotRepository`, `FeedItemRepository`
- Read repositories: `FollowReadRepository`, `CommentReadRepository`, `LikeReadRepository`, `FeedItemReadRepository`

**Legi.Social.Api**
- `FollowsController`: `/api/v1/social/follows` — follow/unfollow; `/api/v1/social/users/{userId}/followers|following` — list followers/following
- `UserProfilesController`: `/api/v1/social/users/{userId}` — public profile with IsFollowing contextual flag
- `FeedController`: `/api/v1/social/feed` — personal feed (auth); `/api/v1/social/users/{userId}/activity` — user activity
- `PostInteractionsController`: `/api/v1/social/posts/{postId}/likes|comments` — like/unlike/comment on posts
- `ListInteractionsController`: `/api/v1/social/lists/{listId}/likes|comments` — like/unlike/comment on lists
- `CommentsController`: `/api/v1/social/comments/{commentId}` — delete comment (cross-type)
- JWT Bearer authentication (shared `JwtSettings` from Identity Infrastructure)
- `ExceptionHandlingMiddleware`: Maps exceptions to ProblemDetails (ValidationException → 400, NotFoundException → 404, ConflictException → 409, ForbiddenException → 403, DomainException → 400)

### Web Frontend

- **Location**: `web/legi-web/`
- **Stack**: React 19, TypeScript, Vite 8, Tailwind CSS v4, react-router-dom, i18next, axios
- **Utility**: `src/lib/utils.ts` — `cn()` helper using `clsx` + `tailwind-merge`
- **Docker**: Multi-stage build (node:22-alpine → nginx:alpine), served on port 3000
- **Nginx**: Serves SPA with `try_files` fallback to `/index.html`, reverse proxies API routes:
  - `/api/v1/identity/` → `identity-api:8080`
  - `/api/v1/catalog/` → `catalog-api:8080`
  - `/api/v1/library/` → `library-api:8080`
  - `/api/v1/social/` → `social-api:8080`

## Adding New Features

### Adding a Command (State-Changing Operation)

1. **Domain Layer**: Add business logic to entity or create new entity
2. **Application Layer**:
   - Create folder: `Application/[Feature]/Commands/[CommandName]/`
   - Add `[CommandName]Command.cs` record implementing `IRequest<TResponse>` (from `Legi.SharedKernel.Mediator`)
   - Add `[CommandName]CommandHandler.cs` implementing `IRequestHandler<TCommand, TResponse>`
   - Add `[CommandName]CommandValidator.cs` inheriting `AbstractValidator<TCommand>`
   - Add `[CommandName]Response.cs` record for the response DTO
3. **Infrastructure Layer**: Implement any new interfaces needed
4. **API Layer**: Add controller endpoint that sends command via `_mediator.Send(command)`

### Adding a Query (Read-Only Operation)

Follow same structure as commands but in `Queries/` folder instead of `Commands/`.

### Adding a New Entity

1. Create entity in `Domain/Entities/` (inherit from `BaseEntity` or `BaseAuditableEntity`)
2. Add repository interface in `Domain/Repositories/`
3. Create repository implementation in `Infrastructure/Persistence/Repositories/`
4. Create entity configuration in `Infrastructure/Persistence/Configurations/` using Fluent API
5. Add `DbSet<Entity>` to the appropriate `DbContext`
6. Create and apply migration

## Important Domain Rules

### User Entity (Identity)
- Maximum of 5 active refresh tokens per user (LRU eviction)
- Email is a Value Object with regex validation, normalized to lowercase
- Username is a Value Object: 3-30 chars, `[a-z][a-z0-9_]*`
- Password changes revoke all refresh tokens
- Profile updates raise `UserProfileUpdatedDomainEvent`

### Book Entity (Catalog)
- Maximum 10 authors, minimum 1 author
- Maximum 30 tags
- Title required, max 500 characters
- ISBN validated with checksum (ISBN-10 or ISBN-13), unique
- AverageRating: 0-5, rounded to 2 decimal places
- Authors and Tags are Value Objects identified by slug (kebab-case)

### UserBook Entity (Library)
- Soft delete via `DeletedAt` — `Remove()` marks as deleted, ReadingPosts preserved, UserListItems hard-deleted via domain event
- Re-adding same book creates new UserBook (new reading cycle)
- WishList auto-resets to `false` when status changes away from `NotStarted`
- Finishing sets progress to `Progress.Completed()` (100%); reverting from Finished resets progress to `null`
- Rating is independent of status (nullable, add/remove anytime via `Rate(Rating)` / `RemoveRating()`)
- Progress at 100% (Percentage) auto-transitions to Finished

### ReadingPost Entity (Library)
- Must have Content OR Progress (or both)
- Content max 2000 characters (`MaxContentLength` constant)
- Creating a post with progress updates `UserBook.CurrentProgress` in same transaction
- Desnormalized `UserId` and `BookId` for feed/query efficiency

### UserList Entity (Library)
- Name: 2-50 characters (`MinNameLength`, `MaxNameLength` constants), unique per user (case-insensitive)
- Description: max 500 characters (`MaxDescriptionLength` constant), optional
- Maximum 100 lists per user
- Duplicate book detection in list (same UserBookId)
- Supports reordering via `ReorderBooks()`

### Follow Entity (Social)
- User cannot follow themselves (validated in `Create` factory)
- Composite natural key: (FollowerId, FollowingId)
- Hard delete on unfollow (no soft delete)
- Raises `FollowCreatedDomainEvent` / `FollowRemovedDomainEvent` to update UserProfile counters

### UserProfile Entity (Social)
- UserId as PK (does not inherit BaseEntity)
- Bio: max 500 characters (`MaxBioLength` constant)
- FollowersCount / FollowingCount cannot go negative
- Created via integration event when user registers in Identity
- Username is a snapshot from Identity, updated via integration events

### Like Entity (Social)
- Composite natural key: (UserId, TargetType, TargetId) — user can like same content only once
- Hard delete on unlike
- Polymorphic via `InteractableType` (Post, Review, List)

### Comment Entity (Social)
- Content required, 1-500 characters (`MinContentLength`, `MaxContentLength` constants)
- Immutable — can only be created or deleted, no editing
- Deletable by comment author OR content owner (checked via ContentSnapshot in handler)
- Hard delete

### Authentication Flow
- Login returns both access token (JWT, 60min) and refresh token (7 days)
- Access token contains claims: `sub` (userId), `email`, `name`, `jti`, `iat`
- Refresh endpoint validates refresh token and issues new access token
- Logout revokes the specific refresh token used

## Configuration

### Docker Services

```bash
docker-compose up -d   # Starts all services
```

| Service        | Container            | Port          | Description                          |
|----------------|----------------------|---------------|--------------------------------------|
| identity-db    | legi-identity-db     | 5432          | PostgreSQL — Identity service        |
| catalog-db     | legi-catalog-db      | 5433          | PostgreSQL — Catalog service         |
| library-db     | legi-library-db      | 5434          | PostgreSQL — Library service         |
| social-db      | legi-social-db       | 5435          | PostgreSQL — Social service          |
| rabbitmq       | legi-rabbitmq        | 5672 / 15672  | RabbitMQ broker / Management UI      |
| identity-api   | legi-identity-api    | 5000          | Identity API                         |
| catalog-api    | legi-catalog-api     | 5112          | Catalog API                          |
| library-api    | legi-library-api     | 5200          | Library API                          |
| social-api     | legi-social-api      | 5300          | Social API                           |
| web            | legi-web             | 3000          | React frontend (nginx)               |

RabbitMQ Management UI: http://localhost:15672 (user `legi`, password `legi_dev`).

### Environment Variables (REQUIRED)

**CRITICAL**: Never commit secrets to source control. Use environment variables for all sensitive configuration.

1. Copy `.env.example` to `.env`:
   ```bash
   cp .env.example .env
   ```

2. Generate a secure JWT secret:
   ```bash
   openssl rand -base64 32
   ```

3. Update `.env` with your values (note the connection-string keys differ per service):
   ```bash
   Jwt__Secret=YOUR_GENERATED_SECRET_HERE
   ConnectionStrings__IdentityDb=Host=localhost;Port=5432;Database=legi_identity_dev;Username=postgres;Password=postgres
   ConnectionStrings__CatalogDatabase=Host=localhost;Port=5433;Database=legi_catalog_dev;Username=postgres;Password=postgres
   ConnectionStrings__LibraryDatabase=Host=localhost;Port=5434;Database=legi_library_dev;Username=postgres;Password=postgres
   ConnectionStrings__SocialDatabase=Host=localhost;Port=5435;Database=legi_social_dev;Username=postgres;Password=postgres
   ```

4. The `.env` file is gitignored and will never be committed.

**Note**: `docker-compose.yml` injects each service's connection string and `RabbitMq__*` settings directly, so Docker mode only needs `Jwt__Secret` in `.env`. For **local** runs (`dotnet run`), each service's `appsettings.Development.json` already carries localhost defaults for both its database and RabbitMQ, so `Jwt__Secret` is the only strictly required `.env` value. The `ConnectionStrings__*` keys in `.env` mainly exist to override mismatched defaults (e.g. `appsettings.json` hardcodes Catalog on port 5432 instead of 5433). `.env`/`.env.example` now include `SocialDatabase` for consistency; RabbitMq local overrides are commented examples in `.env.example`.

### JWT Settings
```bash
Jwt__Secret=YOUR_GENERATED_SECRET_MINIMUM_32_CHARACTERS
Jwt__Issuer=Legi.Identity
Jwt__Audience=Legi
Jwt__AccessTokenExpirationMinutes=60
Jwt__RefreshTokenExpirationDays=7
```

**Note**: The `appsettings.json` files have empty secrets. You MUST configure via environment variables.

## Package Versions

**Critical**: Keep JWT package versions aligned:
- `Microsoft.IdentityModel.Tokens` and `System.IdentityModel.Tokens.Jwt` must match (currently 8.14.0)
- If updating, update both packages together to avoid downgrade errors

## Testing Strategy

- **Domain.Tests**: Test entity business logic, value object validation, domain events
- **Application.Tests**: Test command/query handlers, validators
- Use xUnit with coverlet for code coverage
- Mock repositories and infrastructure services in Application tests
- Test factories provide reusable test data builders
- Integration tests (`*.Integration.Tests`) use `Xunit.SkippableFact` and require a Postgres connection via env vars (`CATALOG_TEST_DB`, `LIBRARY_TEST_DB`, `SOCIAL_TEST_DB`) — they skip otherwise
- **Current unit test counts**: Identity Domain (35), Identity Application (38), Catalog Domain (66), Catalog Application (90), Library Domain (1), Library Application (23), Social Application (21), Messaging (14) — Total: 288 tests (plus the skippable integration suites)
