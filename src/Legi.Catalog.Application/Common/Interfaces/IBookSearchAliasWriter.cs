namespace Legi.Catalog.Application.Common.Interfaces;

/// <summary>
/// Records associations between an external search query and the books that
/// query resolved to, so future local searches for the same term can find books
/// whose stored title/ISBN don't literally contain it.
///
/// External providers (Open Library / Google Books) match on titles, subtitles,
/// descriptions and every language edition, whereas local search only matches
/// the single stored title + ISBN. Without aliasing, a query like "redoma"
/// (the Portuguese title of "The Bell Jar") imports the canonical English book
/// that the user can then never find again by the term they typed.
/// </summary>
public interface IBookSearchAliasWriter
{
    /// <summary>
    /// Links the given books to the search <paramref name="query"/>.
    /// Idempotent: re-linking an existing (book, query) pair is a no-op.
    /// </summary>
    Task LinkAsync(
        string query,
        IReadOnlyCollection<Guid> bookIds,
        CancellationToken cancellationToken = default);
}
