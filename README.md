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
./scripts/gen-dev-keys.sh
```

O script grava um par RSA de desenvolvimento (`Jwt__PublicKey` /
`Jwt__PrivateKey`) e `Mfa__EncryptionKey` no `.env`. Os demais defaults sao
adequados para desenvolvimento.

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

### Compose de producao

O arquivo `docker-compose.prod.yml` exige todos os segredos no momento em que o
Compose carrega o arquivo, inclusive para `build`. Ele nao usa `.env.prod`
automaticamente; passe o arquivo com `--env-file`:

```bash
cp .env.prod.example .env.prod
$EDITOR .env.prod

docker compose --env-file .env.prod -f docker-compose.prod.yml build
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d
```

Para build e subida em um passo:

```bash
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --build
```

Sem `--env-file .env.prod`, erros como `required variable RabbitMq__Username is
missing a value` sao esperados.

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

## CI e seguranca

Os workflows em `.github/workflows` rodam build/testes backend, build do frontend,
scan de pacotes NuGet vulneraveis, gitleaks em historico completo e Trivy para
vulnerabilidades/segredos. As actions usam versoes compativeis com o runtime
Node 24 do GitHub Actions.

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

## APIs

Use os links Swagger da tabela "Acesse os servicos" para consultar contratos,
endpoints e payloads atualizados. O frontend acessa as APIs pelo proxy Nginx em
`http://localhost:3000/api/v1/*` no ambiente Docker.

## Documentacao

- [Arquitetura detalhada](Docs/ARCHITECTURE.md)
- [Decisoes de arquitetura - Messaging](Docs/MESSAGING-ARCHITECTURE-decisions.md)
- [Decisoes de arquitetura - Social](Docs/SOCIAL-ARCHITECTURE-decisions.md)
- [Decisoes de arquitetura - Library](Docs/LIBRARY-ARCHITECTURE-decisions.md)
- [Catalog APIs externas](Docs/CATALOG-ARCHITECTURE-external-apis.md)
- [Guia de testes](tests/TESTING_GUIDELINES.md)
