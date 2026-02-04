# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Building and Running

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/Legi.Identity.Api/Legi.Identity.Api.csproj

# Run the API (starts on https://localhost:5001, http://localhost:5000)
dotnet run --project src/Legi.Identity.Api/Legi.Identity.Api.csproj

# Start PostgreSQL database
docker-compose up -d

# Stop PostgreSQL database
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
# Add a new migration
dotnet ef migrations add MigrationName --project src/Legi.Identity.Infrastructure --startup-project src/Legi.Identity.Api

# Apply migrations
dotnet ef database update --project src/Legi.Identity.Infrastructure --startup-project src/Legi.Identity.Api

# Remove last migration
dotnet ef migrations remove --project src/Legi.Identity.Infrastructure --startup-project src/Legi.Identity.Api
```

## Architecture

This is a **Clean Architecture** project with **Domain-Driven Design (DDD)** principles. The solution follows strict layering with dependencies flowing inward toward the Domain.

### Layer Structure

```
Domain (Core - No Dependencies)
  ↑
Application (Depends on Domain only)
  ↑
Infrastructure (Implements Application interfaces)
  ↑
API (Presentation - Orchestrates everything)
```

### Key Patterns

- **CQRS**: Commands and Queries are separated using MediatR
- **Mediator Pattern**: MediatR dispatches all requests through a pipeline with behaviors
- **Repository Pattern**: Data access abstracted via `IUserRepository`
- **Aggregate Root**: `User` entity manages its `RefreshToken` collection
- **Value Objects**: `Email` encapsulates validation and ensures value equality
- **Domain Events**: Entities raise events (`UserRegisteredDomainEvent`, etc.)
- **Pipeline Behaviors**: `ValidationBehavior` (FluentValidation), `LoggingBehavior`, `UnhandledExceptionBehavior`

### Project Responsibilities

**Legi.Identity.Domain** (Core business logic)
- Entities: `User` (aggregate root), `RefreshToken`
- Value Objects: `Email`
- Repository interfaces: `IUserRepository`
- Domain events and exceptions
- NO external dependencies

**Legi.Identity.Application** (Use cases)
- Commands: `RegisterCommand`, `LoginCommand`, `RefreshTokenCommand`, `LogoutCommand`, `UpdateProfileCommand`, `DeleteAccountCommand`
- Queries: `GetCurrentUserQuery`, `GetPublicProfileQuery`
- Each command/query has: Handler, Validator (FluentValidation), Request/Response DTOs
- Interfaces for infrastructure: `IJwtTokenService`, `IPasswordHasher`
- Pipeline behaviors for cross-cutting concerns

**Legi.Identity.Infrastructure** (Technical implementation)
- `IdentityDbContext`: EF Core with PostgreSQL
- Entity configurations using Fluent API (maps Value Objects, relationships)
- `UserRepository`: Implements `IUserRepository`
- `JwtTokenService`: JWT token generation with claims
- `PasswordHasher`: BCrypt-based password hashing
- `JwtSettings`: Configuration via Options pattern

**Legi.Identity.Api** (HTTP interface)
- `AuthController`: `/api/v1/identity/auth` - register, login, refresh, logout
- `UsersController`: `/api/v1/identity/users` - profile management
- JWT Bearer authentication configured
- Swagger/OpenAPI documentation at `/swagger`

## Adding New Features

### Adding a Command (State-Changing Operation)

1. **Domain Layer**: Add business logic to entity or create new entity
2. **Application Layer**:
   - Create folder: `Application/[Feature]/Commands/[CommandName]/`
   - Add `[CommandName]Command.cs` record implementing `IRequest<TResponse>`
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
5. Add `DbSet<Entity>` to `IdentityDbContext`
6. Create and apply migration

## Important Domain Rules

### User Entity
- Maximum of 5 active refresh tokens per user
- Email is a Value Object with regex validation
- Password changes revoke all refresh tokens
- Profile updates raise `UserProfileUpdatedDomainEvent`

### RefreshToken
- 7-day expiration (configurable via `JwtSettings.RefreshTokenExpirationDays`)
- Can be revoked explicitly via logout
- Stored as hashed value, not plaintext

### Authentication Flow
- Login returns both access token (JWT, 60min) and refresh token (7 days)
- Access token contains claims: `sub` (userId), `email`, `name`, `jti`, `iat`
- Refresh endpoint validates refresh token and issues new access token
- Logout revokes the specific refresh token used

## Configuration

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
   ```

4. The `.env` file is gitignored and will never be committed.

### Database Connection
Use environment variables:
```bash
ConnectionStrings__IdentityDb=Host=localhost;Port=5432;Database=legi_identity_dev;Username=postgres;Password=postgres
```

### JWT Settings
Use environment variables (REQUIRED):
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
