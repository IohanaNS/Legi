using System.Text.Json.Serialization;

namespace Legi.Catalog.Infrastructure.ExternalServices.GoogleBooks;

/// <summary>
/// Maps to: GET https://www.googleapis.com/books/v1/volumes?q=isbn:{isbn}
/// The search endpoint returns a list of volumes (usually just one for ISBN queries).
/// 
/// Google Books API docs: https://developers.google.com/books/docs/v1/reference/volumes
/// </summary>
internal record GoogleBooksSearchResponse
{
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; init; }

    [JsonPropertyName("items")]
    public List<GoogleBooksVolume>? Items { get; init; }
}

internal record GoogleBooksVolume
{
    [JsonPropertyName("volumeInfo")]
    public GoogleBooksVolumeInfo? VolumeInfo { get; init; }
}

/// <summary>
/// The volumeInfo object contains the book's metadata.
/// 
/// Notable quirks:
/// - authors[] contains actual names (unlike Open Library which returns references)
/// - description is HTML-formatted (contains &lt;b&gt;, &lt;i&gt;, &lt;br&gt; tags)
/// - imageLinks.thumbnail URLs sometimes use http:// instead of https://
/// - categories[] contains subject categories like "Fiction", "Science", etc.
/// </summary>
internal record GoogleBooksVolumeInfo
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("authors")]
    public List<string>? Authors { get; init; }

    /// <summary>
    /// Synopsis/description. WARNING: Contains HTML tags (b, i, br).
    /// Must be stripped before use.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("pageCount")]
    public int? PageCount { get; init; }

    [JsonPropertyName("publisher")]
    public string? Publisher { get; init; }

    [JsonPropertyName("imageLinks")]
    public GoogleBooksImageLinks? ImageLinks { get; init; }

    /// <summary>
    /// ISO 639-1 two-letter language code (e.g. "en", "pt", "fr").
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; init; }

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; init; }

    [JsonPropertyName("publishedDate")]
    public string? PublishedDate { get; init; }
}

internal record GoogleBooksImageLinks
{
    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; init; }

    [JsonPropertyName("smallThumbnail")]
    public string? SmallThumbnail { get; init; }

    [JsonPropertyName("small")]
    public string? Small { get; init; }

    [JsonPropertyName("medium")]
    public string? Medium { get; init; }
}