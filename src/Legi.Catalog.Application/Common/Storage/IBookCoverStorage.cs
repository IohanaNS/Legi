namespace Legi.Catalog.Application.Common.Storage;

/// <summary>
/// Owns book covers in an S3-compatible bucket (MinIO in dev), separate from the
/// Social profile-image bucket. Once a cover is here it is immune to the external
/// providers disappearing — that is the real Plan B for provider resilience.
/// </summary>
public interface IBookCoverStorage
{
    /// <summary>
    /// Uploads a validated cover and returns the public URL to persist on the
    /// book (e.g. <c>/covers/{ownerKey}/{guid}.webp</c>). The key layout is owned
    /// here; <paramref name="ownerKey"/> is just a path-safe grouping segment
    /// (the ISBN on the import path, the book id on the manual-upload path).
    /// </summary>
    Task<string> StoreAsync(string ownerKey, CoverImage image, CancellationToken cancellationToken);

    /// <summary>
    /// Best-effort deletion of a previously stored cover, addressed by the public
    /// URL persisted on the book. No-op for URLs this store does not own (e.g. a
    /// legacy external provider URL).
    /// </summary>
    Task DeleteByUrlAsync(string url, CancellationToken cancellationToken);
}
