using System.Text.RegularExpressions;
using Legi.Catalog.Application.Common.Interfaces;

namespace Legi.Catalog.Infrastructure.ExternalServices.GoogleBooks;

/// <summary>
/// Maps Google Books API responses to our normalized ExternalBookData.
/// Handles Google-specific quirks: HTML descriptions, http:// image URLs.
/// </summary>
internal static partial class GoogleBooksMapper
{
    public static ExternalBookData ToExternalBookData(GoogleBooksVolumeInfo volumeInfo)
    {
        return new ExternalBookData
        {
            Title = volumeInfo.Title,
            Authors = volumeInfo.Authors,
            Synopsis = StripHtmlTags(volumeInfo.Description),
            PageCount = volumeInfo.PageCount,
            Publisher = volumeInfo.Publisher,
            CoverUrl = GetBestCoverUrl(volumeInfo.ImageLinks),
            Language = volumeInfo.Language,
            ProviderName = "GoogleBooks"
        };
    }

    public static ExternalBookCandidate? ToExternalBookCandidate(GoogleBooksVolume volume)
    {
        var volumeInfo = volume.VolumeInfo;
        if (volumeInfo is null
            || string.IsNullOrWhiteSpace(volumeInfo.Title)
            || volumeInfo.Authors is null
            || volumeInfo.Authors.Count == 0)
        {
            return null;
        }

        var (isbn10, isbn13) = SelectIsbns(volumeInfo.IndustryIdentifiers);
        if (isbn10 is null && isbn13 is null)
        {
            return null;
        }

        return new ExternalBookCandidate
        {
            Provider = "GoogleBooks",
            ProviderBookId = volume.Id,
            Isbn10 = isbn10,
            Isbn13 = isbn13,
            Title = volumeInfo.Title,
            Authors = volumeInfo.Authors,
            Synopsis = StripHtmlTags(volumeInfo.Description),
            PageCount = volumeInfo.PageCount,
            Publisher = volumeInfo.Publisher,
            CoverUrl = GetBestCoverUrl(volumeInfo.ImageLinks),
            Tags = volumeInfo.Categories ?? [],
            Language = volumeInfo.Language,
            PublishedDate = volumeInfo.PublishedDate
        };
    }

    /// <summary>
    /// Google Books descriptions contain HTML tags (b, i, br, p).
    /// We strip them for clean plain text storage.
    /// </summary>
    private static string? StripHtmlTags(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;

        // Replace <br> and <p> tags with newlines before stripping
        var text = html
            .Replace("<br>", "\n")
            .Replace("<br/>", "\n")
            .Replace("<br />", "\n")
            .Replace("</p>", "\n");

        text = HtmlTagRegex().Replace(text, string.Empty);

        // Clean up excessive whitespace
        text = ExcessiveNewlinesRegex().Replace(text.Trim(), "\n\n");

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>
    /// Picks the best available cover URL, preferring larger sizes.
    /// Google Books sometimes serves http:// URLs — we upgrade to https://.
    /// </summary>
    private static string? GetBestCoverUrl(GoogleBooksImageLinks? imageLinks)
    {
        if (imageLinks is null)
            return null;

        var url = imageLinks.Medium
            ?? imageLinks.Small
            ?? imageLinks.Thumbnail
            ?? imageLinks.SmallThumbnail;

        return UpgradeToHttps(url);
    }

    private static string? UpgradeToHttps(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        return url.Replace("http://", "https://");
    }

    private static (string? Isbn10, string? Isbn13) SelectIsbns(
        IEnumerable<GoogleBooksIndustryIdentifier>? identifiers)
    {
        if (identifiers is null)
        {
            return (null, null);
        }

        string? isbn10 = null;
        string? isbn13 = null;

        foreach (var identifier in identifiers)
        {
            var normalized = NormalizeIsbn(identifier.Identifier);
            if (normalized.Length == 13 && (identifier.Type == "ISBN_13" || isbn13 is null))
            {
                isbn13 = normalized;
            }
            else if (normalized.Length == 10 && (identifier.Type == "ISBN_10" || isbn10 is null))
            {
                isbn10 = normalized;
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

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessiveNewlinesRegex();
}
