namespace Legi.Contracts.Catalog;

/// <summary>
/// Published when a new book is added to the catalog. Consumers (Library, Social)
/// use this to create their local BookSnapshot read model.
///
/// AuthorDisplay is NOT carried — each consumer joins the Authors list according
/// to its own display convention.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, section 6.3.
/// </summary>
/// <param name="BookId">Catalog's book identifier; same UUID used by all consumers.</param>
/// <param name="Isbn">Book's ISBN at creation time.</param>
/// <param name="Title">Book title.</param>
/// <param name="Authors">List of author names in order.</param>
/// <param name="CoverUrl">URL of the cover image, if known.</param>
/// <param name="PageCount">Number of pages, if known.</param>
public sealed record BookCreatedIntegrationEvent(
    Guid BookId,
    string Isbn,
    string Title,
    List<string> Authors,
    string? CoverUrl,
    int? PageCount
) : IIntegrationEvent;
