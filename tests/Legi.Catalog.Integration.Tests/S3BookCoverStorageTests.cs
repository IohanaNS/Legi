using Amazon.S3;
using Amazon.S3.Model;
using Legi.Catalog.Application.Common.Storage;
using Legi.Catalog.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Legi.Catalog.Integration.Tests;

/// <summary>
/// Pure unit tests (no network) for the cover blob key/URL layout. Guards the
/// regression the Docker smoke test caught: the key must not repeat the "covers/"
/// segment that the public base path already carries (no <c>/covers/covers/…</c>).
/// </summary>
public class S3BookCoverStorageTests
{
    private static (S3BookCoverStorage storage, List<string> putKeys) Build()
    {
        var putKeys = new List<string>();
        var client = new Mock<IAmazonS3>();
        client
            .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => putKeys.Add(req.Key))
            .ReturnsAsync(new PutObjectResponse());

        var options = Options.Create(new CatalogStorageOptions
        {
            Bucket = "legi-covers",
            PublicBasePath = "/covers"
        });

        return (new S3BookCoverStorage(client.Object, options, NullLogger<S3BookCoverStorage>.Instance), putKeys);
    }

    [Fact]
    public async Task StoreAsync_ProducesSingleCoversPrefix_InUrl()
    {
        var (storage, putKeys) = Build();
        var image = new CoverImage([1, 2, 3], "image/webp", "webp");

        var url = await storage.StoreAsync("9780132350884", image, CancellationToken.None);

        Assert.StartsWith("/covers/9780132350884/", url);
        Assert.EndsWith(".webp", url);
        Assert.DoesNotContain("/covers/covers/", url);
        // The stored object key must not carry the public-path segment.
        Assert.StartsWith("9780132350884/", putKeys.Single());
    }
}
