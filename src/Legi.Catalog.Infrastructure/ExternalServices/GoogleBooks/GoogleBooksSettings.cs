namespace Legi.Catalog.Infrastructure.ExternalServices.GoogleBooks;

/// <summary>
/// Configuration for the Google Books API.
/// Bound from appsettings.json section "ExternalServices:GoogleBooks".
/// 
/// API key is optional — Google Books works without one but with strict rate limits (~100 req/day).
/// With a key: 1,000 req/day free tier.
/// 
/// To get an API key:
/// 1. Go to https://console.cloud.google.com
/// 2. Create a project (or select existing)
/// 3. Enable "Books API"
/// 4. Create credentials → API Key
/// 5. (Optional) Restrict the key to Books API only
/// </summary>
public class GoogleBooksSettings
{
    public const string SectionName = "ExternalServices:GoogleBooks";

    /// <summary>
    /// Google Books API key. Optional for development, recommended for production.
    /// When null/empty, requests are made without a key (lower rate limits).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Request timeout in seconds. Default: 10.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Whether to use this provider at all. Default: true.
    /// Set to false to disable Google Books in the fallback chain.
    /// </summary>
    public bool Enabled { get; set; } = true;

    public bool HasApiKey => !string.IsNullOrWhiteSpace(ApiKey);
}   