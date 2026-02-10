# External Book Data Integration — Architecture Decision Record

## Context

The Legi Catalog service needs to enrich book data from external APIs during book creation.
When a user provides an ISBN, the system should attempt to fetch metadata (title, authors, synopsis,
page count, publisher, cover image) from external providers, merging it with any user-provided data.

## Decision

### Provider Chain: Open Library → Google Books

| Provider | Priority | API Key | Rate Limits | Strengths | Weaknesses |
|----------|----------|---------|-------------|-----------|------------|
| Open Library | 1 (first) | None | Unofficial (be respectful) | Free, constructable cover URLs, rich metadata | Authors returned as references (need 2nd call), description often at Work level |
| Google Books | 2 (fallback) | Optional (recommended) | 100/day without key, 1,000/day with key | Author names directly, good descriptions | Descriptions are HTML, covers are lower quality, API key recommended |

### Architecture: Ports & Adapters

```
┌─────────────────────────────────────────────────────────┐
│ Application Layer                                       │
│                                                         │
│  IBookDataProvider (port)                               │
│  └── ExternalBookData (normalized record)               │
│                                                         │
│  CreateBookCommandHandler                               │
│  └── Fetch → Merge → Validate → Create                 │
└─────────────────┬───────────────────────────────────────┘
                  │ implements
┌─────────────────▼───────────────────────────────────────┐
│ Infrastructure Layer                                    │
│                                                         │
│  BookDataProvider (orchestrator)                         │
│  ├── IExternalBookClient (internal interface)            │
│  ├── OpenLibraryClient (priority 1)                     │
│  │   ├── OpenLibraryModels.cs                           │
│  │   ├── OpenLibraryMapper.cs                           │
│  │   └── OpenLibrarySettings.cs                         │
│  └── GoogleBooksClient (priority 2)                     │
│      ├── GoogleBooksModels.cs                           │
│      ├── GoogleBooksMapper.cs                           │
│      └── GoogleBooksSettings.cs                         │
└─────────────────────────────────────────────────────────┘
```

### Merge Strategy

User input ALWAYS takes priority over external data, per field:

```
final_title     = user.Title     ?? api.Title     ?? THROW ValidationException
final_authors   = user.Authors   ?? api.Authors   ?? THROW ValidationException
final_synopsis  = user.Synopsis  ?? api.Synopsis   (optional)
final_pageCount = user.PageCount ?? api.PageCount  (optional)
final_publisher = user.Publisher ?? api.Publisher   (optional)
final_coverUrl  = user.CoverUrl  ?? api.CoverUrl   (optional)
```

### Resilience

- Individual provider failures are caught and logged as warnings — never thrown
- If Open Library fails or returns no data, the chain automatically tries Google Books
- If ALL providers fail, book creation proceeds with user-provided data only
- Mandatory field validation happens AFTER merge — only fails if no source provided title/authors
- Timeouts are configurable per provider (default: 10 seconds each)

### Validation Strategy Change

Title and Authors are validated AFTER merge in the handler (Option A), not in FluentValidation.
FluentValidation still handles: ISBN format, string lengths, tag limits, field format validation.
The handler handles: "do we have enough data after merging?" — this is application logic, not input validation.

## File Placement Guide

```
Legi.Catalog.Application/
└── Common/
    └── Interfaces/
        └── IBookDataProvider.cs          ← Port + ExternalBookData record

Legi.Catalog.Infrastructure/
├── ExternalServices/
│   ├── IExternalBookClient.cs            ← Internal interface (not exposed to Application)
│   ├── BookDataProvider.cs               ← Orchestrator (implements IBookDataProvider)
│   ├── OpenLibrary/
│   │   ├── OpenLibraryClient.cs          ← HTTP client
│   │   ├── OpenLibraryModels.cs          ← JSON deserialization DTOs
│   │   ├── OpenLibraryMapper.cs          ← OL response → ExternalBookData
│   │   └── OpenLibrarySettings.cs        ← Configuration (Options pattern)
│   └── GoogleBooks/
│       ├── GoogleBooksClient.cs          ← HTTP client
│       ├── GoogleBooksModels.cs          ← JSON deserialization DTOs
│       ├── GoogleBooksMapper.cs          ← GB response → ExternalBookData
│       └── GoogleBooksSettings.cs        ← Configuration (Options pattern)
└── ExternalServicesRegistration.cs       ← DI registration extension method

Legi.Catalog.Api/
└── appsettings.json                      ← Add ExternalServices section
```

## DI Registration

In your existing `DependencyInjection.cs` or `Program.cs`:

```csharp
services.AddExternalBookServices(configuration);
```

## Configuration (appsettings.json)

```json
{
  "ExternalServices": {
    "OpenLibrary": {
      "Enabled": true,
      "TimeoutSeconds": 10
    },
    "GoogleBooks": {
      "Enabled": true,
      "TimeoutSeconds": 10,
      "ApiKey": ""
    }
  }
}
```

Environment variable override for API key (production):
```
ExternalServices__GoogleBooks__ApiKey=your-google-api-key
```

## Open Items

1. **CreateBookCommand DTO**: Decide if Title/Authors become nullable (enables ISBN-only creation UX)
2. **Caching**: Not implemented yet — volume is low (one lookup per book creation). Consider later.
3. **Google Books API Key**: Get one from Google Cloud Console for production use.