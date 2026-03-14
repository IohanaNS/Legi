# Legi

Sistema de gerenciamento pessoal de leitura com recursos sociais, construido com .NET 10, Clean Architecture e Domain-Driven Design.

## Requisitos

- [Docker](https://docs.docker.com/get-docker/) e Docker Compose
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (apenas para desenvolvimento local)
- [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (`dotnet tool install --global dotnet-ef`) (apenas para migrations)

## Como subir a aplicacao

### Opcao 1: Docker (recomendado)

Sobe todos os servicos (3 APIs + 3 bancos PostgreSQL) com um unico comando.

#### 1. Configure as variaveis de ambiente

```bash
cp .env.example .env
```

Gere um secret para o JWT e cole no `.env`:

```bash
openssl rand -base64 32
```

Edite o `.env`:

```env
Jwt__Secret=SEU_SECRET_GERADO_AQUI
```

> Os demais valores do `.env` ja vem com defaults adequados para desenvolvimento.

#### 2. Suba tudo

```bash
docker compose up -d
```

Isso builda as 3 APIs e inicia todos os containers. Na primeira vez pode demorar alguns minutos.

#### 3. Verifique se esta rodando

```bash
docker compose ps
```

Todos os 6 containers devem estar `running` (ou `healthy` para os bancos):

| Container | Servico | Porta |
|-----------|---------|-------|
| legi-identity-db | PostgreSQL (Identity) | 5432 |
| legi-catalog-db | PostgreSQL (Catalog) | 5433 |
| legi-library-db | PostgreSQL (Library) | 5434 |
| legi-identity-api | Identity API | 5000 |
| legi-catalog-api | Catalog API | 5112 |
| legi-library-api | Library API | 5200 |

#### 4. Aplique as migrations

As migrations criam as tabelas nos bancos. Execute uma vez (ou sempre que houver novas migrations):

```bash
# Identity
dotnet ef database update --project src/Legi.Identity.Infrastructure --startup-project src/Legi.Identity.Api

# Catalog
dotnet ef database update --project src/Legi.Catalog.Infrastructure --startup-project src/Legi.Catalog.Api

# Library
dotnet ef database update --project src/Legi.Library.Infrastructure --startup-project src/Legi.Library.Api
```

> **Nota**: As migrations rodam localmente e se conectam aos bancos via `localhost`. Certifique-se de que o `.env` tem as connection strings corretas (os defaults funcionam com o docker-compose).

#### 5. Acesse as APIs

| Servico | Swagger | Base URL |
|---------|---------|----------|
| Identity | http://localhost:5000/swagger | `http://localhost:5000/api/v1/identity` |
| Catalog | http://localhost:5112/swagger | `http://localhost:5112/api/v1/catalog` |
| Library | http://localhost:5200/swagger | `http://localhost:5200/api/v1/library` |

#### 6. Teste rapido

Registre um usuario:

```bash
curl -X POST http://localhost:5000/api/v1/identity/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email": "teste@email.com", "username": "teste", "password": "Senha123!", "displayName": "Teste"}'
```

#### Parando e reiniciando

```bash
# Parar tudo (preserva dados)
docker compose down

# Parar e apagar volumes (apaga todos os dados dos bancos)
docker compose down -v

# Rebuild apos mudancas no codigo
docker compose up -d --build
```

---

### Opcao 2: Desenvolvimento local

Para rodar as APIs diretamente na maquina (necessario .NET 10 SDK).

#### 1. Suba apenas os bancos

```bash
docker compose up -d identity-db catalog-db library-db
```

#### 2. Configure o `.env`

```bash
cp .env.example .env
# Edite o Jwt__Secret conforme descrito acima
```

#### 3. Aplique as migrations

```bash
dotnet ef database update --project src/Legi.Identity.Infrastructure --startup-project src/Legi.Identity.Api
dotnet ef database update --project src/Legi.Catalog.Infrastructure --startup-project src/Legi.Catalog.Api
dotnet ef database update --project src/Legi.Library.Infrastructure --startup-project src/Legi.Library.Api
```

#### 4. Build e rode

```bash
dotnet build
```

Rode cada API em um terminal separado:

```bash
# Terminal 1
dotnet run --project src/Legi.Identity.Api/Legi.Identity.Api.csproj

# Terminal 2
dotnet run --project src/Legi.Catalog.Api/Legi.Catalog.Api.csproj

# Terminal 3
dotnet run --project src/Legi.Library.Api/Legi.Library.Api.csproj
```

#### 5. Rode os testes

```bash
dotnet test
```

## Estrutura do Projeto

```
Legi/
├── src/
│   ├── Legi.SharedKernel/           # Base classes, custom Mediator
│   ├── Legi.Identity.*/             # Autenticacao, usuarios, JWT
│   ├── Legi.Catalog.*/              # Catalogo global de livros
│   └── Legi.Library.*/              # Biblioteca pessoal, listas, posts
├── tests/
│   ├── Legi.Identity.Domain.Tests/
│   ├── Legi.Identity.Application.Tests/
│   ├── Legi.Catalog.Domain.Tests/
│   └── Legi.Catalog.Application.Tests/
├── Docs/                            # Documentacao de arquitetura
└── docker-compose.yml
```

Cada servico segue a mesma estrutura em camadas:

```
Domain          → Entidades, Value Objects, interfaces de repositorio
Application     → Commands, Queries, Validators, DTOs
Infrastructure  → EF Core, Repositorios, Servicos externos
Api             → Controllers, Middleware, Program.cs
```

## Criando Novas Migrations

```bash
# Criar migration
dotnet ef migrations add NomeDaMigration \
  --project src/Legi.<Servico>.Infrastructure \
  --startup-project src/Legi.<Servico>.Api

# Aplicar
dotnet ef database update \
  --project src/Legi.<Servico>.Infrastructure \
  --startup-project src/Legi.<Servico>.Api

# Reverter (remove a ultima migration nao aplicada)
dotnet ef migrations remove \
  --project src/Legi.<Servico>.Infrastructure \
  --startup-project src/Legi.<Servico>.Api
```

Substitua `<Servico>` por `Identity`, `Catalog` ou `Library`.

## API Endpoints

### Identity (`/api/v1/identity`)
| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| POST | `/auth/register` | Registrar usuario |
| POST | `/auth/login` | Login |
| POST | `/auth/refresh` | Renovar token |
| POST | `/auth/logout` | Logout |
| GET | `/users/me` | Meu perfil |
| PATCH | `/users/me` | Atualizar perfil |
| DELETE | `/users/me` | Deletar conta |
| GET | `/users/{userId}` | Perfil publico |

### Catalog (`/api/v1/catalog`)
| Metodo | Endpoint | Descricao |
|--------|----------|-----------|
| GET | `/books` | Buscar livros |
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
| PATCH | `/{userBookId}` | Atualizar status/progresso |
| DELETE | `/{userBookId}` | Remover livro |
| PUT | `/{userBookId}/rating` | Dar rating |
| DELETE | `/{userBookId}/rating` | Remover rating |
| GET | `/{userBookId}/posts` | Posts do livro |
| POST | `/{userBookId}/posts` | Criar post |
| PUT | `/posts/{postId}` | Editar post |
| DELETE | `/posts/{postId}` | Excluir post |
| GET | `/lists` | Minhas listas |
| POST | `/lists` | Criar lista |
| GET | `/lists/{listId}` | Detalhes da lista |
| PATCH | `/lists/{listId}` | Atualizar lista |
| DELETE | `/lists/{listId}` | Excluir lista |
| GET | `/lists/{listId}/books` | Livros da lista |
| POST | `/lists/{listId}/books` | Adicionar livro a lista |
| DELETE | `/lists/{listId}/books/{userBookId}` | Remover livro da lista |
| GET | `/lists/search` | Buscar listas publicas |

## Documentacao

- [Arquitetura detalhada](Docs/ARCHITECTURE.md)
- [Decisoes de arquitetura - Library](Docs/LIBRARY-ARCHITECTURE-decisions.md)
- [Decisoes de arquitetura - Catalog APIs externas](Docs/CATALOG-ARCHITECTURE-external-apis.md)
