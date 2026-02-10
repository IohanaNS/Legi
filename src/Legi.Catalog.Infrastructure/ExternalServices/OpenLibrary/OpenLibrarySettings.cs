namespace Legi.Catalog.Infrastructure.ExternalServices.OpenLibrary;

/// <summary>
/// Configuration for the Open Library API.
/// Bound from the appsettings.json section "ExternalServices:OpenLibrary".
/// 
/// Open Library is free and requires no API key.
/// They do ask for a User-Agent header to identify your application.
/// API docs: https://openlibrary.org/developers/api
/// </summary>
public class OpenLibrarySettings
{
    public const string SectionName = "ExternalServices:OpenLibrary";

    /// <summary>
    /// Request timeout in seconds. Default: 10.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Whether to use this provider at all. Default: true.
    /// Set to false to disable Open Library in the fallback chain.
    /// </summary>
    public bool Enabled { get; set; } = true;
}