# Testing Guidelines (Identity + Catalog)

## Core Principles
- Keep unit tests deterministic: avoid real time, random values, and external I/O unless explicitly controlled.
- Follow Arrange-Act-Assert (AAA) with one clear behavior under test.
- Name tests as `Method_Scenario_ExpectedBehavior`.
- Assert behavior, not implementation details.

## Factory-First Test Data
- Use factories for all complex setup.
- Keep factory defaults valid and explicit.
- Override only what the scenario needs.
- Use `CreateRandom` only when uniqueness is required by the scenario.

## Boundaries
- Domain tests: no mocks, test invariants and domain events.
- Application tests: mock repositories/services, verify orchestration and error handling.
- Do not hit DB, network, filesystem, or system clock in unit tests.

## Quality Gates
- Run tests with `tests/.runsettings`.
- Coverage threshold: line `75%`, branch `65%` (total).

## Commands
- From repo root: `dotnet test Legi.sln --settings tests/.runsettings`
- Focus Catalog domain: `dotnet test tests/Legi.Catalog.Domain.Tests/Legi.Catalog.Domain.Tests.csproj --settings tests/.runsettings`
