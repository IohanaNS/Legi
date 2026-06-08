using Legi.Catalog.Application.Common.Interfaces;

namespace Legi.Catalog.Infrastructure.ExternalServices.OpenLibrary;

/// <summary>
/// Maps Open Library API responses to our normalized ExternalBookData.
/// Stateless and pure — all transformation logic centralized here.
/// </summary>
internal static class OpenLibraryMapper
{
    public static ExternalBookData ToExternalBookData(
        OpenLibraryEdition edition,
        List<string> resolvedAuthorNames,
        string? synopsis)
    {
        return new ExternalBookData
        {
            Title = edition.Title,
            Authors = resolvedAuthorNames.Count > 0 ? resolvedAuthorNames : null,
            Synopsis = synopsis,
            PageCount = edition.NumberOfPages,
            Publisher = edition.Publishers?.FirstOrDefault(),
            CoverUrl = BuildCoverUrl(edition.Covers),
            Language = ParseLanguageCode(edition.Languages),
            ProviderName = "OpenLibrary"
        };
    }

    public static ExternalBookCandidate? ToExternalBookCandidate(OpenLibrarySearchDoc doc)
    {
        var (isbn10, isbn13) = SelectIsbns(doc.Isbns);

        if (string.IsNullOrWhiteSpace(doc.Title)
            || doc.AuthorNames is null
            || doc.AuthorNames.Count == 0
            || (isbn10 is null && isbn13 is null))
        {
            return null;
        }

        return new ExternalBookCandidate
        {
            Provider = "OpenLibrary",
            ProviderBookId = doc.Key,
            Isbn10 = isbn10,
            Isbn13 = isbn13,
            Title = doc.Title,
            Authors = doc.AuthorNames,
            PageCount = doc.NumberOfPagesMedian,
            Publisher = doc.Publishers?.FirstOrDefault(),
            CoverUrl = BuildCoverUrl(doc.CoverId),
            Tags = doc.Subjects?.Take(10).ToList() ?? [],
            Language = doc.Languages?.FirstOrDefault(),
            PublishedDate = doc.FirstPublishYear?.ToString()
        };
    }

    /// <summary>
    /// Constructs a cover URL from Open Library's internal cover IDs.
    /// Format: https://covers.openlibrary.org/b/id/{coverId}-{size}.jpg
    /// Sizes: S (small), M (medium), L (large)
    /// </summary>
    private static string? BuildCoverUrl(List<long>? covers)
    {
        var coverId = covers?.FirstOrDefault();
        return coverId is > 0
            ? $"https://covers.openlibrary.org/b/id/{coverId}-L.jpg"
            : null;
    }

    private static string? BuildCoverUrl(long? coverId)
    {
        return coverId is > 0
            ? $"https://covers.openlibrary.org/b/id/{coverId}-L.jpg"
            : null;
    }

    /// <summary>
    /// Extracts language code from Open Library's reference format.
    /// Input: "/languages/eng" → Output: "eng"
    /// </summary>
    private static string? ParseLanguageCode(List<OpenLibraryRef>? languages)
    {
        var langKey = languages?.FirstOrDefault()?.Key;
        return langKey?.Split('/').LastOrDefault();
    }

    private static (string? Isbn10, string? Isbn13) SelectIsbns(IEnumerable<string>? isbns)
    {
        if (isbns is null)
        {
            return (null, null);
        }

        string? isbn10 = null;
        string? isbn13 = null;

        foreach (var isbn in isbns.Select(NormalizeIsbn))
        {
            if (isbn13 is null && isbn.Length == 13)
            {
                isbn13 = isbn;
            }
            else if (isbn10 is null && isbn.Length == 10)
            {
                isbn10 = isbn;
            }

            if (isbn10 is not null && isbn13 is not null)
            {
                break;
            }
        }

        return (isbn10, isbn13);
    }

    private static string NormalizeIsbn(string? isbn)
    {
        return string.IsNullOrWhiteSpace(isbn)
            ? string.Empty
            : isbn.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
    }
}
