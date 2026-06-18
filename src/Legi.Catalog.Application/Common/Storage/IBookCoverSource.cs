namespace Legi.Catalog.Application.Common.Storage;

/// <summary>
/// Fetches a real cover image by fanning out across candidate URLs (provider
/// covers, then the ISBN-addressable endpoint) and returning the first one that
/// passes validation — HTTP 200, an <c>image/*</c> body that actually decodes,
/// and minimum dimensions/byte-size. Fetching <em>is</em> the validation.
///
/// This is the only SSRF surface for user-supplied cover URLs, so the host
/// allowlist, size cap, content-type and timeout guards live in the
/// implementation. Returns <c>null</c> when no candidate yields a real cover —
/// never throws for a bad/unreachable URL (that is a normal "no cover" outcome).
/// </summary>
public interface IBookCoverSource
{
    /// <summary>
    /// Tries each candidate URL in order and returns the bytes of the first that
    /// validates as a real image, or <c>null</c> if none do. Null/blank entries
    /// are skipped. Distinguishes nothing for the caller — a transient fetch
    /// failure and a confirmed "no image" both surface as <c>null</c> here; the
    /// retry policy that cares about the difference lives one layer up.
    /// </summary>
    Task<CoverImage?> FetchAsync(
        IReadOnlyList<string?> candidateUrls,
        CancellationToken cancellationToken);
}
