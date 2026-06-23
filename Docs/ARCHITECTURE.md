# Legi - Arquitetura do Sistema

Sistema de gerenciamento pessoal de leitura com recursos sociais.

## Status de Implementação

| Serviço | Status | Observação |
|---------|--------|------------|
| **SharedKernel** | ✅ Implementado | Base classes, custom Mediator |
| **Identity** | ✅ Implementado | Auth completa (e-mail/senha + login social Google), perfil de usuário |
| **Catalog** | ✅ Implementado | CRUD de livros, cadastro manual protegido, busca/autocomplete de autores e tags, JWT auth integrado |
| **Library** | ✅ Implementado | Domain ✅, Application ✅, Infrastructure ✅, Api ✅ |
| **Web Frontend** | 🚧 Em desenvolvimento | React 19 + Vite 8 + Tailwind CSS v4, Docker/Nginx, integração API progressiva |
| **Social** | ✅ Implementado | Domain ✅, Application ✅, Infrastructure ✅, Api ✅ |

## Stack Tecnológica

| Camada         | Tecnologia                                                               |
|----------------|--------------------------------------------------------------------------|
| Backend        | .NET 10, ASP.NET Core                                                    |
| Frontend       | React 19 + TypeScript + Vite 8 + Tailwind CSS v4 + i18next              |
| Banco de Dados | PostgreSQL (db separado por serviço)                                     |
| Mensageria     | RabbitMQ — outbox/inbox, at-least-once + idempotência (Fases 1–4)         |
| API Gateway    | Nginx no frontend Docker como proxy reverso para `/api/v1/*`             |
| API Externa    | Open Library + Google Books API (integração ativa no Catalog/CreateBook) |
| Mediator       | Custom (`Legi.SharedKernel.Mediator` — sem dependência MediatR)          |
| Validação      | FluentValidation                                                         |
| ORM            | Entity Framework Core 10 + Npgsql                                        |
| Auth           | JWT Bearer (RS256 assimétrico) + BCrypt + MFA (TOTP / e-mail) + Google Sign-In |
| Testes         | xUnit + coverlet                                                         |

## Bounded Contexts

```
┌─────────────────────────────────────────────────────────────────┐
│                     Web Frontend (nginx:3000)                    │
│              React 19 + Vite 8 + Tailwind CSS v4                │
│       Reverse proxy: /api/v1/{service}/ → {service}-api         │
└──────────┬──────────┬──────────┬──────────┬────────────────────┘
           │          │          │          │
           ▼          ▼          ▼          ▼
     ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
     │ Identity │ │ Catalog  │ │ Library  │ │  Social  │
     │ Service  │ │ Service  │ │ Service  │ │ Service  │
     │    ✅    │ │    ✅    │ │    ✅    │ │    ✅    │
     └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘
          │            │            │            │
          ▼            ▼            ▼            ▼
     ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
     │ identity │ │ catalog  │ │ library  │ │  social  │
     │  db:5432 │ │  db:5433 │ │  db:5434 │ │  db:5435 │
     └──────────┘ └──────────┘ └──────────┘ └──────────┘

┌──────────────────────────────────────────────────────────────────┐
│                  RabbitMQ (5672, management 15672)               │
│       Async integration via outbox/inbox + integration events     │
└──────────────────────────────────────────────────────────────────┘

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
| **Identity** | Autenticação, refresh tokens, perfil básico e ciclo de vida do usuário | Suporte |
| **Catalog** | Catálogo global de livros, autores, tags, snapshots e dados externos | Core |
| **Library** | Biblioteca pessoal, wishlist, progresso de leitura, posts e listas | Core |
| **Social** | Perfis sociais, follows, feed, likes, comments e projeções sociais | Core |
| **Web Frontend** | SPA React e proxy reverso nginx para `/api/v1/*` | Apresentação |

### Limites e integrações

- Cada contexto backend possui seu próprio banco PostgreSQL e aplica migrations no startup.
- `Legi.Contracts` define os integration events compartilhados entre contextos.
- `Legi.Messaging` implementa outbox/inbox, RabbitMQ publisher/consumer e idempotência.
- `SharedKernel` contém apenas abstrações transversais de domínio e mediator; regras de negócio permanecem em seus contextos.
- O frontend não é fonte de regra de negócio: consome as APIs e roteia `/api/v1/{identity|catalog|library|social}` via Nginx.

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
- `INotification` / `INotificationHandler<T>` — Publish/subscribe para domain events
- `Unit` — Tipo void para C#

**Pagination:**
- `CursorPaginatedList<T>` — Tipo compartilhado disponível para paginação cursor-based futura (Items, NextCursor, HasMore, PageSize). Os feeds atuais do Social usam paginação offset com `PaginatedList<T>`.

---

## 1. Identity Service ✅

### 1.1 Domínio

**Aggregates:**

```
User (Aggregate Root)
├── Id: Guid
├── Email: Email (VO)
├── Username: Username (VO)
├── PasswordHash: string?        (nullable — contas só-Google não têm senha)
├── EmailConfirmedAt: DateTime?
├── MfaEnabled: bool             ← MFA ativo?
├── MfaMethod: MfaMethod         (None / Totp / Email)
├── TotpSecret: string?          (segredo TOTP criptografado em repouso; null no método e-mail)
├── MfaEnabledAt: DateTime?
├── MfaRecoveryCodes: List<MfaRecoveryCode>   (códigos de recuperação, valem p/ ambos os métodos)
├── RefreshTokens: List<RefreshToken>
├── ExternalLogins: List<ExternalLogin>
├── CreatedAt: DateTime
└── UpdatedAt: DateTime
   (métodos MFA: StartMfaEnrollment/ConfirmMfaEnrollment p/ TOTP, EnableEmailMfa p/ e-mail, DisableMfa, TryConsumeRecoveryCode)

RefreshToken (Entity)
├── Id: Guid
├── TokenHash: string
├── ExpiresAt: DateTime
├── CreatedAt: DateTime
└── RevokedAt: DateTime?

ExternalLogin (Entity)        ← login social (ex.: Google)
├── Id: Guid
├── Provider: string          (ex.: "google")
├── ProviderKey: string       (claim `sub` do provedor, estável)
└── CreatedAt: DateTime
   (chave única: provider + provider_key)

MfaEmailCode (Entity/tabela própria — estado transitório, fora do agregado User)
├── Id: Guid
├── UserId: Guid
├── CodeHash: string          (hash do código; o texto nunca é persistido)
├── ExpiresAt: DateTime
├── AttemptCount: int         (limite MaxAttempts = 5 → invalida)
└── ConsumedAt: DateTime?     (uso único; 1 código ativo por usuário)
```

**Value Objects:**
- `Email` - formato válido, normalizado para lowercase, único
- `Username` - 3-30 chars, lowercase, letras/números/underscore, começa com letra

**Regras:**
- Máximo 5 refresh tokens ativos por usuário (LRU eviction)
- Refresh tokens são opacos, gerados com 64 bytes aleatórios, persistidos somente como hash SHA-256 e entregues em cookie `HttpOnly`
- Refresh token rotation em todo `/auth/refresh`: o token atual é revogado e substituído por um novo token
- Reuso de refresh token já revogado é tratado como replay e revoga todos os refresh tokens ativos do usuário
- Password: mínimo 8 chars, 1 maiúscula, 1 número
- Ao trocar senha, todos refresh tokens são revogados
- Logout revoga o refresh token atual; deleção de conta remove os tokens por cascade
- **Login social (Google):** usuário pode ser criado sem senha (`PasswordHash` null) via `User.CreateFromExternalLogin(...)` — e-mail já confirmado (Google verifica). `AddExternalLogin(...)` vincula um provedor a uma conta existente (idempotente). Login por senha é rejeitado para contas sem senha.

**Domain Events:**
- `UserRegisteredDomainEvent`
- `UserProfileUpdatedDomainEvent`
- `UserDeletedDomainEvent`

### 1.2 Application

**Auth Commands:** `RegisterCommand`, `LoginCommand`, `GoogleSignInCommand`, `RefreshTokenCommand`, `LogoutCommand`, `ForgotPasswordCommand`, `ResetPasswordCommand`, `ConfirmEmailCommand`, `ResendConfirmationCommand`
**MFA Commands:** `BeginMfaSetupCommand`/`ConfirmMfaSetupCommand` (TOTP), `BeginEmailMfaSetupCommand`/`ConfirmEmailMfaSetupCommand` (e-mail), `SendMfaEmailCodeCommand` (envio/reenvio no login), `CompleteMfaLoginCommand` (segundo fator no login), `DisableMfaCommand`
**User Commands:** `DeleteAccountCommand`
**User Queries:** `GetCurrentUserQuery`, `GetPublicProfileQuery`
**Behaviors:** `ValidationBehavior`, `LoggingBehavior`, `UnhandledExceptionBehavior`
**Interfaces:** `IJwtTokenService`, `IPasswordHasher`, `IGoogleTokenValidator`, `ITotpService`, `IMfaSecretProtector`, `IMfaEmailCodeRepository`, `ISecureTokenFactory`, `IEmailSender`, `ISecurityAuditLogger`, `IBreachedPasswordChecker`
**Helpers:** `UsernameGenerator` (gera username único a partir do e-mail/nome do Google), `MfaRecoveryCodeGenerator` (10 códigos de recuperação), `MfaEmailCodeGenerator` (código numérico de 6 dígitos)
**Exceptions:** `ConflictException`, `NotFoundException`, `UnauthorizedException`

> **Fluxo Google Sign-In:** `GoogleSignInCommand` recebe o ID token do Google (botão GIS no frontend). O handler valida o token e resolve o usuário em cascata: (1) por `ExternalLogin(google, sub)`; senão (2) por e-mail verificado → **vincula** automaticamente e confirma o e-mail; senão (3) cria conta nova sem senha com username gerado. Um único endpoint serve cadastro **e** login.

> **Fluxo MFA (dois fatores):** o usuário ativa **um** método — **TOTP** (app autenticador, forte, segredo no dispositivo) **ou** **E-mail** (código de uso único enviado ao e-mail, menos atrito, mais fraco — o código cai na mesma caixa que controla o reset de senha). O método ativo fica em `User.MfaMethod` (`None`/`Totp`/`Email`); os códigos de recuperação valem para ambos. No login, após a senha, se `MfaEnabled` o handler retorna um **challenge token** (audience `{Audience}:mfa`, não serve como access token) em vez dos tokens; o cliente troca o challenge + código em `auth/mfa-login` (`CompleteMfaLoginCommand`). Para o método de e-mail, `auth/mfa-email/send` emite/reenvia o código (gated pelo challenge token). Códigos de e-mail são curtos por design — a segurança vem do **TTL curto + limite de tentativas + uso único** (tabela `mfa_email_codes`), não do tamanho. Detalhes/decisão: `Docs/IDENTITY-FEATURE-email-mfa.md`.

> **Ciclo de refresh tokens:** login, Google sign-in e conclusão de MFA emitem um refresh token novo. O refresh usa `RotateRefreshTokenAsync` em transação com `SELECT ... FOR UPDATE`: valida o hash atual, rejeita expirado/e-mail não confirmado, revoga o token corrente e cria um novo. Se o token encontrado já estava revogado, o sistema considera replay e chama `RevokeAllRefreshTokens()`. Também revogamos todos os tokens ativos em reset/troca de senha e quando um Google Sign-In reivindica uma conta local ainda não confirmada; ao adicionar uma sessão acima do limite de 5, o token ativo mais antigo é revogado. Tokens expirados não são marcados como revogados: eles apenas deixam de ser ativos (`IsExpired`).

### 1.3 Infrastructure

- `IdentityDbContext` — EF Core + PostgreSQL (porta 5432)
- `UserRepository` — Implementa `IUserRepository`
- `JwtTokenService` — Gera access token (JWT, **RS256 / RSA assimétrico**) + refresh token (64 bytes Base64) + **challenge token de MFA** (audience distinta `{Audience}:mfa`, ~5 min)
- `PasswordHasher` — BCrypt hash/verify
- `JwtSettings` — Options pattern (**`PublicKey`/`PrivateKey`** em PEM ou base64-PEM, Issuer, Audience, expirations). `CreatePublicSigningKey()` importa a chave RSA; validação fixa `ValidAlgorithms = [RsaSha256]` (bloqueia alg-confusion / `alg:none`)
- `GoogleTokenValidator` — valida o ID token do Google (assinatura/issuer/audience/expiry) via `Google.Apis.Auth`
- `GoogleAuthSettings` — Options pattern (`ClientId`); sem `ValidateOnStart`, então a app sobe mesmo sem configurar (Google sign-in fica desativado)
- **MFA:** `TotpService` (RFC 6238, HMAC-SHA1, 30s, 6 dígitos, Base32), `AesMfaSecretProtector` (AES-256-GCM, criptografa o segredo TOTP em repouso), `MfaSettings` (`EncryptionKey` base64-32B, `Issuer`, `EmailCodeLifetimeMinutes`), `MfaEmailCodeRepository`
- `SecureTokenFactory` (gera/hash de tokens e códigos), `SmtpEmailSender` (`IEmailSender` via MailKit; faz fallback para log quando SMTP não configurado), `SecurityAuditLogger` (log estruturado de eventos de segurança)

> **Chaves JWT (RS256):** os access tokens são assinados com RSA. **Só o Identity tem a chave privada e emite tokens**; os demais serviços recebem apenas a `PublicKey` e somente validam. Logo, um comprometimento de Catalog/Library/Social **não** permite forjar tokens. Rotação: novo par de chaves; access tokens expiram em `AccessTokenExpirationMinutes` (15) e refresh tokens são opacos, então sobrevivem à troca. (Migrado de HMAC-SHA256 simétrico nesta linha de endurecimento — ver `Docs/DEPLOYMENT-hardening.md` §5.)

### 1.4 API Endpoints

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| POST | `/api/v1/identity/auth/register` | Registrar usuário | - |
| POST | `/api/v1/identity/auth/login` | Login (email ou username) — retorna challenge se MFA | - |
| POST | `/api/v1/identity/auth/mfa-login` | Completa o login com o segundo fator (TOTP/e-mail/recuperação) | - |
| POST | `/api/v1/identity/auth/mfa-email/send` | Envia/reenvia o código de e-mail no login (requer challenge) | - |
| POST | `/api/v1/identity/auth/google` | Login/cadastro com ID token do Google | - |
| POST | `/api/v1/identity/auth/refresh` | Renovar token | - |
| POST | `/api/v1/identity/auth/logout` | Logout | 🔒 |
| POST | `/api/v1/identity/mfa/setup` · `/confirm` | Ativar MFA por **TOTP** (QR + confirmação) | 🔒 |
| POST | `/api/v1/identity/mfa/email/setup` · `/email/confirm` | Ativar MFA por **e-mail** (envia código + confirmação) | 🔒 |
| POST | `/api/v1/identity/mfa/disable` | Desativar MFA (exige código atual ou de recuperação) | 🔒 |
| GET | `/api/v1/identity/users/me` | Meu perfil (inclui `MfaEnabled`/`MfaMethod`) | 🔒 |
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
    password_hash VARCHAR(255),          -- nullable: contas só-Google não têm senha
    email_confirmed_at TIMESTAMPTZ,
    mfa_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    mfa_method VARCHAR(16) NOT NULL DEFAULT 'None',   -- None | Totp | Email
    totp_secret VARCHAR(512),            -- segredo TOTP criptografado (AES-256-GCM); null no método e-mail
    mfa_enabled_at TIMESTAMPTZ,
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

-- Tabela: external_logins (login social — ex.: Google)
CREATE TABLE external_logins (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    provider VARCHAR(50) NOT NULL,
    provider_key VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX ix_external_logins_provider_provider_key ON external_logins(provider, provider_key);
CREATE INDEX ix_external_logins_user_id ON external_logins(user_id);

-- Tabela: mfa_recovery_codes (filhos do agregado User; valem p/ TOTP e e-mail)
CREATE TABLE mfa_recovery_codes (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    code_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    used_at TIMESTAMPTZ
);

-- Tabela: mfa_email_codes (estado transitório, como login_attempts — sem FK p/ users)
-- 1 código ativo por usuário (índice único); o texto nunca é persistido.
CREATE TABLE mfa_email_codes (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    code_hash VARCHAR(255) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    attempt_count INT NOT NULL DEFAULT 0,
    consumed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX ix_mfa_email_codes_user_id ON mfa_email_codes(user_id);
```

> **Nota:** o schema acima reflete a tabela `users` simplificada (perfil rico — name/bio/avatar — vive no Social via `UserProfile`). Lockout de login e tokens de reset/confirmação omitidos por brevidade.

---

## 2. Catalog Service ✅

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
- AverageRating recalculado quando ratings mudam (consome `UserBookRated` do Library)
- `ReviewsCount` mantido por incremento/decremento ao consumir `ReviewCreated`/`ReadingPostDeleted` (resenha) do Library — as resenhas em si vivem no Library/Social (ver §3.1 e §5), o Catalog guarda apenas o contador

**Domain Events:**
- `BookCreatedDomainEvent` ✅ (com lista de autores)
- `BookTagsUpdatedDomainEvent` ✅
- `BookRatingRecalculatedDomainEvent` ✅

> **Nota:** o modelo de review *Catalog-owned* originalmente planejado (`book_reviews`, eventos `ReviewCreated/Updated/Deleted` no Catalog, endpoints `/catalog/.../reviews`) foi **abandonado**. Resenhas são `ReadingProgress` marcados no Library e projetadas no feed do Social (decisão em §3.1). O Catalog só expõe `Book.AverageRating`/`RatingsCount`/`ReviewsCount` (métodos `IncrementReviewsCount`/`DecrementReviewsCount`).

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
- `CreateBookCommand` ✅ — Cadastro manual protegido de livro com ISBN, título, autores, sinopse, editora, capa, páginas e tags obrigatórios
- `UpdateBookCommand` ✅ — Atualiza dados básicos, autores e tags de um livro
- `DeleteBookCommand` ✅ — Remove livro do catálogo

**Queries implementadas:**
- `SearchBooksQuery` ✅ — Busca com filtros, paginação, sorting (`BookSortBy`: Relevance, Title, AverageRating, RatingsCount, CreatedAt)
- `GetBookDetailsQuery` ✅ — Detalhes completos por ID
- `SearchAuthorsQuery` ✅ — Busca de autores por prefixo (autocomplete)
- `GetPopularAuthorsQuery` ✅ — Autores mais populares por contagem de livros
- `SearchTagsQuery` ✅ — Busca de tags por prefixo (autocomplete)
- `GetPopularTagsQuery` ✅ — Tags mais populares por contagem de uso

**DTOs:** `BookSummaryDto`, `AuthorDto`, `TagDto`, `PaginationMetadata`, `CreateBookRequest`, `CreateBookResponse`, `UpdateBookResponse`, `GetBookDetailsResponse`, `AuthorResult`, `TagResult`
**Behaviors:** `ValidationBehavior`, `LoggingBehavior`
**Exceptions:** `ConflictException` (com extensões para `ProblemDetails`), `NotFoundException`
**Porta externa (Application):** `IBookDataProvider` (retorna `ExternalBookData`)

**Repositories (Domain interfaces):**
- `IBookRepository` ✅ (write: Add, Update, Delete, GetById, GetByIsbn)
- `IBookReadRepository` ✅ (read: Search, GetDetailsById, GetDetailsByIsbn)
- `IAuthorReadRepository` ✅ (Search, GetPopular, GetBySlug)
- `ITagReadRepository` ✅ (Search, GetPopular)

**Criação/importação compartilhada de livros:**
- `BookImportService` centraliza a criação de livros para o cadastro manual (`CreateBookCommandHandler`) e para a importação de candidatos externos (`ProcessExternalBookSearchJobCommandHandler`).
- Cadastro manual exige ISBN válido, título, pelo menos um autor, sinopse, editora, URL de capa HTTP/HTTPS, número de páginas positivo e pelo menos uma tag.
- Duplicidade manual por ISBN ou por título + primeiro autor retorna `409 Conflict` com `existingBookId` nas extensões do `ProblemDetails`, permitindo que o frontend navegue para o livro existente.
- Importação externa mantém o comportamento de reaproveitar/enriquecer livros existentes e criar aliases para candidatos externos, sem duplicar a persistência.
- Ambos os caminhos criam/atualizam pelo aggregate `Book`, preservando domain events, outbox e projeções consumidoras como Library/Social snapshots via `BookCreatedIntegrationEvent`.

**Integração externa de metadados:**
- `BookImportService` usa `IBookDataProvider` para enriquecer dados por ISBN quando o fluxo é de importação externa ou quando há metadados externos úteis.
- Cadeia de fallback na Infrastructure: `OpenLibrary` (prioridade 1) → `GoogleBooks` (prioridade 2).
- Regra de merge: dados explícitos do request/candidato têm prioridade; APIs externas preenchem lacunas apenas nos fluxos em que lacunas são permitidas.
- Falhas de provedores externos não interrompem o fluxo (logging + fallback); só falha quando título/autores continuam ausentes após merge.
- Componentes de infraestrutura:
    - `BookDataProvider` (orquestrador da cadeia de provedores)
    - `IExternalBookClient` (contrato interno para clientes externos com prioridade)
    - `OpenLibraryClient` + `OpenLibraryMapper` + `OpenLibrarySettings`
    - `GoogleBooksClient` + `GoogleBooksMapper` + `GoogleBooksSettings`
- Registro de DI: `AddExternalBookServices(configuration)` em `Legi.Catalog.Infrastructure`.
- Configuração em `appsettings*.json`:
    - `ExternalServices:OpenLibrary` (`Enabled`, `TimeoutSeconds`)
    - `ExternalServices:GoogleBooks` (`Enabled`, `TimeoutSeconds`, `ApiKey`)
- Referência de decisão técnica detalhada: `Docs/CATALOG-ARCHITECTURE-external-apis.md`.

### 2.3 API Endpoints

**Books (implementados):**

| Método | Endpoint | Descrição | Auth | Status |
|--------|----------|-----------|------|--------|
| GET | `/api/v1/catalog/books` | Buscar livros | 🔓 | ✅ |
| GET | `/api/v1/catalog/books/{bookId}` | Detalhes do livro | 🔓 | ✅ |
| POST | `/api/v1/catalog/books` | Cadastrar livro | 🔒 JWT | ✅ |
| PUT | `/api/v1/catalog/books/{bookId}` | Atualizar livro | 🔒 JWT | ✅ |
| DELETE | `/api/v1/catalog/books/{bookId}` | Excluir livro | 🔒 JWT | ✅ |

**Authors (implementados):**

| Método | Endpoint | Descrição | Auth | Status |
|--------|----------|-----------|------|--------|
| GET | `/api/v1/catalog/authors/search` | Buscar autores (autocomplete) | 🔓 | ✅ |
| GET | `/api/v1/catalog/authors/popular` | Autores populares | 🔓 | ✅ |

**Tags (implementados):**

| Método | Endpoint | Descrição | Auth | Status |
|--------|----------|-----------|------|--------|
| GET | `/api/v1/catalog/tags/search` | Buscar tags (autocomplete) | 🔓 | ✅ |
| GET | `/api/v1/catalog/tags/popular` | Tags populares | 🔓 | ✅ |

**Authors (planejados):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| GET | `/api/v1/catalog/authors/{slug}` | Detalhes do autor | 🔓 |
| GET | `/api/v1/catalog/authors/{slug}/books` | Livros por autor | 🔓 |

**Reviews (~~planejados~~ ABANDONADOS no Catalog):** resenhas **não** são servidas pelo Catalog. Escrita: `POST /api/v1/library/{userBookId}/reviews` (Library). Leitura por livro: `GET /api/v1/social/books/{bookId}/reviews` (Social). O Catalog só expõe `ReviewsCount` em `GET /api/v1/catalog/books/{bookId}`. Ver §3 (Library) e §5 (Social).

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
  "publisher": "Secker & Warburg",
  "coverUrl": "https://example.com/covers/1984.jpg",
  "pageCount": 328,
  "tags": ["dystopia", "classic"],
  "synopsis": "..."
}
```

Obs.: no cadastro manual, todos os campos acima são obrigatórios. Strings em branco, ISBN inválido, URL de capa inválida, `pageCount <= 0` e listas vazias de autores/tags são rejeitados pela validação da Application/API.

**Formato de Response (Create Book - duplicado 409):**
```json
{
  "type": "about:blank",
  "title": "Conflict",
  "status": 409,
  "detail": "A book with this ISBN already exists.",
  "existingBookId": "..."
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

**Autenticação:** JWT Bearer (mesma config do Identity Service — `JwtSettings` compartilhado). Endpoints de escrita (POST, PUT, DELETE) requerem `[Authorize]`. Endpoints de leitura (GET) são públicos. UserId extraído do claim `sub` do JWT.

**Middleware:** `ExceptionHandlingMiddleware` (ValidationException → 422, NotFoundException → 404, ConflictException → 409 com extensões em `ProblemDetails`, DomainException → 400, UnauthorizedAccessException → 401)

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

-- Tabela: book_reviews ❌ ABANDONADA — resenhas vivem no Library (reading_posts.is_review)
--   e são projetadas no feed do Social. O Catalog mantém apenas books.reviews_count.
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

## 3. Library Service ✅

Decisões de arquitetura detalhadas em `Docs/LIBRARY-ARCHITECTURE-decisions.md`.

### 3.1 Domínio ✅

**Aggregates:**

```
UserBook (Aggregate Root) ✅
├── Id: Guid
├── UserId: Guid
├── BookId: Guid
├── Status: ReadingStatus (enum)
├── CurrentProgress: Progress? (VO)
├── WishList: bool
├── CurrentRating: Rating? (VO, 1-10 meias-estrelas)
├── DeletedAt: DateTime? (soft delete)
├── IsDeleted: bool (computed)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

ReadingProgress (Aggregate Root — promovido de entity filha) ✅
├── Id: Guid
├── UserBookId: Guid (referência por ID)
├── UserId: Guid (desnormalizado para feed/queries)
├── BookId: Guid (desnormalizado para feed/queries)
├── Content: string? (max 2000, constante: MaxContentLength; resenha: min MinReviewContentLength = 20)
├── IsSpoiler: bool (default false; metadado de visibilidade no feed)
├── IsReview: bool (default false; distingue resenha de registro de progresso)
├── Rating: Rating? (VO; snapshot da nota no momento da resenha — null em posts de progresso)
├── CurrentProgress: Progress? (VO; sempre null em resenhas)
├── ReadingDate: DateOnly
├── LikesCount: int (desnormalizado, fonte: Social)
├── CommentsCount: int (desnormalizado, fonte: Social)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

UserList (Aggregate Root) ✅
├── Id: Guid
├── UserId: Guid
├── Name: string (2-50, constantes: MinNameLength, MaxNameLength)
├── Description: string? (max 500, constante: MaxDescriptionLength)
├── IsPublic: bool (default false)
├── Items: List<UserListItem> (entity filha)
├── BooksCount: int (desnormalizado)
├── LikesCount: int (desnormalizado, fonte: Social)
├── CommentsCount: int (desnormalizado, fonte: Social)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

UserListItem (Entity — filha de UserList) ✅
├── Id: Guid
├── BookId: Guid          (Catalog BookId — desacoplado do UserBook desde a feature de listas)
├── Order: int
└── AddedAt: DateTime

BookSnapshot (Read Model — não é aggregate) ✅
├── BookId: Guid
├── Title: string
├── AuthorDisplay: string (desnormalizado: "Autor 1, Autor 2")
├── CoverUrl: string?
├── PageCount: int?
└── UpdatedAt: DateTime
```

> **⚠️ TODO (agora DESBLOQUEADO — pronto para remover): workaround de criação inline do BookSnapshot**
>
> Atualmente, o `AddBookToLibraryCommand` aceita campos opcionais (`BookTitle`, `BookAuthorDisplay`, `BookCoverUrl`, `BookPageCount`) e cria o `BookSnapshot` inline quando ele não existe no banco do Library. Era um **workaround temporário** enquanto a integração Catalog → Library via eventos não existia.
>
> **A pré-condição foi satisfeita (Fase 2 + 4A):** o pipeline `BookCreated`/`BookUpdated` está implementado e verificado em runtime — o Library já mantém o `BookSnapshot` automaticamente. Este workaround pode (e deve) ser removido como item de housekeeping:
> 1. ✅ Catalog publica `BookCreated`/`BookUpdated`
> 2. ✅ Library consome e cria/atualiza o `BookSnapshot` automaticamente
> 3. ⏳ Remover os campos `BookTitle`, `BookAuthorDisplay`, `BookCoverUrl`, `BookPageCount` de `AddBookToLibraryCommand` e `AddBookToLibraryRequest`
> 4. ⏳ Restaurar o handler original que apenas faz `throw NotFoundException` quando o snapshot não existe (ou trata como condição transitória, se houver corrida add-livro-novo)
>
> *Nota:* ao remover, considerar a corrida em que um livro recém-criado no Catalog ainda não propagou o `BookSnapshot` para o Library quando o usuário tenta adicioná-lo — mesmo padrão transitório das decisões 8.3. Avaliar no momento da remoção.
>
> **Arquivos afetados:**
> - `src/Legi.Library.Application/UserBooks/Commands/AddBookToLibrary/AddBookToLibraryCommand.cs`
> - `src/Legi.Library.Application/UserBooks/Commands/AddBookToLibrary/AddBookToLibraryCommandHandler.cs`
> - `src/Legi.Library.Api/Controllers/UserBooksController.cs` (request DTO)

**Decisão: ReadingProgress como Aggregate Root.** Registros de progresso são independentes entre si — não existe invariante cross-registro. Evita carregar centenas de registros na memória ao adicionar um novo. Coordenação de progresso (registro com progresso → atualiza UserBook.CurrentProgress) feita na mesma transação pelo command handler.

**Decisão: Resenha (review) como `ReadingProgress` marcado.** Uma resenha é um `ReadingProgress` *content-only* (sem progresso) com `IsReview = true` e um snapshot de `Rating`, criado pela factory `ReadingProgress.CreateReview(...)`. Reutiliza toda a máquina existente (contadores de likes/comments, delete, fan-out) em vez de um aggregate `BookReview` paralelo. O `CreateBookReviewCommand` aplica `UserBook.Rate(rating, isPartOfReview: true)` **e** cria a resenha na mesma transação: a nota flui para a média do Catalog via `UserBookRated` (a flag `IsPartOfReview` faz o Social **suprimir** o feed item `BookRated`), enquanto a resenha emite uma única atividade `ReviewCreated`. A resenha é interativa como `InteractableType.Review` (likes/comments). A lista de resenhas de um livro é servida pelo Social (ver §5), não pelo Library — o Social já tem username/avatar/likes/comments/spoiler.

**Decisão: UserListItem como entity filha.** Justificativa: invariantes exigem os itens (duplicação, reordenação, BooksCount). UserListItem é minúsculo (IDs + order + timestamp) — carregar 500 itens é trivial.

**Decisão: listas referenciam `BookId`, não `UserBookId` (desacopladas da biblioteca).** A feature de listas customizadas trocou `UserListItem.UserBookId` por `BookId` (migration `DecoupleUserListItemFromUserBook`: add `book_id` + backfill via join em `user_books` + drop `user_book_id`, unique `(user_list_id, book_id)`). Consequências: uma lista pode conter qualquer livro com `BookSnapshot` (não precisa estar na biblioteca do usuário); adicionar um livro a uma lista **não** o adiciona à biblioteca; quando um `UserBook` é soft-deleted, a lista **mantém** o livro. `UserList.SyncBooks(bookIdsInOrder)` reconcilia o conjunto de livros ao alvo (remove ausentes, adiciona novos, atribui `Order` por posição, preserva `AddedAt` dos retidos, rejeita duplicatas) — usado pelo fluxo de criar/editar que submete o conjunto completo de livros.

**Decisão: Soft Delete no UserBook.** Remoção marca `DeletedAt`. ReadingProgress preservados (histórico). Memberships de lista **preservadas** — listas referenciam `BookId` (independentes da biblioteca), então remover o livro da biblioteca não o tira das listas. Re-adição do mesmo livro cria novo UserBook. Global Query Filter no EF Core para filtrar deletados.

**Enums:**
```csharp
enum ReadingStatus { NotStarted, Reading, Finished, Abandoned, Paused }
enum ProgressType { Page, Percentage }
```

Sem state machine — todas as transições entre status são válidas. O usuário pode corrigir livremente.

**Value Objects:**
- `Rating` — `int` de 1 a 10 (meias-estrelas). `1 = 0.5★, 2 = 1.0★, ..., 10 = 5.0★`. Propriedade `Stars => Value / 2.0m`. API recebe/retorna estrelas (0.5-5.0), conversão via `Rating.FromStars(decimal)`. SMALLINT no banco. Cada bounded context define seu próprio Rating (não compartilhado no SharedKernel). Constantes: `MinValue = 1`, `MaxValue = 10`.
- `Progress` — `Value (int)` + `Type (ProgressType)`. Validação interna: `Value >= 0`; se `Percentage`: `Value <= 100`. Validação de `Page <= PageCount` é feita pelo command handler (acesso ao BookSnapshot). Factory methods: `Create(int, ProgressType)`, `CreatePercentage(int)`, `CreatePage(int)`, `Completed()`. Constante: `MaxPercentage = 100`.

**Regras:**
- Status inicia como `NotStarted`
- Quando status muda para `Reading`, `Finished`, `Abandoned` ou `Paused`, `WishList` é automaticamente setado para `false`. Wishlist só é válido com `NotStarted`.
- Marcar `Finished` manualmente define progress = `Progress.Completed()` (100%)
- Reverter de `Finished` para outro status reseta `CurrentProgress` para `null`
- Progresso 100% (Percentage) auto-transiciona status para `Finished`
- Progresso Page igual ao PageCount do BookSnapshot é convertido para `Completed()`
- Rating é independente do status — pode ser adicionado/removido a qualquer momento
- Soft delete: `UserBook.Remove()` marca `DeletedAt`, preserva ReadingProgress e memberships de lista
- Livro pode estar em múltiplas listas (N:N via UserListItem por `BookId`); listas são independentes da biblioteca
- Máximo 100 listas por usuário
- Nome da lista único por usuário (case-insensitive)
- ReadingProgress deve ter conteúdo OU progresso (ou ambos). Resenha (`IsReview`) exige conteúdo (min `MinReviewContentLength = 20`) + `Rating`, sem progresso.
- `ReadingProgress.IsSpoiler` é metadado de apresentação: não altera invariantes do post, mas é persistido no Library e propagado para o Social para ocultar o texto no feed até o usuário revelar (mesmo padrão para posts de progresso e resenhas).

**Domain Events (11 — princípio YAGNI, apenas com consumidores identificados):**

| Aggregate | Evento | Consumidores |
|-----------|--------|-------------|
| UserBook | `BookAddedToLibraryDomainEvent` | Social (feed) |
| UserBook | `BookRemovedFromLibraryDomainEvent` | Social (feed) |
| UserBook | `ReadingStatusChangedDomainEvent` | Social (feed) |
| UserBook | `UserBookRatedDomainEvent` | Catalog (recalcular média), Social (feed BookRated — suprimido quando `IsPartOfReview`) |
| UserBook | `UserBookRatingRemovedDomainEvent` | Catalog (recalcular média) |
| ReadingProgress | `ReadingProgressCreatedDomainEvent` | Social (feed ProgressPosted) |
| ReadingProgress | `ReviewCreatedDomainEvent` | Social (feed ReviewCreated + snapshot), Catalog (reviews count) |
| ReadingProgress | `ReadingPostDeletedDomainEvent` | Social (remover do feed), Catalog (decrementa reviews count quando `IsReview`) |
| UserList | `UserListCreatedDomainEvent` (carrega `IsPublic`) | Social (snapshot da lista quando pública → interativa) |
| UserList | `UserListUpdatedDomainEvent` (carrega `IsPublic`) | Social (cria/dropa snapshot no toggle público↔privado) |
| UserList | `UserListDeletedDomainEvent` | Social (limpar snapshot + likes + comments + follows) |

> **Nota:** `UserListCreated`/`UserListUpdatedDomainEvent` foram reintroduzidos pela feature de listas customizadas (antes cortados por YAGNI). Agora têm consumidor claro: o Social projeta um `ContentSnapshot(List)` apenas para listas públicas, tornando-as interativas (ver §5). Cada um é traduzido para o `*IntegrationEvent` homônimo via `UserListCreated/Updated/DeletedDomainEventHandler` (outbox).

**Cortados por YAGNI:** `ReadingProgressUpdatedDomainEvent`, `BookAddedToListDomainEvent`, `BookRemovedFromListDomainEvent` — nenhum consumidor claro identificado.

**Repository Interfaces (Domain):**
- `IUserBookRepository` — GetByIdAsync, GetByUserAndBookAsync, AddAsync, UpdateAsync
- `IReadingPostRepository` — GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync
- `IUserListRepository` — GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync, GetCountByUserIdAsync, ExistsByUserAndNameAsync, DeleteAllForUserAsync (cascade na deleção de usuário). *`GetListsContainingBookAsync` foi removido — listas mantêm o livro no soft-delete do UserBook.*
- `IBookSnapshotRepository` — GetByBookIdAsync, AddOrUpdateAsync

### 3.2 Application ✅

**UserBook Commands:**

| Command | Handler | Validator | Response | Status |
|---------|---------|-----------|----------|--------|
| `AddBookToLibraryCommand` | ✅ | ✅ | ✅ | Completo |
| `UpdateUserBookCommand` | ✅ | ✅ | ✅ | Completo |
| `RemoveBookFromLibraryCommand` | ✅ | - | Unit | Completo |
| `RateUserBookCommand` | ✅ | ✅ | ✅ | Completo |
| `RemoveUserBookRatingCommand` | ✅ | - | Unit | Completo |

**ReadingProgress Commands:**

| Command | Handler | Validator | Response | Status |
|---------|---------|-----------|----------|--------|
| `CreateReadingPostCommand` | ✅ | - | ✅ | Completo |
| `CreateBookReviewCommand` | ✅ | ✅ | ✅ | Completo (resenha: rating + conteúdo + spoiler) |
| `UpdateReadingPostCommand` | ✅ | ✅ | ✅ | Completo |
| `DeleteReadingPostCommand` | ✅ | - | Unit | Completo |

**UserList Commands:**

| Command | Handler | Validator | Response | Status |
|---------|---------|-----------|----------|--------|
| `CreateUserListCommand` | ✅ | ✅ | ✅ | Completo (aceita `BookIds`; valida `BookSnapshot` de cada; `SyncBooks`; retorna `ListId` real) |
| `UpdateUserListCommand` | ✅ | ✅ | ✅ | Completo (aceita `BookIds`; checa ownership; `UpdateDetails` + `SyncBooks` numa transação) |
| `DeleteUserListCommand` | ✅ | - | Unit | Completo |
| `AddBookToListCommand` | ✅ | - | ✅ | Completo (por `BookId`; valida snapshot) |
| `RemoveBookFromListCommand` | ✅ | - | Unit | Completo (por `BookId`) |

**Queries:**

| Query | Handler | Status |
|-------|---------|--------|
| `GetMyLibraryQuery` | ✅ | Completo |
| `GetMyUserBookByBookQuery` | ✅ | Completo (UserBook do viewer por livro, ou null — header da página de detalhes) |
| `GetUserBookPostsQuery` | ✅ | Completo |
| `GetMyListsQuery` | ✅ | Completo |
| `GetListDetailsQuery` | ✅ | Completo |
| `GetListBooksQuery` | ✅ | Completo |
| `SearchPublicListsQuery` | ✅ | Completo |

**Read Repository Interfaces (Application):**
- `IUserBookReadRepository` — GetByUserIdAsync (com filtros de status, wishlist, search, paginação), GetByUserAndBookAsync (UserBook ativo do viewer para um livro)
- `IReadingPostReadRepository` — GetByUserBookIdAsync (paginação)
- `IUserListReadRepository` — GetByUserIdAsync, GetDetailByIdAsync, GetListBooksAsync, SearchPublicAsync

**DTOs:** `UserBookDto`, `BookSnapshotDto`, `ReadingPostDto`, `UserListDetailDto` `(ListId, UserId, Name, Description, IsPublic, BooksCount, LikesCount, CommentsCount, CreatedAt, UpdatedAt, IsOwner)`, `UserListSummaryDto` `(ListId, OwnerId, Name, Description, IsPublic, BooksCount, LikesCount, CreatedAt, PreviewBooks ≤4)`, `UserListBookDto` `(BookId, Order, Book: BookSnapshotDto, AddedAt)`, `PaginatedList<T>`
**Domain Event Handlers (Library → integration via outbox):** `UserListCreatedDomainEventHandler`, `UserListUpdatedDomainEventHandler`, `UserListDeletedDomainEventHandler` — traduzem o evento de domínio para o `*IntegrationEvent` correspondente (`Legi.Contracts/Library`) e publicam via `IEventBus` (mesmo padrão de `ReviewCreatedDomainEventHandler`)
**Behaviors:** `ValidationBehavior`, `LoggingBehavior`
**Exceptions:** `ConflictException`, `NotFoundException`, `ForbiddenException`
**DI:** `DependencyInjection.AddLibraryApplication()` — registra Mediator, handlers (reflection scan), behaviors e validators

### 3.3 Infrastructure ✅

**`LibraryDbContext`** — EF Core + PostgreSQL (connection string key: `LibraryDatabase`).
- 5 DbSets: `UserBooks`, `ReadingPosts`, `UserLists`, `UserListItems`, `BookSnapshots`
- `SaveChangesAsync()` override coleta domain events de todas as entities `BaseEntity` antes do save, salva no banco, e publica via `IMediator` após sucesso
- Retry policy habilitada (máx 3 tentativas para falhas transitórias)
- Configurations aplicadas automaticamente via `ApplyConfigurationsFromAssembly()`

**Entity Configurations (Fluent API):**

| Configuration | Tabela | Destaques |
|---------------|--------|-----------|
| `UserBookConfiguration` | `user_books` | Status como string (max 20). Rating e Progress como owned entities. Global Query Filter `DeletedAt == null`. Unique index filtrado `(UserId, BookId) WHERE deleted_at IS NULL`. |
| `ReadingPostConfiguration` | `reading_posts` | Content max `ReadingProgress.MaxContentLength`. `IsSpoiler` e `IsReview` boolean com default false. Progress e Rating (snapshot da resenha) como owned entities. Index composto `(UserBookId, ReadingDate DESC)`. |
| `UserListConfiguration` | `user_lists` | Name max `UserList.MaxNameLength`, Description max `UserList.MaxDescriptionLength`. One-to-many com `UserListItem` via field access (`_items`). Cascade delete. Unique index `(UserId, Name)`. Index filtrado `IsPublic = true`. |
| `UserListItemConfiguration` | `user_list_items` | Shadow FK `user_list_id`. Mapeia `book_id`. Unique index `(user_list_id, book_id)`. Index `(user_list_id, Order)`. |
| `BookSnapshotConfiguration` | `book_snapshots` | PK = `BookId` (do Catalog). Title max 500, AuthorDisplay max 1000. Read model desnormalizado. |

**Write Repositories (Domain interfaces):**

| Repositório | Interface | Métodos |
|-------------|-----------|---------|
| `UserBookRepository` | `IUserBookRepository` | GetByIdAsync, GetByUserAndBookAsync, AddAsync, UpdateAsync |
| `ReadingPostRepository` | `IReadingPostRepository` | GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync |
| `UserListRepository` | `IUserListRepository` | GetByIdAsync (include Items), AddAsync, UpdateAsync, DeleteAsync, GetCountByUserIdAsync, ExistsByUserAndNameAsync (case-insensitive via `.ToLower()`), DeleteAllForUserAsync (bulk SQL, cascade nos items) |
| `BookSnapshotRepository` | `IBookSnapshotRepository` | GetByBookIdAsync, AddOrUpdateAsync (upsert) |

**Read Repositories (Application interfaces):**

| Repositório | Interface | Métodos |
|-------------|-----------|---------|
| `UserBookReadRepository` | `IUserBookReadRepository` | GetByUserIdAsync — filtros opcionais (status, wishlist, search em título/autor), join com BookSnapshots, `AsNoTracking`, paginação, ordena por UpdatedAt DESC. GetByUserAndBookAsync — UserBook ativo do viewer para um livro (ou null), join com BookSnapshots |
| `ReadingPostReadRepository` | `IReadingPostReadRepository` | GetByUserBookIdAsync — filtra por UserBookId, ordena por ReadingDate DESC + CreatedAt DESC, `AsNoTracking`, paginação |
| `UserListReadRepository` | `IUserListReadRepository` | GetByUserIdAsync (listas do usuário, ordena por UpdatedAt DESC; subquery dos 4 primeiros covers para `PreviewBooks`), GetDetailByIdAsync, GetListBooksAsync (join `user_list_items` → `book_snapshots` por `book_id`, ordena por Order), SearchPublicAsync (busca em nome/descrição, ordena por BooksCount DESC + CreatedAt DESC) |

**DI:** `DependencyInjection.AddLibraryInfrastructure(IServiceCollection, IConfiguration)` — registra DbContext, todos os write repositories e read repositories como scoped

### 3.4 API Endpoints ✅

**UserBooks (implementados — `UserBooksController`):**

| Método | Endpoint | Descrição | Auth | Status |
|--------|----------|-----------|------|--------|
| GET | `/api/v1/library` | Minha biblioteca | 🔒 | ✅ |
| GET | `/api/v1/library/by-book/{bookId}` | Meu UserBook para um livro (200) ou não na biblioteca (204) | 🔒 | ✅ |
| POST | `/api/v1/library` | Adicionar livro | 🔒 | ✅ |
| PATCH | `/api/v1/library/{userBookId}` | Atualizar status/wishlist/progresso | 🔒 | ✅ |
| DELETE | `/api/v1/library/{userBookId}` | Remover da biblioteca (soft delete) | 🔒 | ✅ |
| PUT | `/api/v1/library/{userBookId}/rating` | Dar/atualizar rating | 🔒 | ✅ |
| DELETE | `/api/v1/library/{userBookId}/rating` | Remover rating | 🔒 | ✅ |

**ReadingProgress (implementados — `ReadingPostsController`):**

| Método | Endpoint | Descrição | Auth | Status |
|--------|----------|-----------|------|--------|
| GET | `/api/v1/library/{userBookId}/posts` | Listar registros de progresso | 🔒 | ✅ |
| POST | `/api/v1/library/{userBookId}/posts` | Criar registro de progresso | 🔒 | ✅ |
| POST | `/api/v1/library/{userBookId}/reviews` | Escrever resenha (rating + conteúdo + spoiler) | 🔒 | ✅ |
| PUT | `/api/v1/library/posts/{postId}` | Editar registro de progresso | 🔒 | ✅ |
| DELETE | `/api/v1/library/posts/{postId}` | Excluir registro de progresso | 🔒 | ✅ |

**UserLists (implementados — `UserListsController`):**

| Método | Endpoint | Descrição | Auth | Status |
|--------|----------|-----------|------|--------|
| GET | `/api/v1/library/lists` | Minhas listas | 🔒 | ✅ |
| POST | `/api/v1/library/lists` | Criar lista (nome, descrição, visibilidade, `BookIds`) | 🔒 | ✅ |
| GET | `/api/v1/library/lists/{listId}` | Detalhes da lista (viewer context — pública p/ qualquer autenticado, privada só dono) | 🔒 | ✅ |
| PATCH | `/api/v1/library/lists/{listId}` | Atualizar lista (detalhes + `BookIds`) | 🔒 | ✅ |
| DELETE | `/api/v1/library/lists/{listId}` | Excluir lista | 🔒 | ✅ |
| GET | `/api/v1/library/lists/{listId}/books` | Livros da lista (viewer context) | 🔒 | ✅ |
| POST | `/api/v1/library/lists/{listId}/books` | Adicionar livro a lista (por `bookId`) | 🔒 | ✅ |
| DELETE | `/api/v1/library/lists/{listId}/books/{bookId}` | Remover livro da lista | 🔒 | ✅ |
| GET | `/api/v1/library/lists/search` | Buscar listas públicas | 🔓 | ✅ |

**Query Params para biblioteca (`GET /api/v1/library`):**
- `status` - filtro por ReadingStatus
- `wishlist` - true/false
- `search` - busca em título/autor
- `page`, `pageSize` - paginação

**Autenticação:** JWT Bearer (mesma config do Identity Service — `JwtSettings` compartilhado). Todos os endpoints requerem `[Authorize]` exceto a busca de listas públicas (`[AllowAnonymous]`). Detalhes/livros de lista exigem autenticação porque carregam o viewer para aplicar a `IUserListVisibilityPolicy` (lista pública é visível a qualquer autenticado; privada só ao dono). UserId extraído do claim `sub` do JWT.

**Middleware:** `ExceptionHandlingMiddleware` (ValidationException → 400, DomainException → 400, NotFoundException → 404, ConflictException → 409, ForbiddenException → 403, UnhandledException → 500)

### 3.5 Database Schema

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
    current_progress_value INT,
    current_progress_type progress_type,
    wishlist BOOLEAN NOT NULL DEFAULT FALSE,
    rating SMALLINT CHECK (rating >= 1 AND rating <= 10),  -- meias-estrelas: 1=0.5★, 10=5.0★
    deleted_at TIMESTAMPTZ,                                -- soft delete
    added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CHECK ((current_progress_value IS NULL AND current_progress_type IS NULL) OR
           (current_progress_value IS NOT NULL AND current_progress_type IS NOT NULL))
);

CREATE UNIQUE INDEX ix_user_books_user_book ON user_books(user_id, book_id) WHERE deleted_at IS NULL;
CREATE INDEX ix_user_books_user_id ON user_books(user_id);
CREATE INDEX ix_user_books_status ON user_books(user_id, status);
CREATE INDEX ix_user_books_wishlist ON user_books(user_id) WHERE wishlist = TRUE;
CREATE INDEX ix_user_books_added_at ON user_books(added_at DESC);

-- Tabela: reading_posts (ReadingProgress aggregate, referência por ID ao user_books)
CREATE TABLE reading_posts (
    id UUID PRIMARY KEY,
    user_book_id UUID NOT NULL REFERENCES user_books(id) ON DELETE CASCADE,
    user_id UUID NOT NULL,       -- desnormalizado para feed/queries
    book_id UUID NOT NULL,       -- desnormalizado para feed/queries
    content VARCHAR(2000),
    is_spoiler BOOLEAN NOT NULL DEFAULT FALSE,
    is_review BOOLEAN NOT NULL DEFAULT FALSE,             -- distingue resenha de progresso
    rating_value SMALLINT,                                -- snapshot da nota da resenha (1-10), null em progresso
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
CREATE INDEX ix_reading_posts_user_id ON reading_posts(user_id);
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

-- Tabela: user_list_items (entity filha de user_lists)
-- Desde a feature de listas customizadas referencia book_id (Catalog), não user_book_id —
-- migration DecoupleUserListItemFromUserBook (add book_id + backfill + drop user_book_id).
CREATE TABLE user_list_items (
    id UUID PRIMARY KEY,
    user_list_id UUID NOT NULL REFERENCES user_lists(id) ON DELETE CASCADE,
    book_id UUID NOT NULL,            -- Catalog BookId (sem FK p/ user_books)
    "order" INT NOT NULL DEFAULT 0,
    added_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_list_id, book_id)
);

CREATE INDEX ix_user_list_items_list_order ON user_list_items(user_list_id, "order");
```

---

## 4. Web Frontend 🚧 EM DESENVOLVIMENTO

**Localização:** `web/legi-web/`

### 4.1 Stack

| Tecnologia | Versão | Uso |
|------------|--------|-----|
| React | 19.2 | UI framework |
| TypeScript | 5.9 | Type safety |
| Vite | 8.0 | Build tool / dev server |
| Tailwind CSS | 4.2 | Estilização (via @tailwindcss/vite) |
| React Router DOM | 7.13 | Roteamento SPA |
| Axios | 1.13 | Cliente HTTP |
| i18next | 25.8 | Internacionalização (pt-BR, en) |
| Lucide React | 0.577 | Ícones |
| @react-oauth/google | 0.13 | Botão "Continuar com o Google" (GIS) nas páginas de login/cadastro |
| class-variance-authority | 0.7 | Variantes de componentes |
| clsx + tailwind-merge | - | Utilitário de classes (`cn()` em `src/lib/utils.ts`) |

### 4.2 Estrutura

```
web/legi-web/src/
├── app/
│   ├── App.tsx              (componente principal)
│   ├── Layout.tsx           (layout com navegação)
│   └── routes.tsx           (definição de rotas)
├── components/ui/           (componentes reutilizáveis)
│   ├── Avatar.tsx
│   ├── Badge.tsx
│   ├── BookCard.tsx
│   ├── Button.tsx
│   ├── Card.tsx
│   ├── ProgressBar.tsx
│   └── StarRating.tsx
├── features/
│   ├── catalog/             (ExplorePage, RegisterBookPage, BookDetailsPage, API hooks, ISBN validation)
│   ├── library/             (ProfilePage, ListsPage, WishlistPage + mock data)
│   └── social/              (FeedPage, CurrentlyReading, FeedSidebar + mock data)
├── i18n/
│   ├── index.ts             (configuração i18next)
│   └── locales/
│       ├── en.json
│       └── pt-BR.json
├── lib/utils.ts             (helper cn())
├── hooks/                   (vazio — hooks customizados futuros)
├── services/                (vazio — integração API futura)
└── main.tsx                 (entry point)
```

### 4.3 Rotas

| Rota | Componente | Descrição |
|------|------------|-----------|
| `/` | `Navigate → /feed` | Redirect para feed |
| `/feed` | `FeedPage` | Feed social (currently reading) |
| `/explore` | `ExplorePage` | Busca e navegação do catálogo |
| `/books/new` | `RegisterBookPage` | Cadastro manual protegido de livro com metadados obrigatórios |
| `/books/:bookId` | `BookDetailsPage` | Detalhes do livro: info, média, status/progresso, resenhas (escrever/curtir/comentar) |
| `/lists` | `ListsPage` | Listas do usuário (grid de cards com mosaico 2×2 de capas) |
| `/lists/new` | `ListEditorPage` | Criar lista (nome, descrição, visibilidade, busca + seleção de livros) |
| `/lists/:listId` | `ListDetailPage` | Detalhes da lista + interação social (like/comment/follow; dono: editar/excluir) |
| `/lists/:listId/edit` | `ListEditorPage` | Editar lista própria |
| `/wishlist` | `WishlistPage` | Lista de desejos |
| `/profile` | `ProfilePage` | Perfil do usuário |
| `/users/:userId` | `UserProfilePage` | Perfil público de outro usuário |

> **Nota:** O frontend está dockerizado e roteia `/api/v1/*` via Nginx. A integração é progressiva: o fluxo `RegisterBookPage`/`useCreateBook` usa a API do Catalog e `BookDetailsPage` renderiza avisos de rota para livro criado ou já existente; alguns módulos ainda mantêm dados mock durante a migração.

### 4.4 Docker & Nginx

- **Build:** Multi-stage (node:22-alpine → nginx:alpine)
- **Porta:** 3000
- **SPA:** `try_files $uri $uri/ /index.html`
- **Reverse Proxy:** Nginx encaminha chamadas API para os serviços backend:
    - `/api/v1/identity/` → `identity-api:8080`
    - `/api/v1/catalog/` → `catalog-api:8080`
    - `/api/v1/library/` → `library-api:8080`
    - `/api/v1/social/` → `social-api:8080`
- **Google Sign-In:** o `Client ID` chega ao bundle via build-arg `VITE_GOOGLE_CLIENT_ID` (Dockerfile/compose). O `Content-Security-Policy` do Nginx libera os domínios do GIS (`script-src`/`frame-src`/`connect-src`/`style-src` → `accounts.google.com/gsi/...`); sem isso o navegador bloqueia o carregamento do botão. Origem `http://localhost:3000` precisa estar nas *Authorized JavaScript origins* do client OAuth no Google Console.

---

## 5. Social Service ✅

Decisões de arquitetura detalhadas em `Docs/SOCIAL-ARCHITECTURE-decisions.md`.

### 5.1 Domínio ✅

**Aggregates:**

```
Follow (Aggregate Root — magro)
├── Id: Guid
├── FollowerId: Guid
├── FollowingId: Guid
├── CreatedAt: DateTime
├── Factory: Create(followerId, followingId) → valida auto-follow
└── Hard delete no unfollow

ListFollow (Aggregate Root — magro; seguir uma lista pública)
├── Id: Guid
├── UserId: Guid
├── ListId: Guid
├── CreatedAt: DateTime
├── Factory: Create(userId, listId)
└── Chave natural composta (UserId, ListId). Hard delete no unfollow.
    Distinto de Follow (user↔user); sem contadores/eventos — count lido live,
    listas seguidas ainda não surgem no perfil.

UserProfile (Aggregate Root — UserId como PK, não herda BaseEntity)
├── UserId: Guid (PK)
├── Username: string (snapshot do Identity)
├── Bio: string? (max 500, constante: MaxBioLength)
├── AvatarUrl: string?
├── BannerUrl: string?
├── FollowersCount: int (≥ 0, nativo do Social)
├── FollowingCount: int (≥ 0, nativo do Social)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

Like (Aggregate Root — magro)
├── Id: Guid
├── UserId: Guid
├── TargetType: InteractableType
├── TargetId: Guid
├── CreatedAt: DateTime
├── Factory: Create(userId, targetType, targetId)
└── Unicidade: (userId + targetType + targetId) no banco. Hard delete no unlike

Comment (Aggregate Root — magro, imutável)
├── Id: Guid
├── UserId: Guid
├── TargetType: InteractableType
├── TargetId: Guid
├── Content: string (1-500, constantes: MinContentLength, MaxContentLength)
├── CreatedAt: DateTime
├── Factory: Create(userId, targetType, targetId, content)
└── Sem edição — só cria ou deleta. Deletável pelo autor OU pelo dono do conteúdo alvo

ContentSnapshot (Read Model — PK composta, enriquecido)
├── TargetType: InteractableType
├── TargetId: Guid
├── OwnerId: Guid (para autorização de deleção de comments)
├── OwnerUsername: string (snapshot do Identity)
├── OwnerAvatarUrl: string? (snapshot do Identity)
├── BookTitle: string? (do Catalog/Library)
├── BookAuthor: string? (do Catalog/Library)
├── BookCoverUrl: string? (do Catalog/Library)
├── ContentPreview: string? (primeiros ~200 chars do post/review; null para posts marcados como spoiler)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

FeedItem (Read Model — desnormalizado para feed)
├── Id: Guid
├── ActorId: Guid
├── ActorUsername: string
├── ActorAvatarUrl: string?
├── ActivityType: ActivityType
├── TargetType: InteractableType? (null se não é interagível, ex: BookAdded)
├── ReferenceId: Guid (id do post, review, lista, etc)
├── BookId: Guid? (livro da atividade, quando aplicável — permite query de resenhas por livro; null em ListCreated)
├── BookTitle: string?
├── BookAuthor: string?
├── BookCoverUrl: string?
├── Data: string? (JSON — progresso, rating, texto do post; `ProgressPosted`/`ReviewCreated` podem carregar `isSpoiler`)
└── CreatedAt: DateTime
```

**Enums:**
```csharp
enum InteractableType { Post, Review, List }
enum ActivityType { ProgressPosted, BookFinished, BookStarted, BookAdded, BookRated, ReviewCreated, ListCreated }
```

**Regras:**
- Não pode seguir a si mesmo (validado no aggregate)
- Follow é unique (FollowerId, FollowingId). Hard delete no unfollow
- Like é unique (UserId, TargetType, TargetId). Usuário só pode curtir o mesmo conteúdo uma vez
- Comentários são imutáveis — só cria ou deleta, sem edição
- Comentário deletável pelo autor OU dono do conteúdo (verificado no handler via ContentSnapshot.OwnerId)
- Contadores de seguidores/seguindo nunca negativos (protegido no aggregate)
- UserProfile criado via integration event no registro do Identity
- Like e Comment usam `TargetType + TargetId` polimórfico — modelo unificado para Post, Review e List
- **Listas públicas são interagíveis ✅ (pipeline implementado pela feature de listas customizadas — Opção B).** A decisão original (Opção A: listas não-interagíveis) foi revertida. O Library publica `UserListCreated`/`UserListUpdated`/`UserListDeleted`; o Social cria um `ContentSnapshot(List, listId, contentPreview: Name)` **apenas para listas públicas** (handler snapshot-only, sem `FeedItem` — criar lista não vira atividade de feed). **A existência do snapshot é o gate de visibilidade/interação:** lista privada não tem snapshot, então os handlers genéricos de like/comment/follow a rejeitam (NotFound) sem ramificação extra; um toggle público→privado dropa o snapshot. `ContentLiked`/`ContentCommented` agora carregam `TargetType ∈ {"Post", "Review", "List"}`. Likes/comments de lista via `ListInteractionsController` (`/social/lists/{id}/likes|comments`), follow de lista via `ListFollow` (mesmo controller, `/follows`). **Nota:** `UserList.LikesCount`/`CommentsCount` no Library seguem dormentes — a página de detalhes lê os contadores live do Social (`GET /social/lists/{listId}`), então o Social não propaga esses contadores de volta ao Library.
- **Resenhas (Review) são interagíveis ✅ (pipeline implementado).** `ReviewCreatedIntegrationEvent` (Library) cria `ContentSnapshot(Review, reviewId)` + `FeedItem(ReviewCreated, TargetType=Review, BookId, Data={rating, content, isSpoiler})`. `ContentLiked`/`ContentCommented` carregam `TargetType ∈ {"Post", "Review"}`; o Library resolve ambos pela mesma `ReadingProgress` (o `InteractionTargetResolver` aceita os dois). Likes/comments de resenha via `ReviewInteractionsController` (`/social/reviews/{id}/likes|comments`).
- Feed: fan-out on read (query com join em follows), não fan-out on write
- `IsSpoiler` é preservado no `FeedItem.Data` de `ProgressPosted` **e** `ReviewCreated`; quando true, o `ContentSnapshot.ContentPreview` fica null para não vazar texto em superfícies de interação/notificação, e o frontend oculta o texto até revelar (mesmo padrão para progresso e resenha).
- `BookAdded` ≠ `BookStarted`: adicionar um livro à biblioteca (não-wishlist) gera `ActivityType.BookAdded` ("adicionou à biblioteca"), **não** "começou a ler". `BookStarted` fica reservado para um futuro evento de início de leitura (hoje sem produtor — uma mudança de status para `Reading` é no-op no feed; só `Finished` vira `BookFinished`). Ambos são não-interagíveis (`TargetType = null`). `ActivityType` é persistido como string, então novos valores não exigem migração.
- LikesCount/CommentsCount no feed são query em tempo real (mesmo banco), não desnormalizados na Activity

**Domain Events (6 — princípio YAGNI, apenas com consumidores identificados):**

| Aggregate | Evento | Consumidores |
|-----------|--------|-------------|
| Follow | `FollowCreatedDomainEvent` | UserProfile (incrementa contadores) |
| Follow | `FollowRemovedDomainEvent` | UserProfile (decrementa contadores) |
| Like | `ContentLikedDomainEvent` | Library (incrementa LikesCount) via integration event |
| Like | `ContentUnlikedDomainEvent` | Library (decrementa LikesCount) via integration event |
| Comment | `CommentCreatedDomainEvent` | Library (incrementa CommentsCount) via integration event |
| Comment | `CommentDeletedDomainEvent` | Library (decrementa CommentsCount) via integration event |

**Repository Interfaces (Domain):**
- `IFollowRepository` — GetByIdAsync, GetByPairAsync, AddAsync, DeleteAsync
- `IUserProfileRepository` — GetByUserIdAsync, AddAsync, UpdateAsync, DeleteAsync
- `ILikeRepository` — GetByIdAsync, GetByUserAndTargetAsync, AddAsync, DeleteAsync
- `ICommentRepository` — GetByIdAsync, AddAsync, DeleteAsync
- `IContentSnapshotRepository` — GetByTargetAsync, AddOrUpdateAsync, StageAddOrUpdateAsync, DeleteByTargetAsync, StageDeleteByTargetAsync
- `IFeedItemRepository` — AddAsync, DeleteByReferenceAsync, DeleteByActorAsync, StageAddAsync, StageDeleteByReferenceAsync
- `IListFollowRepository` — GetByUserAndListAsync, ExistsAsync, CountByListAsync, AddAsync, DeleteAsync, StageDeleteByListAsync

### 5.2 Application ✅

**Follow Commands:** `FollowUserCommand`, `UnfollowUserCommand`
**List Commands:** `FollowListCommand`, `UnfollowListCommand` (exigem snapshot de lista pública existente; rejeitam o dono; unfollow idempotente)
**Comment Commands:** `CreateCommentCommand`, `DeleteCommentCommand`
**Like Commands:** `LikeContentCommand`, `UnlikeContentCommand`
**Profile Commands:** `UpdateProfileCommand`

**Follow Queries:** `GetFollowersQuery`, `GetFollowingQuery`
**Comment Queries:** `GetContentCommentsQuery`
**Like Queries:** `GetContentLikesQuery`
**Feed Queries:** `GetFeedQuery`, `GetUserActivityQuery`, `GetBookReviewsQuery` (resenhas de um livro)
**Profile Queries:** `GetUserProfileQuery`
**Content Queries:** `GetContentContextQuery`
**List Queries:** `GetListSocialStateQuery` → `ListSocialStateDto` (estado social live da lista para o viewer)

**Behaviors:** `ValidationBehavior`, `LoggingBehavior`
**Exceptions:** `ConflictException`, `NotFoundException`, `ForbiddenException`

**Read Repository Interfaces (Application):**
- `IFollowReadRepository` — GetFollowersAsync, GetFollowingAsync (com `ViewerUserId` opcional para `IsFollowedByViewer`)
- `ICommentReadRepository` — GetByTargetAsync (paginado, com username/avatar via join com user_profiles)
- `ILikeReadRepository` — GetByTargetAsync (paginado, com `ViewerUserId` opcional para `IsFollowedByViewer`)
- `IFeedItemReadRepository` — GetFeedAsync, GetUserActivityAsync e GetBookReviewsAsync (filtra `BookId` + `ActivityType=ReviewCreated`) com paginação offset (`page`, `pageSize` → `PaginatedList<FeedItemDto>`)
- `IListSocialReadRepository` — GetStateAsync (computa estado social live da lista: counts + flags do viewer; lista sem `ContentSnapshot` → estado não-interativo com counts zerados)

**DTOs:**
- `FollowUserDto` (UserId, Username, AvatarUrl, Bio, IsFollowedByViewer)
- `CommentDto` (Id, UserId, Username, AvatarUrl, Content, CreatedAt)
- `FeedItemDto` (Id, ActorId, ActorUsername, ActorAvatarUrl, ActivityType, TargetType, ReferenceId, BookId, BookTitle, BookAuthor, BookCoverUrl, Data, LikesCount, CommentsCount, IsLikedByMe, CreatedAt)
- `UserProfileDto` (UserId, Username, Bio, AvatarUrl, BannerUrl, FollowersCount, FollowingCount, IsFollowing, CreatedAt)
- `ContentContextDto` (TargetType, TargetId, OwnerId, OwnerUsername, OwnerAvatarUrl, BookTitle, BookAuthor, BookCoverUrl, ContentPreview)
- `LikeUserDto` (UserId, Username, AvatarUrl, IsFollowedByViewer)
- `ListSocialStateDto` (ListId, IsInteractable, IsOwner, LikesCount, CommentsCount, FollowersCount, IsLikedByMe, IsFollowedByMe) — `IsInteractable=false` quando a lista não tem `ContentSnapshot` (privada), com counts/flags zerados
- `PaginatedList<T>` (Items, Page, PageSize, TotalItems, TotalPages, HasNext, HasPrevious) — usado pelas queries de feed e listas sociais com `page`/`pageSize`
- Response DTOs: `FollowResponse`, `CreateCommentResponse`, `LikeResponse`, `UpdateProfileResponse`

**Domain Event Handlers:**
- `FollowCreatedDomainEventHandler` — incrementa FollowersCount/FollowingCount no UserProfile
- `FollowRemovedDomainEventHandler` — decrementa contadores no UserProfile
- `CommentCreatedDomainEventHandler` — traduz e publica `ContentCommentedIntegrationEvent` (Fase 4D)
- `CommentDeletedDomainEventHandler` — traduz e publica `CommentDeletedIntegrationEvent` (Fase 4D)
- `ContentLikedDomainEventHandler` — traduz e publica `ContentLikedIntegrationEvent` (Fase 4D)
- `ContentUnlikedDomainEventHandler` — traduz e publica `ContentUnlikedIntegrationEvent` (Fase 4D)

**Integration Event Handlers (incoming):** `UserRegistered`/`UserDeleted` (Fase 3), `BookCreated`/`BookUpdated` (Fase 4A → `BookSnapshot`), handlers de eventos do Library → `FeedItem`/`ContentSnapshot` (Fase 4C): `BookAddedToLibrary`, `ReadingStatusChanged`, `ReadingPostCreated`, `ReadingPostDeleted`, `UserBookRated` e `ReviewCreated` (cria `ContentSnapshot(Review)` + `FeedItem(ReviewCreated)`), e os handlers de listas (feature de listas customizadas): `UserListCreated`/`UserListUpdated` (snapshot-only — cria `ContentSnapshot(List)` se pública, dropa no toggle privado; **sem** `FeedItem`) e `UserListDeleted` (purga snapshot + likes + comments + follows da lista). Todos seguem o padrão stage-no-SaveChanges (decisão 8.1).

**DI:** `DependencyInjection.AddSocialApplication()` — registra Mediator, handlers (reflection scan), notification handlers, behaviors e validators

### 5.3 API Endpoints ✅

Decisões detalhadas em `Docs/SOCIAL-ARCHITECTURE-decisions.md` seção 12.

**Follows (implementados — `FollowsController`):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| POST | `/api/v1/social/follows` | Seguir usuário | 🔒 |
| DELETE | `/api/v1/social/follows/{userId}` | Deixar de seguir | 🔒 |
| GET | `/api/v1/social/users/{userId}/followers` | Listar seguidores | 🔓 |
| GET | `/api/v1/social/users/{userId}/following` | Listar seguindo | 🔓 |

**Profile (implementado — `UserProfilesController`):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| GET | `/api/v1/social/users/{userId}` | Perfil público | 🔓 |

**Feed (implementado — `FeedController`):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| GET | `/api/v1/social/feed` | Feed de atividades paginado | 🔒 |
| GET | `/api/v1/social/users/{userId}/activity` | Atividades de um usuário paginadas | 🔓 |
| GET | `/api/v1/social/books/{bookId}/reviews` | Resenhas de um livro (página de detalhes) | 🔓 |

**Post Interactions (implementados — `PostInteractionsController`):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| POST | `/api/v1/social/posts/{postId}/likes` | Curtir post | 🔒 |
| DELETE | `/api/v1/social/posts/{postId}/likes` | Descurtir post | 🔒 |
| GET | `/api/v1/social/posts/{postId}/comments` | Listar comentários do post | 🔓 |
| POST | `/api/v1/social/posts/{postId}/comments` | Comentar em post | 🔒 |

**List Interactions (implementados — `ListInteractionsController`):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| GET | `/api/v1/social/lists/{listId}` | Estado social da lista (counts + flags do viewer) | 🔓 |
| POST | `/api/v1/social/lists/{listId}/likes` | Curtir lista | 🔒 |
| DELETE | `/api/v1/social/lists/{listId}/likes` | Descurtir lista | 🔒 |
| GET | `/api/v1/social/lists/{listId}/comments` | Listar comentários da lista | 🔓 |
| POST | `/api/v1/social/lists/{listId}/comments` | Comentar em lista | 🔒 |
| POST | `/api/v1/social/lists/{listId}/follows` | Seguir lista | 🔒 |
| DELETE | `/api/v1/social/lists/{listId}/follows` | Deixar de seguir lista | 🔒 |

**Review Interactions (implementados — `ReviewInteractionsController`):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| POST | `/api/v1/social/reviews/{reviewId}/likes` | Curtir resenha | 🔒 |
| DELETE | `/api/v1/social/reviews/{reviewId}/likes` | Descurtir resenha | 🔒 |
| GET | `/api/v1/social/reviews/{reviewId}/comments` | Listar comentários da resenha | 🔓 |
| POST | `/api/v1/social/reviews/{reviewId}/comments` | Comentar em resenha | 🔒 |

**Comments (implementado — `CommentsController`):**

| Método | Endpoint | Descrição | Auth |
|--------|----------|-----------|------|
| DELETE | `/api/v1/social/comments/{commentId}` | Excluir comentário | 🔒 |

**Fora do v1:** `GET /api/v1/social/discover` foi deferido para uma versão futura.

**Query Params para feed (`GET /api/v1/social/feed` e `/users/{userId}/activity`):**
- `page` - número da página (default 1)
- `pageSize` - tamanho da página (default 20)

**Query Params para listas paginadas (seguidores, comments, likes):**
- `page` - número da página (default 1)
- `pageSize` - tamanho da página (default 20, max 50)

**Autenticação:** JWT Bearer (mesma config do Identity — `JwtSettings` compartilhado). Endpoints de escrita e feed pessoal requerem `[Authorize]`. Leitura pública (perfil, seguidores, comments, atividade de usuário) é `[AllowAnonymous]`. UserId extraído do claim `sub`.

**Middleware:** `ExceptionHandlingMiddleware` (ValidationException → 400, DomainException → 400, NotFoundException → 404, ConflictException → 409, ForbiddenException → 403, UnhandledException → 500)

### 5.4 Database Schema ✅

Schema implementado por `SocialDbContext` e migrations em `src/Legi.Social.Infrastructure/Migrations`.

```sql
-- Tabelas próprias do Social
user_profiles(user_id PK, username, bio, avatar_url, banner_url, followers_count, following_count, created_at, updated_at)
follows(id PK, follower_id, following_id, created_at, UNIQUE(follower_id, following_id))
list_follows(id PK, user_id, list_id, created_at, UNIQUE(user_id, list_id))   -- migration AddListFollows
likes(id PK, user_id, target_type, target_id, created_at, UNIQUE(user_id, target_type, target_id))
comments(id PK, user_id, target_type, target_id, content, created_at)
content_snapshots(target_type, target_id, owner_id, owner_username, owner_avatar_url, book_title, book_author, book_cover_url, content_preview, created_at, updated_at, PK(target_type, target_id))
feed_items(id PK, actor_id, actor_username, actor_avatar_url, activity_type, target_type, reference_id, book_id, book_title, book_author, book_cover_url, data JSONB, created_at; index (book_id, activity_type, created_at DESC) para resenhas por livro)
book_snapshots(book_id PK, title, author_display, cover_url, page_count, updated_at)

-- Tabelas compartilhadas de mensageria
inbox_messages(id PK, type, processed_at)
outbox_messages(id PK, type, payload JSONB, occurred_at, processed_at, attempts, next_retry_at, error)
```

---

## 6. Comunicação Entre Serviços ✅ IMPLEMENTADO (Fases 1–6)

Mensageria assíncrona via RabbitMQ com padrão **outbox/inbox** (entrega at-least-once + idempotência via inbox). **Fonte de verdade dos contratos, tópicos/filas, idempotência e ordering:** `Docs/MESSAGING-ARCHITECTURE-decisions.md`. Esta seção é apenas o panorama — não duplicar o catálogo aqui (foi essa duplicação que gerou o drift anterior).

### 6.1 Fluxos implementados

**Identity → Social** (Fase 3): `UserRegistered` cria `UserProfile`.
**Identity → Catalog + Library + Social** (Fase 3): `UserDeleted` — cada serviço purga seus próprios dados (Catalog: `created_by` → "Usuário Removido"; Library: user_books/lists; Social: follows/likes/comments/feed).
**Catalog → Library + Social** (Fases 2 / 4A): `BookCreated` / `BookUpdated` — cada serviço mantém seu `BookSnapshot` local como fonte de lookup de display data em write-time (decisão 2.6.1).
**Library → Social** (Fases 4B / 4C): `BookAddedToLibrary`, `ReadingStatusChanged`, `ReadingPostCreated`, `ReadingPostDeleted`, `UserBookRated`, `ReviewCreated` — Social projeta `FeedItem` / `ContentSnapshot` (feed fan-out on read).
`ReadingPostCreated`/`ReviewCreated` carregam `Content`, `IsSpoiler` (e `ReviewCreated` também o rating + `BookId`); o Social grava `isSpoiler` no `FeedItem.Data` e suprime `ContentPreview` quando spoiler. `UserBookRated` carrega `IsPartOfReview`: quando true, o Social **não** cria o feed item `BookRated` (a resenha já emite `ReviewCreated`), evitando atividade duplicada.
**Library → Social** (listas customizadas): `UserListCreated`, `UserListUpdated`, `UserListDeleted` — Social mantém o `ContentSnapshot(List)` que governa a interatividade da lista (snapshot só existe enquanto a lista é pública; `UserListDeleted` purga snapshot + likes + comments + follows). Sem `FeedItem` (criar/editar lista não é atividade de feed).
**Social → Library** (Fases 4D / 4E): `ContentLiked` / `ContentUnliked` / `ContentCommented` / `CommentDeleted` — Library ajusta `LikesCount` / `CommentsCount` no `ReadingProgress` (`TargetType ∈ {Post, Review}`; idempotência inbox-only, decisão 8.1.1).
**Library → Catalog** (Fase 5): `UserBookRated` / `UserBookRatingRemoved` → Catalog mantém uma projeção `BookRating` por-usuário e recalcula `average_rating`/`ratings_count` no `Book` (recompute-from-rows, convergente; `UserBookRated` é um segundo consumer no fanout que o Social já usa).
**Library → Catalog** (resenhas): `ReviewCreated` incrementa e `ReadingPostDeleted` (quando `IsReview`) decrementa `Book.ReviewsCount` (idempotência inbox-only).

**Total atual: 19 arquivos de integration event em `Legi.Contracts`** (inclui `PingIntegrationEvent` diagnóstico, `ReviewCreatedIntegrationEvent` e os 3 eventos de lista `UserListCreated/Updated/DeletedIntegrationEvent`). Contrato a contrato em `MESSAGING-ARCHITECTURE-decisions.md` §6.

### 6.2 Implementado desde a v1 inicial

> O **pipeline de Review** (antes fora de escopo) está completo — `ReviewCreatedIntegrationEvent`, `ContentSnapshot(Review)`, `FeedItem(ReviewCreated)`, likes/comments de resenha e contagem no Catalog. Ver §3.1, §5 e os fluxos acima.

> **Listas customizadas interagíveis** (antes fora de escopo, decisão Fase 4 Opção A — agora revertida para Opção B): existem os integration events `UserListCreated`/`UserListUpdated`/`UserListDeleted`; o Social cria `ContentSnapshot(List)` apenas para listas públicas (snapshot-only, sem `FeedItem`), habilitando like/comment/follow de lista. Listas privadas (sem snapshot) seguem não-interagíveis. Ver §3.1, §5 e o fluxo Library → Social (listas) acima.

### 6.3 Resiliência, observabilidade e operação (Fase 6 — hardening)

Panorama; detalhes (topologia, opções, gates) em `MESSAGING-ARCHITECTURE-decisions.md` §8 e Fase 6.

- **Retry/parking no consumer:** cada work queue tem DLX → fila de retry (TTL fixo) → reentrega; ao esgotar o budget de tentativas a mensagem vai p/ uma **error/parking queue** (terminal, sem consumer) — sem mais loop infinito de redelivery. Falhas classificadas: `TransientMessagingException` (pré-condição que se resolve, ex. snapshot ainda não chegou) recebe budget generoso; exceções genéricas parkam rápido. Producer/outbox já tinha retry com backoff + marcação poison.
- **Observabilidade:** `/health` em cada API (conexão RabbitMQ + backlog do outbox → Degraded acima do threshold); métricas OTel (`Legi.Messaging`: consumed/failed/parked/redelivered, console exporter); correlação por `MessageId` no log scope do consumer.
- **Operação:** migração de schema via modo `--migrate` (sai após migrar) + flag `RunMigrationsOnStartup` (default true em single-instance; false + step `--migrate` em multi-replica p/ evitar race); retenção que poda outbox processado / inbox consumido (mantém poison); comando `--reconcile-ratings` (Catalog) recomputa médias a partir das rows `BookRating` (backfill/drift, idempotente).
- **Não construído (YAGNI consciente):** recompute de drift feed/snapshot (nenhum drift observado; auditoria §8.1.4 provou consumers convergentes) e `CausationId` (seria sempre null — nenhum consumer republica; grafo de eventos é de um salto).

---

## 7. Padrões e Convenções

### 7.1 Estrutura de Projeto

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

web/legi-web/                  (React SPA)
├── src/
│   ├── app/                   (App, Layout, routes)
│   ├── components/ui/         (Avatar, Badge, BookCard, Button, Card, ProgressBar, StarRating)
│   ├── features/              (catalog, library, social — cada um com components, data, types)
│   ├── i18n/locales/          (en.json, pt-BR.json)
│   ├── lib/utils.ts           (cn() helper)
│   └── main.tsx
├── Dockerfile                 (node:22-alpine → nginx:alpine)
└── nginx.conf                 (SPA + reverse proxy)
```

### 7.2 Formato de Resposta de Erro

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

### 7.3 Formato de Paginação

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

### 7.4 HTTP Status Codes

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

## 8. Resumo de Endpoints

| Serviço | Endpoints | Status |
|---------|-----------|--------|
| Identity | 8 | ✅ Implementado (inclui `POST /auth/google`) |
| Catalog | 9 | ✅ Implementado (books: 5, authors: 2, tags: 2) |
| Library | 21 | ✅ Implementado (inclui `GET /library/by-book/{bookId}` e `POST /library/{userBookId}/reviews`; listas referenciam `bookId`) |
| Social | 24 | ✅ Implementado (inclui `GET /social/books/{bookId}/reviews`, Review Interactions: 4 e List social-state/follows: 3) |
| Web Frontend | 11 rotas | 🚧 Em desenvolvimento (inclui `/books/new`, `/books/:bookId` e as páginas de lista `/lists/new`, `/lists/:listId`, `/lists/:listId/edit`) |
| **Total** | **62 endpoints API + 11 rotas frontend** | |

*Além dos endpoints de domínio acima, cada API expõe `/swagger` e `/health` (health check de RabbitMQ + backlog do outbox, Fase 6 — ver §6.3).*

## 9. Resumo de Tabelas

| Serviço | Tabelas | Status |
|---------|---------|--------|
| Identity | 3 domínio + inbox/outbox | ✅ Migrado (users, refresh_tokens, external_logins — login social) |
| Catalog | 6 domínio + inbox/outbox | ✅ Migrado (inclui `book_ratings` — projeção por-usuário, Fase 5; `book_reviews` abandonada — resenhas vivem no Library/Social, Catalog mantém só `books.reviews_count`) |
| Library | 5 domínio + inbox/outbox | ✅ Migrado |
| Social | 8 domínio/read-model + inbox/outbox | ✅ Migrado (inclui `list_follows` — feature de listas customizadas) |
| **Total** | **22 tabelas de domínio/read-model + tabelas de mensageria por serviço** | |
