# Legi

Sistema de gerenciamento pessoal de leitura com recursos sociais, construido com .NET 10, Clean Architecture, Domain-Driven Design, mensageria com RabbitMQ e frontend React/Vite.

## Requisitos

- [Docker](https://docs.docker.com/get-docker/) e Docker Compose
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) para desenvolvimento local
- [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) para criar/remover migrations: `dotnet tool install --global dotnet-ef`
- Yarn para rodar o frontend localmente em `web/legi-web`

## Como subir a aplicacao

### Opcao 1: Docker completo

Sobe as 4 APIs, 4 bancos PostgreSQL, RabbitMQ e o frontend.

#### 1. Configure as variaveis de ambiente

```bash
cp .env.example .env
openssl rand -base64 32
```

Cole o valor gerado em `Jwt__Secret` no `.env`. Os demais defaults sao adequados para desenvolvimento.

#### 2. Suba tudo

```bash
docker compose up -d --build
docker compose ps
```

As APIs aplicam migrations EF Core automaticamente no startup, de forma idempotente.

| Container | Servico | Porta |
|-----------|---------|-------|
| legi-web | Frontend + proxy Nginx | 3000 |
| legi-identity-api | Identity API | 5000 |
| legi-catalog-api | Catalog API | 5112 |
| legi-library-api | Library API | 5200 |
| legi-social-api | Social API | 5300 |
| legi-identity-db | PostgreSQL Identity | 5432 |
| legi-catalog-db | PostgreSQL Catalog | 5433 |
| legi-library-db | PostgreSQL Library | 5434 |
| legi-social-db | PostgreSQL Social | 5435 |
| legi-rabbitmq | RabbitMQ / Management UI | 5672 / 15672 |

#### 3. Acesse os servicos

| Servico | Swagger | Base URL |
|---------|---------|----------|
| Web | http://localhost:3000 | Nginx proxy para `/api/v1/*` |
| Identity | http://localhost:5000/swagger | `http://localhost:5000/api/v1/identity` |
| Catalog | http://localhost:5112/swagger | `http://localhost:5112/api/v1/catalog` |
| Library | http://localhost:5200/swagger | `http://localhost:5200/api/v1/library` |
| Social | http://localhost:5300/swagger | `http://localhost:5300/api/v1/social` |
| RabbitMQ UI | http://localhost:15672 | usuario `legi`, senha `legi_dev` |

#### 4. Teste rapido

```bash
curl -X POST http://localhost:5000/api/v1/identity/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "teste@email.com", "username": "teste", "password": "Senha123!"}'
```

#### 5. Seed de dados mock

Cria usuarios de desenvolvimento e livros no Catalog de forma idempotente:

```bash
docker compose build identity-api catalog-api
docker compose run --rm identity-api --seed-dev-data
docker compose run --rm catalog-api --seed-dev-data
```

Usuarios criados com senha `Password123!`:

| Username | Email |
|----------|-------|
| `alice_reader` | `alice.reader@example.com` |
| `bruno_books` | `bruno.books@example.com` |
| `carla_shelf` | `carla.shelf@example.com` |

#### Parando e reiniciando

```bash
docker compose down
docker compose down -v
docker compose up -d --build
```

### Opcao 2: Desenvolvimento local

Suba apenas a infraestrutura:

```bash
docker compose up -d identity-db catalog-db library-db social-db rabbitmq
```

Configure o `.env` e compile:

```bash
cp .env.example .env
dotnet build
```

Rode cada API em um terminal separado:

```bash
dotnet run --project src/Legi.Identity.Api/Legi.Identity.Api.csproj
dotnet run --project src/Legi.Catalog.Api/Legi.Catalog.Api.csproj
dotnet run --project src/Legi.Library.Api/Legi.Library.Api.csproj
dotnet run --project src/Legi.Social.Api/Legi.Social.Api.csproj
```

Rode o frontend:

```bash
cd web/legi-web
yarn
yarn dev
```

## Build e testes

```bash
dotnet build
dotnet test Legi.sln --settings tests/.runsettings
```

Os testes usam xUnit e coverlet. As metas em `tests/.runsettings` sao 75% de cobertura de linha e 65% de branch.

Frontend:

```bash
cd web/legi-web
yarn lint
yarn build
```

## Estrutura do projeto

```text
Legi/
├── src/
│   ├── Legi.SharedKernel/           # Base entities, ValueObject, mediator customizado
│   ├── Legi.Contracts/              # Integration events compartilhados
│   ├── Legi.Messaging/              # Outbox, inbox e RabbitMQ
│   ├── Legi.Identity.*/             # Autenticacao, usuarios, JWT
│   ├── Legi.Catalog.*/              # Catalogo global de livros
│   ├── Legi.Library.*/              # Biblioteca pessoal, listas, posts de leitura
│   └── Legi.Social.*/               # Perfis sociais, follows, feed, likes, comentarios
├── tests/                           # Testes unitarios e integracao por contexto
├── web/legi-web/                    # Frontend React 19 + Vite + Nginx proxy
├── Docs/                            # Arquitetura e decisoes
└── docker-compose.yml
```

Cada contexto backend segue as camadas:

```text
Domain          -> Entidades, Value Objects, eventos, interfaces de repositorio
Application     -> Commands, Queries, Validators, DTOs, event handlers
Infrastructure  -> EF Core, migrations, repositorios, servicos externos
Api             -> Controllers, Middleware, Program.cs
```

## Migrations

As APIs executam `Database.Migrate()` no startup. Para criar ou remover migrations manualmente:

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/Legi.<Servico>.Infrastructure \
  --startup-project src/Legi.<Servico>.Api

dotnet ef migrations remove \
  --project src/Legi.<Servico>.Infrastructure \
  --startup-project src/Legi.<Servico>.Api
```

Substitua `<Servico>` por `Identity`, `Catalog`, `Library` ou `Social`.

## API Endpoints

### Identity (`/api/v1/identity`)

| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| POST | `/auth/register` | Registrar usuario |
| POST | `/auth/login` | Login por email ou username |
| POST | `/auth/refresh` | Renovar access token |
| POST | `/auth/logout` | Invalidar refresh token |
| GET | `/users/me` | Perfil autenticado |
| DELETE | `/users/me` | Deletar conta |
| GET | `/users/{userId}` | Perfil publico basico |

### Catalog (`/api/v1/catalog`)

| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| GET | `/books` | Buscar livros com filtros e paginacao |
| GET | `/books/{bookId}` | Detalhes do livro |
| POST | `/books` | Cadastrar livro |
| PUT | `/books/{bookId}` | Atualizar livro |
| DELETE | `/books/{bookId}` | Excluir livro |
| GET | `/authors/search` | Buscar autores |
| GET | `/authors/popular` | Autores populares |
| GET | `/tags/search` | Buscar tags |
| GET | `/tags/popular` | Tags populares |

### Library (`/api/v1/library`)

| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| GET | `/` | Minha biblioteca |
| POST | `/` | Adicionar livro |
| PATCH | `/{userBookId}` | Atualizar status, wishlist ou progresso |
| DELETE | `/{userBookId}` | Remover livro |
| PUT | `/{userBookId}/rating` | Dar ou atualizar rating |
| DELETE | `/{userBookId}/rating` | Remover rating |
| GET | `/{userBookId}/posts` | Posts de leitura do livro |
| POST | `/{userBookId}/posts` | Criar post de leitura |
| PUT | `/posts/{postId}` | Editar post de leitura |
| DELETE | `/posts/{postId}` | Excluir post de leitura |
| GET | `/lists` | Minhas listas |
| POST | `/lists` | Criar lista |
| GET | `/lists/search` | Buscar listas publicas |
| GET | `/lists/{listId}` | Detalhes da lista |
| PATCH | `/lists/{listId}` | Atualizar lista |
| DELETE | `/lists/{listId}` | Excluir lista |
| GET | `/lists/{listId}/books` | Livros da lista |
| POST | `/lists/{listId}/books` | Adicionar livro a lista |
| DELETE | `/lists/{listId}/books/{userBookId}` | Remover livro da lista |

### Social (`/api/v1/social`)

| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| POST | `/follows` | Seguir usuario |
| DELETE | `/follows/{userId}` | Deixar de seguir usuario |
| GET | `/users/{userId}` | Perfil social publico |
| GET | `/users/{userId}/followers` | Seguidores |
| GET | `/users/{userId}/following` | Usuarios seguidos |
| GET | `/feed` | Feed autenticado |
| GET | `/users/{userId}/activity` | Atividade publica do usuario |
| POST | `/posts/{postId}/likes` | Curtir post |
| DELETE | `/posts/{postId}/likes` | Remover curtida de post |
| GET | `/posts/{postId}/comments` | Comentarios de post |
| POST | `/posts/{postId}/comments` | Comentar em post |
| POST | `/lists/{listId}/likes` | Curtir lista |
| DELETE | `/lists/{listId}/likes` | Remover curtida de lista |
| GET | `/lists/{listId}/comments` | Comentarios de lista |
| POST | `/lists/{listId}/comments` | Comentar em lista |
| DELETE | `/comments/{commentId}` | Excluir comentario |

## Documentacao

- [Arquitetura detalhada](Docs/ARCHITECTURE.md)
- [Decisoes de arquitetura - Messaging](Docs/MESSAGING-ARCHITECTURE-decisions.md)
- [Decisoes de arquitetura - Social](Docs/SOCIAL-ARCHITECTURE-decisions.md)
- [Decisoes de arquitetura - Library](Docs/LIBRARY-ARCHITECTURE-decisions.md)
- [Catalog APIs externas](Docs/CATALOG-ARCHITECTURE-external-apis.md)
- [Guia de testes](tests/TESTING_GUIDELINES.md)
