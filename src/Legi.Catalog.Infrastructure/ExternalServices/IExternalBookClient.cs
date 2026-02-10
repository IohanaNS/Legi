using Legi.Catalog.Application.Common.Interfaces;

namespace Legi.Catalog.Infrastructure.ExternalServices;

/// <summary>
/// Internal contract for individual book data providers.
/// NOT exposed to Application layer — only <see cref="BookDataProvider"/> uses this.
/// Each implementation wraps a specific external API (Open Library, Google Books, etc.).
/// </summary>
internal interface IExternalBookClient
{
    /// <summary>
    /// Display name for logging (e.g. "OpenLibrary", "GoogleBooks").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Determines fallback order. Lower value = tried first.
    /// OpenLibrary = 1, GoogleBooks = 2.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Attempts to fetch book data from this specific provider.
    /// Returns null if not found. Must never throw — catch and log internally.
    /// </summary>
    Task<ExternalBookData?> GetByIsbnAsync(string isbn, CancellationToken ct);
}