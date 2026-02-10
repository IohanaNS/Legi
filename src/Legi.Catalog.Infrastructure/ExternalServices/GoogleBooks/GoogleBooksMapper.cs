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

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessiveNewlinesRegex();
}