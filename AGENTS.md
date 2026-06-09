# Repository Guidelines

## Project Structure & Module Organization
This repository is a .NET 10 monorepo with a React frontend. Core backend code lives under `src/`, split by bounded context: `Legi.Identity.*`, `Legi.Catalog.*`, `Legi.Library.*`, `Legi.Social.*`, plus shared code in `src/Legi.SharedKernel` and contracts in `src/Legi.Contracts`. Tests live under `tests/`, with one project per domain/application slice. The frontend is in `web/legi-web/`. Architecture notes and design decisions are in `Docs/`.

## Build, Test, and Development Commands
- `dotnet build` builds the backend solution from the repo root.
- `dotnet test Legi.sln --settings tests/.runsettings` runs the full .NET test suite with the repository coverage settings.
- `docker compose up -d` starts the full local stack, including the three APIs and their PostgreSQL databases.
- `dotnet run --project src/Legi.Identity.Api/Legi.Identity.Api.csproj` runs a single API locally; replace `Identity` with `Catalog` or `Library` as needed.
- In `web/legi-web/`: `yarn dev` starts the frontend, `yarn build` type-checks and bundles it, and `yarn lint` runs ESLint.

## Coding Style & Naming Conventions
Follow `.editorconfig`: UTF-8, LF line endings, 4-space indentation for C#, and 2-space indentation for JSON/JS files. Prefer `var` where the codebase already does, keep C# accessibility explicit on non-interface members, and use PascalCase for types, methods, and test classes. Frontend files use `camelCase` for utilities and `PascalCase` for React components.

## Testing Guidelines
Tests use xUnit with `coverlet.collector`. Keep tests deterministic, AAA-shaped, and focused on one behavior. Name tests as `Method_Scenario_ExpectedBehavior`. Domain tests should validate invariants and domain events; application tests should mock repositories/services and verify orchestration. Coverage targets are 75% line and 65% branch.

## Commit & Pull Request Guidelines
Recent commits use scoped conventional messages such as `feat(identity): ...`, `refactor(library): ...`, and `docs(messaging): ...`. Keep commit subjects short, imperative, and scoped. Pull requests should explain the change, note affected services, link related issues when applicable, and include screenshots or API examples for UI or contract changes.

## Configuration Notes
Copy `.env.example` to `.env` before running locally and set `Jwt__Secret`. Database migrations are managed per service with `dotnet ef database update --project src/Legi.<Service>.Infrastructure --startup-project src/Legi.<Service>.Api`.
