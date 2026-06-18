namespace Legi.Catalog.Infrastructure.Storage;

/// <summary>
/// Guards for fetching cover images from the internet. <c>CreateManualAsync</c>
/// accepts a user-supplied cover URL, so the fetcher is an SSRF surface — these
/// bound what it will pull and from where.
/// </summary>
public sealed class CoverSourceOptions
{
    public const string SectionName = "CoverSource";

    /// <summary>
    /// Hosts the fetcher is allowed to pull from. A candidate URL whose host is
    /// not (a subdomain of) one of these is rejected before any request — blocks
    /// SSRF to internal addresses via a user-supplied URL. Defaults cover the
    /// provider endpoints we synthesize/return today.
    /// </summary>
    public string[] AllowedHosts { get; set; } =
    [
        "covers.openlibrary.org",
        "books.google.com",
        "books.googleusercontent.com",
        "lh3.googleusercontent.com"
    ];

    /// <summary>Per-URL fetch timeout. Keeps the inline fan-out snappy.</summary>
    public int PerFetchTimeoutSeconds { get; set; } = 3;

    /// <summary>
    /// Hard cap on the whole fan-out across all candidates, so the inline acquire
    /// on the manual-add request can't stack up to N×PerFetch. Hitting it returns
    /// "no cover" (the background discovery job retries later).
    /// </summary>
    public int OverallTimeoutSeconds { get; set; } = 5;

    /// <summary>Reject bodies larger than this (defensive against huge responses).</summary>
    public int MaxBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Reject suspiciously tiny bodies (blank/1x1 placeholders).</summary>
    public int MinBytes { get; set; } = 1024;

    /// <summary>
    /// Minimum width and height in pixels. Catches Open Library's tiny/blank
    /// placeholder, which decodes fine but isn't a usable cover.
    /// </summary>
    public int MinDimension { get; set; } = 100;
}
