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

# Run the Identity API
dotnet run --project src/Legi.Identity.Api/Legi.Identity.Api.csproj

# Run the Catalog API
dotnet run --project src/Legi.Catalog.Api/Legi.Catalog.Api.csproj

# Run the Library API
dotnet run --project src/Legi.Library.Api/Legi.Library.Api.csproj

# Start PostgreSQL databases
docker-compose up -d

# Stop PostgreSQL databases
docker-compose down
```

### Testing

```bash
# Run all tests
dotnet test

# Run tests for a specific project
dotnet test tests/Legi.Identity.Domain.Tests/Legi.Identity.Domain.Tests.csproj
dotnet test tests/Legi.Identity.Application.Tests/Legi.Identity.Application.Tests.csproj

# Run a single test class
dotnet test --filter "FullyQualifiedName~ClassName"

# Run tests with coverage
dotnet test /p:CollectCoverage=true
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
```

## Architecture

This is a **Clean Architecture** solution with **Domain-Driven Design (DDD)** principles, organized as multiple bounded contexts (services) sharing a common kernel. Dependencies flow inward toward the Domain.

### Solution Structure

```
Legi.SharedKernel              (shared base classes + mediator)
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
│   ├── Domain                 (✅ complete)
│   ├── Application            (✅ complete)
│   ├── Infrastructure         (✅ complete)
│   └── Api                    (✅ complete)
└── tests/
    ├── Legi.Identity.Domain.Tests
    ├── Legi.Identity.Application.Tests
    ├── Legi.Catalog.Domain.Tests
    └── Legi.Catalog.Application.Tests
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
- UserBook Commands: `AddBookToLibraryCommand`, `UpdateUserBookCommand`, `RemoveBookFromLibraryCommand`, `RateUserBookCommand`, `RemoveUserBookRatingCommand`
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

### Authentication Flow
- Login returns both access token (JWT, 60min) and refresh token (7 days)
- Access token contains claims: `sub` (userId), `email`, `name`, `jti`, `iat`
- Refresh endpoint validates refresh token and issues new access token
- Logout revokes the specific refresh token used

## Configuration

### Docker Services

```bash
docker-compose up -d   # Starts identity-db (port 5432) and catalog-db (port 5433)
```

Future services (commented out in docker-compose): library-db, social-db, rabbitmq.

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

3. Update `.env` with your values:
   ```bash
   Jwt__Secret=YOUR_GENERATED_SECRET_HERE
   ConnectionStrings__IdentityDb=Host=localhost;Port=5432;Database=legi_identity_dev;Username=postgres;Password=postgres
   ConnectionStrings__CatalogDb=Host=localhost;Port=5433;Database=legi_catalog_dev;Username=postgres;Password=postgres
   ```

4. The `.env` file is gitignored and will never be committed.

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
- **Current test counts**: Identity Domain (42), Identity Application (45), Catalog Domain (52), Catalog Application (44) — Total: 183 tests
