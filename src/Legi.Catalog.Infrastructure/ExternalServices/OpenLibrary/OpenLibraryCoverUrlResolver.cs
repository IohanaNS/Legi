using Legi.Catalog.Application.Common.Interfaces;

namespace Legi.Catalog.Infrastructure.ExternalServices.OpenLibrary;

/// <summary>
/// Builds Open Library's ISBN-addressable cover URL. Open Library serves a cover
/// for most ISBNs at a deterministic address, so any book with a valid ISBN can
/// get a cover even when the provider search results didn't include one.
///
/// "?default=false" makes Open Library return 404 for ISBNs it has no cover for,
/// instead of a blank placeholder image — so the frontend can fall back to its
/// own placeholder on the image error.
/// </summary>
internal sealed class OpenLibraryCoverUrlResolver : IBookCoverUrlResolver
{
    public string? ResolveByIsbn(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }

        var normalized = isbn.Replace("-", "").Replace(" ", "").Trim();
        return $"https://covers.openlibrary.org/b/isbn/{normalized}-L.jpg?default=false";
    }
}
