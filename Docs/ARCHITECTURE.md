# Legi - Arquitetura do Sistema

Sistema de gerenciamento pessoal de leitura com recursos sociais.

## Status de Implementação

| Serviço | Status | Observação |
|---------|--------|------------|
| **SharedKernel** | ✅ Implementado | Base classes, custom Mediator |
| **Identity** | ✅ Implementado | Auth completa, perfil de usuário |
| **Catalog** | 🔧 Em progresso | Book management com create/search/details/update/delete implementados |
| **Library** | 📋 Planejado | Não iniciado |
| **Social** | 📋 Planejado | Não iniciado |

## Stack Tecnológica

| Camada         | Tecnologia                      |
|----------------|---------------------------------|
| Backend        | .NET 8, ASP.NET Core            |
| Frontend       | React + TypeScript (planejado)  |
| Banco de Dados | PostgreSQL (db separado por serviço) |
| Mensageria     | RabbitMQ (planejado)            |
| API Gateway    | YARP (planejado)                |
| API Externa    | Open Library + Google Books Api (planejado) |
| Mediator       | Custom (`Legi.SharedKernel.Mediator` — sem dependência MediatR) |
| Validação      | FluentValidation                |
| ORM            | Entity Framework Core 8 + Npgsql |
| Auth           | JWT Bearer + BCrypt             |
| Testes         | xUnit + coverlet                |

## Bounded Contexts

```
┌─────────────────────────────────────────────────────────────────┐
│                       API Gateway (planejado)                   │
└──────────┬──────────┬──────────┬──────────┬────────────────────┘
           │          │          │          │
           ▼          ▼          ▼          ▼
     ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
     │ Identity │ │ Catalog  │ │ Library  │ │  Social  │
     │ Service  │ │ Service  │ │ Service  │ │ Service  │
     │    ✅    │ │    🔧    │ │    📋    │ │    📋    │
     └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘
          │            │            │            │
          ▼            ▼            ▼            ▼
     ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
     │ identity │ │ catalog  │ │ library  │ │  social  │
     │  db:5432 │ │  db:5433 │ │    db    │ │    db    │
     └──────────┘ └──────────┘ └──────────┘ └──────────┘

Todos os serviços dependem de:
┌──────────────────────────────────────────────────────────────────┐
│                      Legi.SharedKernel                           │
│  BaseEntity, BaseAuditableEntity, ValueObject, IDomainEvent,    │
│  DomainException, Mediator (IMediator, IRequest,                │
│  IRequestHandler, IPipelineBehavior, RequestHandlerDelegate,    │
│  Unit)                                                          │
└──────────────────────────────────────────────────────────────────┘
```

| Serviço | Responsabilidade | Tipo |
|---------|------------------|------|
| **Identity** | Autenticação, usuários, JWT | Suporte |
| **Catalog** | Livros globais, tags, reviews | Core |
| **Library** | Biblioteca pessoal, progresso, listas | Core |
| **Social** | Follows, feed, likes, comments, descoberta | Core |

### Estrutura de Camadas (por serviço)

```
SharedKernel (Base classes, Mediator — Sem Dependências)
  ↑
Domain (Entities, Value Objects, Repository interfaces — Depende apenas do SharedKernel)
  ↑
Application (Commands, Queries, Behaviors — Depende apenas do Domain)
  ↑
Infrastructure (EF Core, Repositories, Services externos — Implementa interfaces do Domain/Application)
  ↑
API (Controllers, Middleware — Orquestra tudo)
```

---

## 0. SharedKernel ✅

Abstrações compartilhadas com zero dependências externas.

**Base Classes:**
- `BaseEntity` — Id (Guid), coleção de domain events
- `BaseAuditableEntity` — Adiciona `CreatedAt`, `UpdatedAt`
- `ValueObject` — Base abstrata com igualdade por componentes
- `IDomainEvent` — Interface marker com `OccurredOn`
- `DomainException` — Exceção base de domínio

**Mediator (custom, sem MediatR):**
- `IMediator` / `Mediator` — Despacha requests para handlers via pipeline de behaviors (reflection-based)
- `IRequest<TResponse>` / `IRequest` — Marker interfaces para commands/queries
- `IRequestHandler<TRequest, TResponse>` — Handler interface
- `IPipelineBehavior<TRequest, TResponse>` — Cross-cutting concerns (validation, logging)
- `RequestHandlerDelegate<TResponse>` — Delegate para pipeline continuation
- `Unit` — Tipo void para C#

---

## 1. Identity Service ✅

### 1.1 Domínio

**Aggregates:**

```
User (Aggregate Root)
├── Id: Guid
├── Email: Email (VO)
├── Username: Username (VO)
├── PasswordHash: string
├── Name: string (2-100 chars)
├── Bio: string? (max 500)
├── AvatarUrl: string?
├── RefreshTokens: List<RefreshToken>
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

RefreshToken (Entity)
├── Id: Guid
├── TokenHash: string
├── ExpiresAt: DateTime
├── CreatedAt: DateTime
└── RevokedAt: DateTime?
```

**Value Objects:**
- `Email` - formato válido, normalizado para lowercase, único
- `Username` - 3-30 chars, lowercase, letras/números/underscore, começa com letra

**Regras:**
- Máximo 5 refresh tokens ativos por usuário (LRU eviction)
- Password: mínimo 8 chars, 1 maiúscula, 1 número
- Ao trocar senha, todos refresh tokens são revogados

**Domain Events:**
- `UserRegisteredDomainEvent`
- `UserProfileUpdatedDomainEvent`
- `UserDeletedDomainEvent`

### 1.2 Application

**Auth Commands:** `RegisterCommand`, `LoginCommand`, `RefreshTokenCommand`, `LogoutCommand`
**User Commands:** `UpdateProfileCommand`, `DeleteAccountCommand`
**User Queries:** `GetCurrentUserQuery`, `GetPublicProfileQuery`
**Behaviors:** `ValidationBehavior`, `LoggingBehavior`, `UnhandledExceptionBehavior`
**Interfaces:** `IJwtTokenService`, `IPasswordHasher`
**Exceptions:** `ConflictException`, `NotFoundException`, `UnauthorizedException`

### 1.3 Infrastructure

- `IdentityDbContext` — EF Core + PostgreSQL (porta 5432)
- `UserRepository` — Implementa `IUserRepository`
- `JwtTokenService` — Gera access token (JWT, HMAC-SHA256) + refresh token (64 bytes Base64)
- `PasswordHasher` — BCrypt hash/verify
- `JwtSettings` — Options pattern (Secret, Issuer, Audience, expirations)

### 1.4 API Endpoints

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| POST | `/api/v1/identity/auth/register` | Registrar usuário | - |
| POST | `/api/v1/identity/auth/login` | Login (email ou username) | - |
| POST | `/api/v1/identity/auth/refresh` | Renovar token | - |
| POST | `/api/v1/identity/auth/logout` | Logout | 🔒 |
| GET | `/api/v1/identity/users/me` | Meu perfil | 🔒 |
| PATCH | `/api/v1/identity/users/me` | Atualizar perfil | 🔒 |
| DELETE | `/api/v1/identity/users/me` | Deletar conta | 🔒 |
| GET | `/api/v1/identity/users/{userId}` | Perfil público | 🔓 |

**Middleware:** `ExceptionHandlingMiddleware`, Rate Limiting (AspNetCoreRateLimit)

### 1.5 Database Schema

```sql
-- Tabela: users
CREATE TABLE users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    username VARCHAR(30) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    name VARCHAR(100) NOT NULL,
    bio VARCHAR(500),
    avatar_url VARCHAR(500),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_users_email ON users(email);
CREATE INDEX ix_users_username ON users(username);
CREATE INDEX ix_users_created_at ON users(created_at DESC);

-- Tabela: refresh_tokens
CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    revoked_at TIMESTAMPTZ
);

CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX ix_refresh_tokens_token_hash ON refresh_tokens(token_hash);
```

---

## 2. Catalog Service 🔧

### 2.1 Domínio

**Aggregates:**

```
Book (Aggregate Root) ✅
├── Id: Guid
├── ISBN: ISBN (VO)
├── Title: string (max 500)
├── Authors: List<Author> (VO) - máximo 10, mínimo 1
├── AuthorDisplay: string (computed - join dos nomes)
├── Synopsis: string?
├── PageCount: int?
├── Publisher: string?
├── CoverUrl: string?
├── AverageRating: decimal (0-5, desnormalizado)
├── RatingsCount: int (desnormalizado)
├── ReviewsCount: int (desnormalizado)
├── Tags: List<Tag> (VO) - máximo 30
├── CreatedByUserId: Guid
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

BookReview (Aggregate Root) 📋 PLANEJADO
├── Id: Guid
├── BookId: Guid
├── UserId: Guid
├── Content: string (10-5000 chars)
├── Rating: Rating? (VO, 0-5)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime
```

**Value Objects:**
- `ISBN` ✅ - 10 ou 13 dígitos, checksum válido
- `Author` ✅ - name (2-255 chars), slug (gerado, kebab-case)
- `Tag` ✅ - name (2-50 chars), slug (gerado, kebab-case)
- `Rating` 📋 - inteiro 0-5 (planejado, para BookReview)

**Persistence Entities (Infrastructure) ✅:**

Separação entre domínio (Value Objects) e persistência (Entities) para search/autocomplete:

```
AuthorEntity (não é domínio)
├── Id: int (PK, SERIAL)
├── Name: string
├── Slug: string (unique)
├── BooksCount: int (desnormalizado)
└── CreatedAt: DateTime

TagEntity (não é domínio)
├── Id: int (PK, SERIAL)
├── Name: string
├── Slug: string (unique)
├── UsageCount: int (desnormalizado)
└── CreatedAt: DateTime

BookAuthorEntity (junction)
├── BookId: Guid (PK, FK)
├── AuthorId: int (PK, FK)
├── Order: int (0 = autor primário)
└── AddedAt: DateTime

BookTagEntity (junction)
├── BookId: Guid (PK, FK)
├── TagId: int (PK, FK)
└── AddedAt: DateTime
```

**Regras:**
- ISBN é obrigatório e único
- Livro deve ter pelo menos 1 autor (máximo 10)
- Máximo 30 tags por livro
- Autores são únicos por slug (evita duplicação: "J.K. Rowling" vs "J.K.Rowling")
- Tags são únicas por slug (evita duplicação)
- Um usuário só pode ter uma review por livro (planejado)
- AverageRating recalculado quando ratings mudam

**Domain Events:**
- `BookCreatedDomainEvent` ✅ (com lista de autores)
- `BookTagsUpdatedDomainEvent` ✅
- `BookRatingRecalculatedDomainEvent` ✅
- `ReviewCreatedDomainEvent` 📋
- `ReviewUpdatedDomainEvent` 📋
- `ReviewDeletedDomainEvent` 📋

**Arquitetura Híbrida (Author/Tag):**

A separação entre Value Objects no domínio e Entities na persistência permite:
- **Domínio limpo**: `Author` e `Tag` são Value Objects imutáveis, sem identidade própria
- **Persistência otimizada**: `AuthorEntity` e `TagEntity` têm ID para:
  - Evitar duplicação (normalização)
  - Busca rápida (autocomplete)
  - Contadores desnormalizados (popularidade)
  - Páginas de autor/tag com todos os livros

O repositório `BookRepository` sincroniza:
- Ao salvar: cria/atualiza entidades de autor/tag, mantém contadores
- Ao carregar: converte entidades em Value Objects para o domínio (via reflection)

### 2.2 Application

**Commands implementados:**
- `CreateBookCommand` ✅ — Cria livro com ISBN, título, autores e tags
- `UpdateBookCommand` ✅ — Atualiza dados básicos, autores e tags de um livro
- `DeleteBookCommand` ✅ — Remove livro do catálogo

**Queries implementadas:**
- `SearchBooksQuery` ✅ — Busca com filtros, paginação, sorting (`BookSortBy`: Relevance, Title, AverageRating, RatingsCount, CreatedAt)
- `GetBookDetailsQuery` ✅ — Detalhes completos por ID

**DTOs:** `BookSummaryDto`, `AuthorDto`, `TagDto`, `PaginationMetadata`, `CreateBookResponse`, `UpdateBookResponse`, `GetBookDetailsResponse`
**Behaviors:** `ValidationBehavior`, `LoggingBehavior`
**Exceptions:** `ConflictException`, `NotFoundException`

**Repositories (Domain interfaces):**
- `IBookRepository` ✅ (write: Add, Update, Delete, GetById, GetByIsbn)
- `IBookReadRepository` ✅ (read: Search, GetDetailsById, GetDetailsByIsbn)
- `IAuthorReadRepository` ✅ (Search, GetPopular, GetBySlug)
- `ITagReadRepository` ✅ (Search, GetPopular)

### 2.3 API Endpoints

**Books (implementados):**

| Método | Endpoint | Descrição | Auth | Status |
|--------|----------|-----------|------|--------|
| GET | `/api/v1/catalog/books` | Buscar livros | 🔓 | ✅ |
| GET | `/api/v1/catalog/books/{bookId}` | Detalhes do livro | 🔓 | ✅ |
| POST | `/api/v1/catalog/books` | Cadastrar livro | 🔒 (TODO: JWT) | ✅ |
| PUT | `/api/v1/catalog/books/{bookId}` | Atualizar livro | 🔒 (TODO: JWT) | ✅ |
| DELETE | `/api/v1/catalog/books/{bookId}` | Excluir livro | 🔒 (TODO: JWT) | ✅ |

**Books (planejados):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| POST | `/api/v1/catalog/books/{bookId}/tags` | Adicionar tags | 🔒 |

**Reviews (planejados):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| GET | `/api/v1/catalog/books/{bookId}/reviews` | Listar reviews | 🔓 |
| POST | `/api/v1/catalog/books/{bookId}/reviews` | Criar review | 🔒 |
| PUT | `/api/v1/catalog/reviews/{reviewId}` | Editar review | 🔒 |
| DELETE | `/api/v1/catalog/reviews/{reviewId}` | Excluir review | 🔒 |

**Authors (planejados — read repositories já implementados na Infrastructure):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| GET | `/api/v1/catalog/authors` | Buscar autores (autocomplete) | 🔓 |
| GET | `/api/v1/catalog/authors/popular` | Autores populares | 🔓 |
| GET | `/api/v1/catalog/authors/{slug}` | Detalhes do autor | 🔓 |
| GET | `/api/v1/catalog/authors/{slug}/books` | Livros por autor | 🔓 |

**Tags (planejados — read repositories já implementados na Infrastructure):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| GET | `/api/v1/catalog/tags` | Buscar tags (autocomplete) | 🔓 |
| GET | `/api/v1/catalog/tags/popular` | Tags populares | 🔓 |

**Query Params para busca de livros:**
- `searchTerm` - busca em título, autor e ISBN
- `authorSlug` - filtro por slug de autor
- `tagSlug` - filtro por slug de tag
- `minRating` - filtro por avaliação mínima (0 a 5)
- `sortBy` - `Relevance` | `Title` | `AverageRating` | `RatingsCount` | `CreatedAt`
- `sortDescending` - ordenação decrescente (true/false)
- `pageNumber`, `pageSize` - paginação

**Formato de Request (Create Book):**
```json
{
  "isbn": "9780451524935",
  "title": "1984",
  "authors": ["George Orwell"],
  "tags": ["dystopia", "classic"],
  "synopsis": "...",
  "pageCount": 328
}
```

**Formato de Request (Update Book):**
```json
{
  "title": "1984 (Edição Revisada)",
  "authors": ["George Orwell"],
  "tags": ["dystopia", "classic"],
  "synopsis": "...",
  "pageCount": 336
}
```

**Formato de Response (Book Details / Update):**
```json
{
  "bookId": "...",
  "isbn": "9780451524935",
  "title": "1984 (Edição Revisada)",
  "authors": [
    { "name": "George Orwell", "slug": "george-orwell" }
  ],
  "synopsis": "...",
  "pageCount": 336,
  "publisher": "Companhia Editora",
  "coverUrl": "https://...",
  "tags": [
    { "name": "dystopia", "slug": "dystopia" }
  ],
  "averageRating": 4.5,
  "ratingsCount": 1250,
  "updatedAt": "2026-02-09T22:00:00Z"
}
```

**Middleware:** `ExceptionHandlingMiddleware` (ValidationException → 422, NotFoundException → 404, ConflictException → 409, DomainException → 400)

### 2.4 Database Schema

```sql
-- Tabela: books ✅
CREATE TABLE books (
    id UUID PRIMARY KEY,
    isbn VARCHAR(13) NOT NULL UNIQUE,
    title VARCHAR(500) NOT NULL,
    synopsis TEXT,
    page_count INT CHECK (page_count > 0),
    publisher VARCHAR(255),
    cover_url VARCHAR(500),
    average_rating DECIMAL(3,2) NOT NULL DEFAULT 0 CHECK (average_rating >= 0 AND average_rating <= 5),
    ratings_count INT NOT NULL DEFAULT 0,
    reviews_count INT NOT NULL DEFAULT 0,
    created_by_user_id UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX ix_books_isbn ON books(isbn);
CREATE INDEX ix_books_title ON books(title);
CREATE INDEX ix_books_average_rating ON books(average_rating DESC);
CREATE INDEX ix_books_created_by_user_id ON books(created_by_user_id);

-- Tabela: authors ✅ (global registry para search/autocomplete)
CREATE TABLE authors (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL UNIQUE,
    books_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX ix_authors_slug ON authors(slug);
CREATE INDEX ix_authors_name ON authors(name);
CREATE INDEX ix_authors_books_count ON authors(books_count DESC);

-- Tabela: book_authors ✅ (N:N entre books e authors)
CREATE TABLE book_authors (
    book_id UUID NOT NULL REFERENCES books(id) ON DELETE CASCADE,
    author_id INT NOT NULL REFERENCES authors(id) ON DELETE CASCADE,
    "order" INT NOT NULL DEFAULT 0,
    added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (book_id, author_id)
);

CREATE INDEX ix_book_authors_author_id ON book_authors(author_id);
CREATE INDEX ix_book_authors_book_order ON book_authors(book_id, "order");

-- Tabela: tags ✅ (global registry para search/autocomplete)
CREATE TABLE tags (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    slug VARCHAR(50) NOT NULL UNIQUE,
    usage_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX ix_tags_slug ON tags(slug);
CREATE INDEX ix_tags_name ON tags(name);
CREATE INDEX ix_tags_usage_count ON tags(usage_count DESC);

-- Tabela: book_tags ✅ (N:N entre books e tags)
CREATE TABLE book_tags (
    book_id UUID NOT NULL REFERENCES books(id) ON DELETE CASCADE,
    tag_id INT NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
    added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (book_id, tag_id)
);

CREATE INDEX ix_book_tags_tag_id ON book_tags(tag_id);

-- Tabela: book_reviews 📋 PLANEJADO
CREATE TABLE book_reviews (
    id UUID PRIMARY KEY,
    book_id UUID NOT NULL REFERENCES books(id) ON DELETE CASCADE,
    user_id UUID NOT NULL,
    content TEXT NOT NULL CHECK (LENGTH(content) >= 10 AND LENGTH(content) <= 5000),
    rating SMALLINT CHECK (rating >= 0 AND rating <= 5),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, book_id)
);

CREATE INDEX ix_book_reviews_book_id ON book_reviews(book_id);
CREATE INDEX ix_book_reviews_user_id ON book_reviews(user_id);
```

**Diagrama de Relacionamentos:**

```
┌──────────────────────┐
│       books          │
├──────────────────────┤
│ id (PK, UUID)        │
│ isbn (unique)        │
│ title                │
│ ...                  │
└──────────────────────┘
         │                           │
         │ 1:N                       │ 1:N
         ▼                           ▼
┌──────────────────────┐    ┌──────────────────────┐
│   book_authors       │    │     book_tags        │
├──────────────────────┤    ├──────────────────────┤
│ book_id (PK, FK)     │    │ book_id (PK, FK)     │
│ author_id (PK, FK)   │    │ tag_id (PK, FK)      │
│ order                │    │ added_at             │
│ added_at             │    └──────────────────────┘
└──────────────────────┘             │
         │                           │ N:1
         │ N:1                       ▼
         ▼                  ┌──────────────────────┐
┌──────────────────────┐    │       tags           │
│      authors         │    ├──────────────────────┤
├──────────────────────┤    │ id (PK, SERIAL)      │
│ id (PK, SERIAL)      │    │ name                 │
│ name                 │    │ slug (unique)        │
│ slug (unique)        │    │ usage_count          │
│ books_count          │    │ created_at           │
│ created_at           │    └──────────────────────┘
└──────────────────────┘
```

---

## 3. Library Service 📋 PLANEJADO

### 3.1 Domínio

**Aggregates:**

```
UserBook (Aggregate Root)
├── Id: Guid
├── UserId: Guid
├── BookId: Guid
├── Status: ReadingStatus (enum)
├── CurrentProgress: int? (0-100 ou páginas)
├── CurrentProgressType: ProgressType? (enum)
├── Wishlist: bool
├── Rating: Rating? (VO, 0-5)
├── Posts: List<ReadingPost>
├── AddedAt: DateTime
└── UpdatedAt: DateTime

ReadingPost (Entity)
├── Id: Guid
├── Content: string? (max 2000)
├── Progress: Progress? (VO)
├── ReadingDate: Date
├── LikesCount: int (desnormalizado)
├── CommentsCount: int (desnormalizado)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

UserList (Aggregate Root)
├── Id: Guid
├── UserId: Guid
├── Name: string (2-50)
├── Description: string? (max 500)
├── IsPublic: bool (default false)
├── BooksCount: int (desnormalizado)
├── LikesCount: int (desnormalizado)
├── CommentsCount: int (desnormalizado)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

BookSnapshot (Read Model - não é aggregate)
├── BookId: Guid
├── Title: string
├── AuthorDisplay: string (desnormalizado: "Autor 1, Autor 2")
├── CoverUrl: string?
├── PageCount: int?
└── UpdatedAt: DateTime
```

**Enums:**
```csharp
enum ReadingStatus { NotStarted, Reading, Finished, Abandoned, Paused }
enum ProgressType { Page, Percentage }
```

**Value Objects:**
- `Rating` - inteiro 0-5
- `Progress` - value + type, validação por tipo

**Regras:**
- Status inicia como `NotStarted`
- Status muda para `Reading` ao criar post com progresso ou manualmente
- Status muda para `Finished` quando progresso = 100% ou manualmente
- Marcar `Finished` manualmente define progress = 100%
- Pode voltar de `Finished` para `Reading` (releitura)
- Livro pode estar em múltiplas listas (N:N)
- Máximo 100 listas por usuário
- Nome da lista único por usuário
- Post deve ter conteúdo OU progresso (ou ambos)

**Domain Events:**
- `BookAddedToLibraryDomainEvent`
- `BookRemovedFromLibraryDomainEvent`
- `BookStatusChangedDomainEvent`
- `UserBookRatedDomainEvent`
- `ReadingPostCreatedDomainEvent`
- `ReadingPostUpdatedDomainEvent`
- `ReadingPostDeletedDomainEvent`
- `UserListCreatedDomainEvent`
- `UserListDeletedDomainEvent`
- `BookAddedToListDomainEvent`
- `BookRemovedFromListDomainEvent`

### 3.2 API Endpoints

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| GET | `/api/v1/library` | Minha biblioteca | 🔒 |
| POST | `/api/v1/library` | Adicionar livro | 🔒 |
| PATCH | `/api/v1/library/{userBookId}` | Atualizar status/wishlist | 🔒 |
| DELETE | `/api/v1/library/{userBookId}` | Remover da biblioteca | 🔒 |
| PUT | `/api/v1/library/{userBookId}/rating` | Dar/atualizar rating | 🔒 |
| DELETE | `/api/v1/library/{userBookId}/rating` | Remover rating | 🔒 |
| GET | `/api/v1/library/{userBookId}/posts` | Listar posts | 🔒 |
| POST | `/api/v1/library/{userBookId}/posts` | Criar post | 🔒 |
| PUT | `/api/v1/library/posts/{postId}` | Editar post | 🔒 |
| DELETE | `/api/v1/library/posts/{postId}` | Excluir post | 🔒 |
| GET | `/api/v1/library/lists` | Minhas listas | 🔒 |
| POST | `/api/v1/library/lists` | Criar lista | 🔒 |
| GET | `/api/v1/library/lists/{listId}` | Detalhes da lista | 🔓 |
| PATCH | `/api/v1/library/lists/{listId}` | Atualizar lista | 🔒 |
| DELETE | `/api/v1/library/lists/{listId}` | Excluir lista | 🔒 |
| GET | `/api/v1/library/lists/{listId}/books` | Livros da lista | 🔓 |
| POST | `/api/v1/library/{userBookId}/lists` | Adicionar livro a lista | 🔒 |
| DELETE | `/api/v1/library/{userBookId}/lists/{listId}` | Remover livro da lista | 🔒 |
| GET | `/api/v1/library/lists/search` | Buscar listas públicas | 🔓 |

**Query Params para biblioteca:**
- `status` - filtro por status
- `wishlist` - true/false
- `listId` - filtro por lista
- `page`, `pageSize` - paginação

### 3.3 Database Schema

```sql
-- Enum types
CREATE TYPE reading_status AS ENUM ('not_started', 'reading', 'finished', 'abandoned', 'paused');
CREATE TYPE progress_type AS ENUM ('page', 'percentage');

-- Tabela: book_snapshots (read model do Catalog)
CREATE TABLE book_snapshots (
    book_id UUID PRIMARY KEY,
    title VARCHAR(500) NOT NULL,
    author_display VARCHAR(500) NOT NULL,
    cover_url VARCHAR(500),
    page_count INT,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Tabela: user_books
CREATE TABLE user_books (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    book_id UUID NOT NULL REFERENCES book_snapshots(book_id),
    status reading_status NOT NULL DEFAULT 'not_started',
    current_progress INT,
    current_progress_type progress_type,
    wishlist BOOLEAN NOT NULL DEFAULT FALSE,
    rating SMALLINT CHECK (rating >= 0 AND rating <= 5),
    added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, book_id),
    CHECK ((current_progress IS NULL AND current_progress_type IS NULL) OR
           (current_progress IS NOT NULL AND current_progress_type IS NOT NULL))
);

CREATE INDEX ix_user_books_user_id ON user_books(user_id);
CREATE INDEX ix_user_books_status ON user_books(user_id, status);
CREATE INDEX ix_user_books_wishlist ON user_books(user_id) WHERE wishlist = TRUE;
CREATE INDEX ix_user_books_added_at ON user_books(added_at DESC);

-- Tabela: reading_posts
CREATE TABLE reading_posts (
    id UUID PRIMARY KEY,
    user_book_id UUID NOT NULL REFERENCES user_books(id) ON DELETE CASCADE,
    content VARCHAR(2000),
    progress_value INT CHECK (progress_value >= 0),
    progress_type progress_type,
    reading_date DATE NOT NULL DEFAULT CURRENT_DATE,
    likes_count INT NOT NULL DEFAULT 0,
    comments_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CHECK (content IS NOT NULL OR progress_value IS NOT NULL),
    CHECK ((progress_value IS NULL AND progress_type IS NULL) OR
           (progress_value IS NOT NULL AND progress_type IS NOT NULL))
);

CREATE INDEX ix_reading_posts_user_book_id ON reading_posts(user_book_id);
CREATE INDEX ix_reading_posts_created_at ON reading_posts(created_at DESC);

-- Tabela: user_lists
CREATE TABLE user_lists (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    name VARCHAR(50) NOT NULL CHECK (LENGTH(name) >= 2),
    description VARCHAR(500),
    is_public BOOLEAN NOT NULL DEFAULT FALSE,
    books_count INT NOT NULL DEFAULT 0,
    likes_count INT NOT NULL DEFAULT 0,
    comments_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, name)
);

CREATE INDEX ix_user_lists_user_id ON user_lists(user_id);
CREATE INDEX ix_user_lists_public ON user_lists(is_public) WHERE is_public = TRUE;

-- Tabela: user_book_lists (N:N)
CREATE TABLE user_book_lists (
    user_book_id UUID NOT NULL REFERENCES user_books(id) ON DELETE CASCADE,
    list_id UUID NOT NULL REFERENCES user_lists(id) ON DELETE CASCADE,
    added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_book_id, list_id)
);

CREATE INDEX ix_user_book_lists_list_id ON user_book_lists(list_id);
```

---

## 4. Social Service 📋 PLANEJADO

### 4.1 Domínio

**Aggregates:**

```
Follow (Aggregate Root)
├── Id: Guid
├── FollowerId: Guid
├── FollowingId: Guid
└── CreatedAt: DateTime

PostInteractions (Aggregate Root)
├── PostId: Guid
├── PostAuthorId: Guid
├── Likes: List<PostLike>
└── Comments: List<PostComment>

PostLike (Entity)
├── Id: Guid
├── UserId: Guid
└── CreatedAt: DateTime

PostComment (Entity)
├── Id: Guid
├── UserId: Guid
├── Content: string (1-500)
└── CreatedAt: DateTime
```

**Regras:**
- Não pode seguir a si mesmo
- Follow é unique (FollowerId, FollowingId)
- Apenas seguidores podem curtir/comentar posts (ou o próprio autor)
- Qualquer usuário autenticado pode interagir com listas públicas
- Usuário só pode curtir um post/lista uma vez
- Comentários passam por filtro de conteúdo ofensivo

**Domain Events:**
- `UserFollowedDomainEvent`
- `UserUnfollowedDomainEvent`
- `PostLikedDomainEvent`
- `PostUnlikedDomainEvent`
- `PostCommentedDomainEvent`
- `CommentDeletedDomainEvent`
- `ListLikedDomainEvent`
- `ListCommentedDomainEvent`

### 4.2 API Endpoints

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| POST | `/api/v1/social/follows` | Seguir usuário | 🔒 |
| DELETE | `/api/v1/social/follows/{userId}` | Deixar de seguir | 🔒 |
| GET | `/api/v1/social/users/{userId}/followers` | Listar seguidores | 🔓 |
| GET | `/api/v1/social/users/{userId}/following` | Listar seguindo | 🔓 |
| GET | `/api/v1/social/feed` | Feed de atividades | 🔒 |
| GET | `/api/v1/social/discover` | Descobrir livros | 🔒 |
| POST | `/api/v1/social/posts/{postId}/likes` | Curtir post | 🔒 |
| DELETE | `/api/v1/social/posts/{postId}/likes` | Descurtir post | 🔒 |
| GET | `/api/v1/social/posts/{postId}/comments` | Listar comentários | 🔒 |
| POST | `/api/v1/social/posts/{postId}/comments` | Comentar post | 🔒 |
| POST | `/api/v1/social/lists/{listId}/likes` | Curtir lista | 🔒 |
| DELETE | `/api/v1/social/lists/{listId}/likes` | Descurtir lista | 🔒 |
| GET | `/api/v1/social/lists/{listId}/comments` | Listar comentários | 🔓 |
| POST | `/api/v1/social/lists/{listId}/comments` | Comentar lista | 🔒 |
| DELETE | `/api/v1/social/comments/{commentId}` | Excluir comentário | 🔒 |

### 4.3 Database Schema

```sql
-- Enum type
CREATE TYPE interactable_type AS ENUM ('post', 'list');
CREATE TYPE feed_item_type AS ENUM ('reading_post', 'review', 'book_added', 'rating');

-- Tabela: follows
CREATE TABLE follows (
    id UUID PRIMARY KEY,
    follower_id UUID NOT NULL,
    following_id UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (follower_id, following_id),
    CHECK (follower_id != following_id)
);

CREATE INDEX ix_follows_follower_id ON follows(follower_id);
CREATE INDEX ix_follows_following_id ON follows(following_id);

-- Tabela: likes (polimórfica)
CREATE TABLE likes (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    target_type interactable_type NOT NULL,
    target_id UUID NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, target_type, target_id)
);

CREATE INDEX ix_likes_target ON likes(target_type, target_id);
CREATE INDEX ix_likes_user_id ON likes(user_id);

-- Tabela: comments (polimórfica)
CREATE TABLE comments (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    target_type interactable_type NOT NULL,
    target_id UUID NOT NULL,
    content VARCHAR(500) NOT NULL CHECK (LENGTH(content) >= 1),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_comments_target ON comments(target_type, target_id);
CREATE INDEX ix_comments_target_created ON comments(target_type, target_id, created_at DESC);
CREATE INDEX ix_comments_user_id ON comments(user_id);

-- Tabela: feed_items (desnormalizada para performance)
CREATE TABLE feed_items (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    item_type feed_item_type NOT NULL,
    reference_id UUID NOT NULL,
    book_id UUID NOT NULL,
    user_name VARCHAR(100) NOT NULL,
    user_avatar_url VARCHAR(500),
    book_title VARCHAR(500) NOT NULL,
    book_cover_url VARCHAR(500),
    data JSONB,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX ix_feed_items_user_created ON feed_items(user_id, created_at DESC);
CREATE INDEX ix_feed_items_created_at ON feed_items(created_at DESC);

-- Tabela: user_tag_preferences (para descoberta)
CREATE TABLE user_tag_preferences (
    user_id UUID NOT NULL,
    tag_slug VARCHAR(50) NOT NULL,
    score DECIMAL(5,2) NOT NULL DEFAULT 0,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, tag_slug)
);
```

---

## 5. Comunicação Entre Serviços 📋 PLANEJADO

### 5.1 Eventos de Integração

```
┌──────────┐                    ┌──────────┐
│ Identity │───UserDeleted────▶│ Catalog  │ (atualiza created_by para "Usuário Removido")
│          │───UserDeleted────▶│ Library  │ (deleta user_books, user_lists)
│          │───UserDeleted────▶│ Social   │ (deleta follows, likes, comments, feed)
└──────────┘                    └──────────┘

┌──────────┐                    ┌──────────┐
│ Catalog  │───BookCreated────▶│ Library  │ (cria book_snapshot)
│          │───BookUpdated────▶│ Library  │ (atualiza book_snapshot)
│          │───ReviewCreated──▶│ Social   │ (adiciona ao feed)
└──────────┘                    └──────────┘

┌──────────┐                    ┌──────────┐
│ Library  │───PostCreated────▶│ Social   │ (adiciona ao feed)
│          │───BookRated──────▶│ Catalog  │ (recalcula average_rating)
│          │───BookRated──────▶│ Social   │ (adiciona ao feed, atualiza preferências)
│          │───BookAdded──────▶│ Social   │ (adiciona ao feed)
└──────────┘                    └──────────┘

┌──────────┐                    ┌──────────┐
│ Social   │───PostLiked──────▶│ Library  │ (incrementa likes_count)
│          │───PostCommented──▶│ Library  │ (incrementa comments_count)
│          │───ListLiked──────▶│ Library  │ (incrementa likes_count)
│          │───ListCommented──▶│ Library  │ (incrementa comments_count)
└──────────┘                    └──────────┘
```

### 5.2 Contratos de Eventos

```csharp
// Identity → Todos
record UserDeletedIntegrationEvent(Guid UserId, DateTime DeletedAt);

// Catalog → Library
record BookCreatedIntegrationEvent(
    Guid BookId,
    string Title,
    List<string> Authors,  // Nomes dos autores
    string AuthorDisplay,  // "Autor 1, Autor 2"
    string? CoverUrl,
    int? PageCount
);

record BookUpdatedIntegrationEvent(
    Guid BookId,
    string Title,
    List<string> Authors,
    string AuthorDisplay,
    string? CoverUrl,
    int? PageCount
);

// Library → Catalog
record UserBookRatedIntegrationEvent(
    Guid BookId,
    Guid UserId,
    int Rating,
    int? PreviousRating
);

// Library → Social
record ReadingPostCreatedIntegrationEvent(
    Guid PostId,
    Guid UserId,
    Guid BookId,
    string? Content,
    int? Progress,
    DateTime CreatedAt
);

// Social → Library
record ContentLikedIntegrationEvent(
    string TargetType, // "post" | "list"
    Guid TargetId,
    Guid UserId
);

record ContentCommentedIntegrationEvent(
    string TargetType,
    Guid TargetId,
    Guid CommentId,
    Guid UserId
);
```

---

## 6. Padrões e Convenções

### 6.1 Estrutura de Projeto

```
Legi.SharedKernel/
├── BaseEntity.cs
├── BaseAuditableEntity.cs
├── ValueObject.cs
├── IDomainEvent.cs
├── DomainException.cs
└── Mediator/
    ├── IMediator.cs
    ├── Mediator.cs
    ├── IRequest.cs
    ├── IRequestHandler.cs
    ├── IPipelineBehavior.cs
    ├── RequestHandlerDelegate.cs
    └── Unit.cs

Legi.{Service}.Domain/
├── Entities/
├── ValueObjects/
├── Enums/
├── Events/
├── Exceptions/
├── Repositories/
└── Common/

Legi.{Service}.Application/
├── {Feature}/
│   ├── Commands/
│   │   └── {Command}/
│   │       ├── {Command}Command.cs
│   │       ├── {Command}CommandHandler.cs
│   │       ├── {Command}CommandValidator.cs
│   │       └── {Command}Response.cs
│   └── Queries/
│       └── {Query}/
│           ├── {Query}Query.cs
│           ├── {Query}QueryHandler.cs
│           └── {Query}Response.cs
├── Common/
│   ├── Behaviors/
│   ├── Exceptions/
│   └── Interfaces/
└── DependencyInjection.cs

Legi.{Service}.Infrastructure/
├── Persistence/
│   ├── Configurations/
│   ├── Entities/          (persistence entities, separados do domínio)
│   ├── Migrations/
│   └── Repositories/
├── Security/
├── ExternalServices/
└── DependencyInjection.cs

Legi.{Service}.Api/
├── Controllers/
├── Middleware/
└── Program.cs
```

### 6.2 Formato de Resposta de Erro

```json
{
    "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
    "title": "Validation Error",
    "status": 400,
    "detail": "One or more validation errors occurred.",
    "errors": {
        "Email": ["Email is required"],
        "Password": ["Password must be at least 8 characters"]
    }
}
```

### 6.3 Formato de Paginação

```json
{
    "items": [...],
    "pagination": {
        "page": 1,
        "pageSize": 20,
        "totalItems": 150,
        "totalPages": 8,
        "hasNext": true,
        "hasPrevious": false
    }
}
```

### 6.4 HTTP Status Codes

| Código | Uso |
|--------|-----|
| 200 | Sucesso |
| 201 | Recurso criado |
| 204 | Sucesso sem corpo |
| 400 | Erro de validação / domínio |
| 401 | Não autenticado |
| 403 | Não autorizado |
| 404 | Não encontrado |
| 409 | Conflito (duplicado) |
| 422 | Entidade não processável |
| 429 | Rate limit |
| 500 | Erro interno |

---

## 7. Resumo de Endpoints

| Serviço | Endpoints | Status |
|---------|-----------|--------|
| Identity | 8 | ✅ Implementado |
| Catalog | 16 (books: 6, reviews: 4, authors: 4, tags: 2) | 🔧 5/16 implementado |
| Library | 19 | 📋 Planejado |
| Social | 15 | 📋 Planejado |
| **Total** | **58** | |

## 8. Resumo de Tabelas

| Serviço | Tabelas | Status |
|---------|---------|--------|
| Identity | 2 (users, refresh_tokens) | ✅ Migrado |
| Catalog | 6 (books, authors, book_authors, tags, book_tags, book_reviews) | 🔧 5/6 migrado (sem book_reviews) |
| Library | 5 (book_snapshots, user_books, reading_posts, user_lists, user_book_lists) | 📋 Planejado |
| Social | 5 (follows, likes, comments, feed_items, user_tag_preferences) | 📋 Planejado |
| **Total** | **18** | |
