namespace Legi.Catalog.Infrastructure.Storage;

/// <summary>
/// Binds the Catalog <c>Storage</c> section. Same S3-compatible MinIO instance as
/// Social, but a <strong>separate bucket</strong> for covers (locked decision 5):
/// reuse the infra, not the code, across bounded contexts.
/// </summary>
public sealed class CatalogStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>S3 service endpoint, e.g. <c>http://minio:9000</c>.</summary>
    public string Endpoint { get; set; } = string.Empty;

    public string Bucket { get; set; } = "legi-covers";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Public URL prefix the bucket is served under (nginx proxies it). Persisted
    /// cover URLs are <c>{PublicBasePath}/{key}</c>.
    /// </summary>
    public string PublicBasePath { get; set; } = "/covers";
}
