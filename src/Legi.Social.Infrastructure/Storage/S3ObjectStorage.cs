using Amazon.S3;
using Amazon.S3.Model;
using Legi.Social.Application.Common.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Legi.Social.Infrastructure.Storage;

/// <summary>
/// Stores profile images in an S3-compatible bucket. Object keys are owned here
/// (<c>avatars/{userId}/{guid}.ext</c>); the persisted URL is the public path
/// the bucket is served under, so read-side DTOs can use it verbatim.
/// </summary>
public sealed class S3ObjectStorage : IObjectStorage
{
    private readonly IAmazonS3 _client;
    private readonly StorageOptions _options;
    private readonly ILogger<S3ObjectStorage> _logger;

    public S3ObjectStorage(
        IAmazonS3 client,
        IOptions<StorageOptions> options,
        ILogger<S3ObjectStorage> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> PutProfileImageAsync(
        Guid userId,
        ProfileImageKind kind,
        ProcessedImage image,
        CancellationToken cancellationToken)
    {
        var folder = kind == ProfileImageKind.Avatar ? "avatars" : "banners";
        var key = $"{folder}/{userId}/{Guid.NewGuid():N}.{image.Extension}";

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
            // Orphaned objects are harmless; never fail the request over cleanup.
            _logger.LogWarning(ex, "Failed to delete replaced object {Key}", key);
        }
    }

    public async Task DeleteProfileImagesAsync(Guid userId, CancellationToken cancellationToken)
    {
        await DeletePrefixAsync($"avatars/{userId}/", cancellationToken);
        await DeletePrefixAsync($"banners/{userId}/", cancellationToken);
    }

    private async Task DeletePrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        try
        {
            string? continuationToken = null;
            do
            {
                var listed = await _client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = _options.Bucket,
                    Prefix = prefix,
                    ContinuationToken = continuationToken
                }, cancellationToken);

                if (listed.S3Objects.Count > 0)
                {
                    await _client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = _options.Bucket,
                        Objects = listed.S3Objects
                            .Select(o => new KeyVersion { Key = o.Key })
                            .ToList()
                    }, cancellationToken);
                }

                continuationToken = listed.IsTruncated
                    ? listed.NextContinuationToken
                    : null;
            } while (continuationToken is not null);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete profile image objects under {Prefix}", prefix);
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
