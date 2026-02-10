using Legi.Catalog.Application.Common.Interfaces;

namespace Legi.Catalog.Infrastructure.ExternalServices;

/// <summary>
/// Orchestrates the fallback chain across multiple external book data providers.
/// Tries each provider in priority order and returns the first result with usable data.
/// 
/// Architecture notes:
/// - Implements the Application layer's IBookDataProvider port
/// - Consumes internal IExternalBookClient implementations via DI
/// - Individual provider failures are logged but never propagated
/// - "Usable data" means at least a title or authors (partial data with only
///   covers/publishers isn't useful enough to stop the chain)
/// </summary>
internal class BookDataProvider : IBookDataProvider
{
    private readonly IExternalBookClient[] _orderedClients;
    private readonly ILogger<BookDataProvider> _logger;

    public BookDataProvider(
        IEnumerable<IExternalBookClient> clients,
        ILogger<BookDataProvider> logger)
    {
        _orderedClients = clients.OrderBy(c => c.Priority).ToArray();
        _logger = logger;
    }

    public async Task<ExternalBookData?> GetByIsbnAsync(string isbn, CancellationToken ct)
    {
        foreach (var client in _orderedClients)
        {
            _logger.LogDebug(
                "Trying {Provider} for ISBN {Isbn}",
                client.ProviderName, isbn);

            var result = await client.GetByIsbnAsync(isbn, ct);

            if (result is not null && HasMinimumUsableData(result))
            {
                _logger.LogInformation(
                    "Book data found via {Provider} for ISBN {Isbn}",
                    client.ProviderName, isbn);
                return result;
            }

            _logger.LogDebug(
                "{Provider} returned no usable data for ISBN {Isbn}",
                client.ProviderName, isbn);
        }

        _logger.LogInformation(
            "No external provider found data for ISBN {Isbn}", isbn);
        return null;
    }

    /// <summary>
    /// Determines if the returned data is useful enough to stop the fallback chain.
    /// A result with only covers or publishers but no title/authors isn't worth stopping for.
    /// </summary>
    private static bool HasMinimumUsableData(ExternalBookData data)
    {
        return !string.IsNullOrWhiteSpace(data.Title)
            || (data.Authors is { Count: > 0 });
    }
}