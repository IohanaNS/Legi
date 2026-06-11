namespace Legi.Social.Application.Common.Storage;

/// <summary>
/// Abstraction over an S3-compatible object store (MinIO in dev, S3/R2/Azure in
/// prod). Owns the key layout and the mapping between a stored object and the
/// public URL persisted on the profile.
/// </summary>
public interface IObjectStorage
{
    /// <summary>
    /// Stores a processed profile image and returns the public URL to persist
    /// (e.g. <c>/media/avatars/{userId}/{guid}.webp</c>).
    /// </summary>
    Task<string> PutProfileImageAsync(
        Guid userId,
        ProfileImageKind kind,
        ProcessedImage image,
        CancellationToken cancellationToken);

    /// <summary>
    /// Best-effort deletion of a previously stored object, addressed by the
    /// public URL that was persisted on the profile. No-op when the URL is not
    /// one this store owns.
    /// </summary>
    Task DeleteByUrlAsync(string url, CancellationToken cancellationToken);
}
