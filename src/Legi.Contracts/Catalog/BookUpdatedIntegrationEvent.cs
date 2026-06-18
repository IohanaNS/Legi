namespace Legi.Contracts.Catalog;

/// <summary>
/// Published when an existing book's data is updated in the catalog. Consumers
/// (Library, Social) use this to upsert their local BookSnapshot read model so
/// display data (title, authors, cover, page count) stays in sync.
///
/// AuthorDisplay is NOT carried — each consumer joins the Authors list according
/// to its own display convention.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, section 6.3.
/// </summary>
/// <param name="BookId">Catalog's book identifier; same UUID used by all consumers.</param>
/// <param name="Isbn">Book's ISBN at the moment of the update.</param>
/// <param name="Title">Book title.</param>
/// <param name="Authors">List of author names in order.</param>
/// <param name="CoverUrl">URL of the cover image, if known.</param>
/// <param name="PageCount">Number of pages, if known.</param>
/// <param name="WorkId">The work this book (edition) belongs to.</param>
public sealed record BookUpdatedIntegrationEvent(
    Guid BookId,
    string Isbn,
    string Title,
    List<string> Authors,
    string? CoverUrl,
    int? PageCount,
    Guid WorkId
) : IIntegrationEvent;
