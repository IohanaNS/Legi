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

    /// <summary>
    /// Extracts language code from Open Library's reference format.
    /// Input: "/languages/eng" → Output: "eng"
    /// </summary>
    private static string? ParseLanguageCode(List<OpenLibraryRef>? languages)
    {
        var langKey = languages?.FirstOrDefault()?.Key;
        return langKey?.Split('/').LastOrDefault();
    }
}