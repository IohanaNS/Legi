namespace Legi.Catalog.Application.Common.Interfaces;

/// <summary>
/// Resolves a deterministic fallback cover image URL for a book by ISBN, used
/// when no external provider returned a cover. Implemented in Infrastructure
/// against a source that exposes ISBN-addressable covers (Open Library).
/// </summary>
public interface IBookCoverUrlResolver
{
    /// <summary>
    /// Returns a cover URL derived from the ISBN, or null when one can't be built.
    /// The URL is not verified to exist — callers should treat a broken image as
    /// "no cover" (the URL is built to 404 rather than serve a blank placeholder).
    /// </summary>
    string? ResolveByIsbn(string isbn);
}
