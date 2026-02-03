# Legi Identity

A robust identity and authentication service built with Clean Architecture and Domain-Driven Design (DDD) principles.

## 🏗️ Architecture

This project follows **Clean Architecture** with clear separation of concerns across multiple layers:

```
┌─────────────────────────────────────────┐
│           API Layer (Controllers)       │
├─────────────────────────────────────────┤
│      Application Layer (CQRS/MediatR)   │
├─────────────────────────────────────────┤
│         Domain Layer (Entities)         │
├─────────────────────────────────────────┤
│    Infrastructure (EF Core, Services)   │
└─────────────────────────────────────────┘
```

### Project Structure

```
src/
├── Legi.Identity.Api/              # HTTP API endpoints
├── Legi.Identity.Application/      # Business logic & CQRS
├── Legi.Identity.Domain/           # Core domain entities & rules
└── Legi.Identity.Infrastructure/   # Database & external services

tests/
├── Legi.Identity.Domain.Tests/
└── Legi.Identity.Application.Tests/
```

## ✨ Features

### Authentication & Authorization
- ✅ User registration with email and username
- ✅ Login with email OR username
- ✅ JWT access tokens
- ✅ Refresh token rotation
- ✅ Secure logout
- ✅ Password hashing with BCrypt

### User Management
- ✅ Profile management (name, bio, avatar)
- ✅ Password updates
- ✅ Account deletion
- ✅ Public profile viewing

### Security
- ✅ Rate limiting (AspNetCoreRateLimit)
- ✅ Custom exception handling middleware
- ✅ Input validation (FluentValidation)
- ✅ Unique email and username constraints
- ✅ Refresh token management (max 5 active tokens per user)

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK
- PostgreSQL 14+
- Docker & Docker Compose (optional)

### Environment Variables

Create a `.env` file in the `src/Legi.Identity.Api` directory:

```env
# Database
DATABASE_HOST=localhost
DATABASE_PORT=5432
DATABASE_NAME=legi_identity
DATABASE_USER=postgres
DATABASE_PASSWORD=your_password

# JWT
JWT_SECRET_KEY=your-super-secret-key-at-least-32-characters-long
JWT_ISSUER=Legi.Identity
JWT_AUDIENCE=Legi.Api
JWT_EXPIRATION_MINUTES=60
```

### Running with Docker Compose

```bash
# Start PostgreSQL
docker-compose up -d

# Run migrations
cd src/Legi.Identity.Infrastructure
dotnet ef database update --startup-project ../Legi.Identity.Api

# Run the API
cd ../Legi.Identity.Api
dotnet run
```

### Running Locally

```bash
# Restore dependencies
dotnet restore

# Run migrations
cd src/Legi.Identity.Infrastructure
dotnet ef database update --startup-project ../Legi.Identity.Api

# Run the API
cd ../Legi.Identity.Api
dotnet run
```

The API will be available at `https://localhost:7001`

## 📡 API Endpoints

### Authentication

#### Register
```http
POST /api/v1/identity/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "username": "johndoe",
  "password": "Password123!",
  "name": "John Doe"
}
```

**Response (201 Created)**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "username": "johndoe",
  "name": "John Doe",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "hashed_refresh_token",
  "expiresAt": "2026-02-03T12:00:00Z"
}
```

#### Login
```http
POST /api/v1/identity/auth/login
Content-Type: application/json

{
  "emailOrUsername": "johndoe",  // or "user@example.com"
  "password": "Password123!"
}
```

**Response (200 OK)**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "username": "johndoe",
  "name": "John Doe",
  "avatarUrl": null,
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "hashed_refresh_token",
  "expiresAt": "2026-02-03T12:00:00Z"
}
```

#### Refresh Token
```http
POST /api/v1/identity/auth/refresh
Content-Type: application/json

{
  "refreshToken": "hashed_refresh_token"
}
```

#### Logout
```http
POST /api/v1/identity/auth/logout
Authorization: Bearer {token}
Content-Type: application/json

{
  "refreshToken": "hashed_refresh_token"
}
```

### User Management

#### Get Current User Profile
```http
GET /api/v1/identity/users/me
Authorization: Bearer {token}
```

#### Update Profile
```http
PUT /api/v1/identity/users/me
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Jane Doe",
  "bio": "Software developer",
  "avatarUrl": "https://example.com/avatar.jpg"
}
```

#### Delete Account
```http
DELETE /api/v1/identity/users/me
Authorization: Bearer {token}
```

#### Get Public Profile
```http
GET /api/v1/identity/users/{username}
```

## 🧪 Testing

### Run All Tests
```bash
dotnet test
```

### Run Domain Tests Only
```bash
dotnet test tests/Legi.Identity.Domain.Tests/
```

### Run Application Tests Only
```bash
dotnet test tests/Legi.Identity.Application.Tests/
```

### Test Coverage

- **39 tests** with 100% pass rate
- **Domain Tests (30)**: Value Objects, Entities, Domain Events
- **Application Tests (9)**: Command Handlers with mocked dependencies

### Test Factories

The project includes test factories for cleaner test code:

```csharp
// Create test users easily
var user = UserFactory.Create();
var user = UserFactory.CreateWithEmail("test@example.com");
var user = UserFactory.CreateRandom();

// Create test commands
var command = RegisterCommandFactory.Create();
var command = LoginCommandFactory.CreateWithUsername("johndoe");
```

## 🛠️ Technologies

### Core
- **.NET 8.0** - Framework
- **ASP.NET Core** - Web API
- **C# 12** - Language

### Architecture & Patterns
- **Clean Architecture** - Separation of concerns
- **Domain-Driven Design (DDD)** - Rich domain model
- **CQRS** - Command Query Responsibility Segregation
- **MediatR** - Mediator pattern implementation

### Data & Persistence
- **Entity Framework Core 8** - ORM
- **PostgreSQL** - Database
- **Npgsql** - PostgreSQL driver

### Security
- **BCrypt.Net** - Password hashing
- **JWT Bearer Authentication** - Token-based auth
- **AspNetCoreRateLimit** - Rate limiting

### Validation & Mapping
- **FluentValidation** - Input validation
- **AutoMapper** - Object mapping (if needed)

### Testing
- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **Moq** - Mocking framework

### Development
- **DotNetEnv** - Environment variable management
- **Swashbuckle (Swagger)** - API documentation

## 📋 Domain Model

### Entities

#### User (Aggregate Root)
```csharp
public class User : AggregateRoot
{
    public Guid Id { get; }
    public Email Email { get; }
    public Username Username { get; }
    public string PasswordHash { get; }
    public string Name { get; }
    public string? Bio { get; }
    public string? AvatarUrl { get; }
    public IReadOnlyList<RefreshToken> RefreshTokens { get; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; }
}
```

#### RefreshToken (Entity)
```csharp
public class RefreshToken : Entity
{
    public string TokenHash { get; }
    public DateTime ExpiresAt { get; }
    public bool IsActive { get; }
    public DateTime? RevokedAt { get; }
}
```

### Value Objects

- **Email** - Email address with validation and normalization
- **Username** - Username with format validation (3-30 chars, alphanumeric + underscore)

### Domain Events

- **UserRegisteredDomainEvent** - Raised when a new user registers
- **UserProfileUpdatedDomainEvent** - Raised when user updates their profile

## 🎯 Validation Rules

### Email
- ✅ Required
- ✅ Valid email format
- ✅ Max 255 characters
- ✅ Normalized to lowercase
- ✅ Must be unique

### Username
- ✅ Required
- ✅ 3-30 characters
- ✅ Must start with a letter
- ✅ Only lowercase letters, numbers, and underscore
- ✅ Normalized to lowercase
- ✅ Must be unique

### Password
- ✅ Required
- ✅ 8-100 characters
- ✅ At least one uppercase letter
- ✅ At least one number
- ✅ Hashed with BCrypt

### Name
- ✅ Required
- ✅ 2-100 characters

### Bio
- ⚠️ Optional
- ✅ Max 500 characters

## 🔐 Security Features

### Rate Limiting

```
General: 1000 requests per 15 minutes
Auth endpoints: 5 requests per 5 minutes
```

### Token Management

- Access tokens expire after 60 minutes (configurable)
- Refresh tokens expire after 7 days
- Maximum 5 active refresh tokens per user
- Oldest token auto-revoked when limit exceeded
- All refresh tokens revoked on password change

### Password Security

- BCrypt hashing with salt
- Minimum complexity requirements
- Never stored or logged in plain text

## 📖 Development

### Add Migration

```bash
cd src/Legi.Identity.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../Legi.Identity.Api
```

### Update Database

```bash
dotnet ef database update --startup-project ../Legi.Identity.Api
```

### Remove Last Migration

```bash
dotnet ef migrations remove --startup-project ../Legi.Identity.Api
```

## 🐛 Error Handling

The API uses RFC 7807 Problem Details for error responses:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["Email is required"],
    "Password": ["Password must be at least 8 characters"]
  }
}
```

### Status Codes

- **200 OK** - Success
- **201 Created** - Resource created
- **204 No Content** - Success with no response body
- **400 Bad Request** - Validation error
- **401 Unauthorized** - Authentication required
- **404 Not Found** - Resource not found
- **409 Conflict** - Duplicate email/username
- **429 Too Many Requests** - Rate limit exceeded
- **500 Internal Server Error** - Server error

## 📝 License

This project is part of the Legi platform.

## 🤝 Contributing

1. Follow Clean Architecture principles
2. Write tests for new features
3. Use English for all code, comments, and documentation
4. Follow the existing code style
5. Run tests before submitting PR

---

Built with ❤️ using Clean Architecture and DDD
