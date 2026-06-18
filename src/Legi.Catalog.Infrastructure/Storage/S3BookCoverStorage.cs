using Amazon.S3;
using Amazon.S3.Model;
using Legi.Catalog.Application.Common.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Legi.Catalog.Infrastructure.Storage;

/// <summary>
/// Stores book covers in an S3-compatible bucket (MinIO in dev). Object keys are
/// owned here (<c>covers/{isbn}/{guid}.ext</c>); the persisted URL is the public
/// path the bucket is served under, so read-side DTOs use it verbatim. Mirrors
/// the Social profile-image store but against the separate <c>legi-covers</c>
/// bucket (locked decision 5).
/// </summary>
public sealed class S3BookCoverStorage : IBookCoverStorage
{
    // The keyed S3 client registration so Catalog's bucket/credentials stay
    // independent of any other AWS client in the container.
    public const string S3ClientKey = "catalog-covers";

    private readonly IAmazonS3 _client;
    private readonly CatalogStorageOptions _options;
    private readonly ILogger<S3BookCoverStorage> _logger;

    public S3BookCoverStorage(
        [FromKeyedServices(S3ClientKey)] IAmazonS3 client,
        IOptions<CatalogStorageOptions> options,
        ILogger<S3BookCoverStorage> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> StoreAsync(string ownerKey, CoverImage image, CancellationToken cancellationToken)
    {
        var normalizedKey = ownerKey.Replace("-", "").Replace(" ", "").Trim();
        var key = $"covers/{normalizedKey}/{Guid.NewGuid():N}.{image.Extension}";

        using var stream = new MemoryStream(image.Bytes, writable: false);

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = stream,
            ContentType = image.ContentType
        }, cancellationToken);

        return $"{_options.PublicBasePath}/{key}";
    }

    public async Task DeleteByUrlAsync(string url, CancellationToken cancellationToken)
    {
        var key = ToObjectKey(url);
        if (key is null)
            return;

        try
        {
            await _client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _options.Bucket,
                Key = key
            }, cancellationToken);
        }
        catch (AmazonS3Exception ex)
        {
            // Orphaned objects are harmless; never fail over cleanup.
            _logger.LogWarning(ex, "Failed to delete replaced cover {Key}", key);
        }
    }

    /// <summary>Strips the public prefix; returns null for URLs this store does not own.</summary>
    private string? ToObjectKey(string url)
    {
        var prefix = $"{_options.PublicBasePath}/";
        return url.StartsWith(prefix, StringComparison.Ordinal)
            ? url[prefix.Length..]
            : null;
    }
}
