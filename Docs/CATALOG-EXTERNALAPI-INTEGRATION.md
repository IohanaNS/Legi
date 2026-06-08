# Plan: Authenticated Catalog Search With Async External Enrichment

## Summary

Use the existing GET /api/v1/catalog/books endpoint as the user search flow. The API returns local Catalog results immediately. When local results are insufficient, Catalog queues an internal async enrichment job that      
searches Open Library and Google Books, imports safe candidates, emits existing book events, and makes imported books visible on later searches.

Each implementation phase must end with targeted tests, dotnet build, and the relevant test command before moving to the next phase.

## Phase 1: Search API And UX Contract

- Require JWT on GET /api/v1/catalog/books.
- Extend SearchBooksQuery with AuthenticatedUserId.
- Keep existing Books and Pagination; add additive Enrichment metadata:                                                                                                                                                        
  Status, Message, RefreshAfterSeconds.

- Status values:                                                                                                                                                                                                               
  NotApplicable, NotNeeded, Queued, AlreadyQueued, RecentlyCompleted, FailedRecently.

- Enrichment is considered only for plain first-page text searches with too few local results.
- Frontend UX: show local results immediately; if status is Queued or AlreadyQueued, auto-refresh once after RefreshAfterSeconds.

Tests/build for this phase:

- Add/adjust unit tests for SearchBooksQueryHandler eligibility and metadata.
- Verify unauthorized search returns 401 if API-level tests exist; otherwise cover controller auth by inspection until integration API tests exist.
- Run dotnet build.
- Run dotnet test tests/Legi.Catalog.Application.Tests/Legi.Catalog.Application.Tests.csproj --settings tests/.runsettings.

## Phase 2: Catalog Job Persistence

- Add external_book_search_jobs in Catalog Infrastructure.
- Store:                                                                                                                                                                                                                       
  Id, QueryHash, Query, Status, RequestedByUserId, MaxResults, Attempts, NextRetryAt, CreatedAt, StartedAt, CompletedAt, Error.

- Normalize query by trim, lowercase, and whitespace collapse; hash normalized query.
- Enforce one active job per QueryHash.
- Add repository methods to enqueue, find active/recent jobs, claim pending jobs, and update status.
- Add EF migration for Catalog.

Tests/build for this phase:

- Add unit tests for query normalization and job enqueue decision.
- Add Catalog integration tests with CATALOG_TEST_DB for enqueue/dedupe persistence.
- Run dotnet build.
- Run dotnet test tests/Legi.Catalog.Application.Tests/Legi.Catalog.Application.Tests.csproj --settings tests/.runsettings.
- Run dotnet test tests/Legi.Catalog.Integration.Tests/Legi.Catalog.Integration.Tests.csproj --settings tests/.runsettings when CATALOG_TEST_DB is available.

## Phase 3: External Candidate Mapping

- Add Application-facing ExternalBookCandidate.
- Candidate fields:                                                                                                                                                                                                            
  Provider, ProviderBookId, Isbn10, Isbn13, Title, Authors, Synopsis, PageCount, Publisher, CoverUrl, Tags, Language, PublishedDate.

- Persist only Catalog-supported fields:                                                                                                                                                                                       
  ISBN, Title, Authors, Synopsis, PageCount, Publisher, CoverUrl, Tags.

- Do not persist provider name, provider ID, language, published date, or raw payload in v1.
- Implement Open Library search mapping.
- Implement Google Books search mapping.
- Keep provider DTOs internal to Infrastructure.

Tests/build for this phase:

- Add provider mapping tests with fake HTTP responses.
- Add tests for ISBN selection, tag extraction, author mapping, cover URL mapping, and partial metadata.
- Run dotnet build.
- Run Catalog application tests.
- Run provider/infrastructure tests if added as a separate test project; otherwise include them in Catalog application tests with mocked ports.

## Phase 4: Async Import Processor

- Add ProcessExternalBookSearchJobCommand.
- Add Catalog background worker that claims jobs with FOR UPDATE SKIP LOCKED.
- Worker queries both providers, merges/dedupes candidates, and processes up to 25.
- Dedupe by normalized ISBN first, then normalized title + first author.
- Create missing books with RequestedByUserId.
- Existing books only fill missing nullable fields:                                                                                                                                                                            
  Synopsis, PageCount, Publisher, CoverUrl.

- Add missing tags up to Book.MaxTags.
- Never overwrite title, authors, ISBN, ratings, review count, or creator.
- Provider failures are isolated; job failures retry with backoff and eventually mark Failed.

Tests/build for this phase:

- Add handler unit tests for import, skip, update-only-missing, dedupe, cap 25, and provider failure behavior.
- Add integration tests proving worker persists books/authors/tags/junction rows and does not duplicate records on repeated jobs.
- Run dotnet build.
- Run Catalog application tests.
- Run Catalog integration tests when CATALOG_TEST_DB is available.

## Phase 5: Events And BookSnapshot Propagation

- New async-imported books raise existing BookCreatedDomainEvent.
- Catalog publishes existing BookCreatedIntegrationEvent:                                                                                                                                                                      
  BookId, Isbn, Title, Authors, CoverUrl, PageCount.

- Library and Social consume that event and upsert BookSnapshot.
- Existing-book enrichment raises BookUpdatedDomainEvent only when snapshot-relevant fields change, especially CoverUrl or PageCount.
- Catalog publishes existing BookUpdatedIntegrationEvent with the same snapshot data shape.
- No new cross-service event contracts for v1.

Tests/build for this phase:

- Add tests proving imported books create outbox rows for BookCreatedIntegrationEvent.
- Add tests proving enrichment creates BookUpdatedIntegrationEvent only when relevant fields changed.
- Add/adjust Library and Social snapshot handler tests only if event payload expectations change; otherwise assert no contract change is needed.
- Run dotnet build.
- Run dotnet test Legi.sln --settings tests/.runsettings.

## Phase 6: Documentation And Frontend Follow-Up

- Update architecture docs and the new plan doc to describe async enrichment, job table, and eventual consistency.
- Frontend implementation can be a later task:
    - replace mock catalog search with API search;
    - show enrichment status;
    - auto-refresh once when enrichment is queued.

- No frontend dependency is required for backend correctness.

Tests/build for this phase:

- Run dotnet build.
- Run dotnet test Legi.sln --settings tests/.runsettings.
- If frontend is touched later, run:                                                                                                                                                                                           
  cd web/legi-web && yarn lint && yarn build.

## Assumptions

- Search is authenticated.
- Existing search endpoint remains the only user-facing search API.
- Enrichment is eventual consistency.
- Search response can add Enrichment metadata.
- Catalog owns the internal job table.
- RabbitMQ remains for cross-service integration events, not internal Catalog job scheduling.
- No provider/source metadata is persisted on Book in v1.
- Every phase must be verified before moving to the next phase.  