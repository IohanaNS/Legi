namespace Legi.Catalog.Application.Common.Storage;

/// <summary>
/// The single "acquire-cover" operation used by every path (inline manual add,
/// bulk import worker, background discovery): fan out across candidate URLs,
/// validate by fetching, and on success upload to the owned bucket — returning
/// the blob URL. Returns <c>null</c> when no real cover was found, MinIO was
/// unreachable, or anything else went wrong. <strong>Never throws and never
/// blocks the book</strong>: a cover is cosmetic, so acquisition is always
/// best-effort and a <c>null</c> result is a valid, complete (cover-less) book.
/// </summary>
public interface IBookCoverAcquisition
{
    /// <summary>
    /// Validates the candidate URLs (provider covers first, ISBN endpoint last)
    /// and, on the first real image, stores it and returns the owned blob URL;
    /// otherwise <c>null</c>.
    /// </summary>
    Task<string?> AcquireAsync(
        string isbn,
        IReadOnlyList<string?> candidateUrls,
        CancellationToken cancellationToken);
}
