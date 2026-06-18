using Legi.Catalog.Application.Common.Storage;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Infrastructure.Storage;

/// <summary>
/// The single acquire-cover operation: fan out + validate via
/// <see cref="IBookCoverSource"/>, then on success upload to the owned bucket via
/// <see cref="IBookCoverStorage"/> and return the blob URL. Swallows everything —
/// a cover is cosmetic and must never fail or block a book — so callers get a
/// blob URL or <c>null</c> and nothing else.
/// </summary>
public sealed class BookCoverAcquisition : IBookCoverAcquisition
{
    private readonly IBookCoverSource _source;
    private readonly IBookCoverStorage _storage;
    private readonly ILogger<BookCoverAcquisition> _logger;

    public BookCoverAcquisition(
        IBookCoverSource source,
        IBookCoverStorage storage,
        ILogger<BookCoverAcquisition> logger)
    {
        _source = source;
        _storage = storage;
        _logger = logger;
    }

    public async Task<string?> AcquireAsync(
        string isbn,
        IReadOnlyList<string?> candidateUrls,
        CancellationToken cancellationToken)
    {
        try
        {
            var image = await _source.FetchAsync(candidateUrls, cancellationToken);
            if (image is null)
                return null;

            return await _storage.StoreAsync(isbn, image, cancellationToken);
        }
        catch (Exception ex)
        {
            // MinIO hiccup or anything unexpected: never fail the book over a cover.
            // The background discovery job is the safety net for transient misses.
            _logger.LogWarning(ex, "Cover acquisition failed for ISBN {Isbn}", isbn);
            return null;
        }
    }
}
