namespace Legi.Social.Infrastructure.Storage;

/// <summary>
/// Binds the <c>Storage</c> configuration section. Targets an S3-compatible
/// endpoint (MinIO locally; S3 / R2 / Azure Blob via the S3 gateway in prod).
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>S3 service endpoint, e.g. <c>http://minio:9000</c>.</summary>
    public string Endpoint { get; set; } = string.Empty;

    public string Bucket { get; set; } = "legi-media";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Public URL prefix under which the bucket is served to browsers (nginx
    /// proxies this to the bucket). Persisted URLs are <c>{PublicBasePath}/{key}</c>.
    /// </summary>
    public string PublicBasePath { get; set; } = "/media";
}
