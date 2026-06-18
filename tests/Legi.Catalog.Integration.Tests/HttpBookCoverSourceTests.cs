using System.Net;
using Legi.Catalog.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Legi.Catalog.Integration.Tests;

/// <summary>
/// Pure unit tests (no DB, no network) for the cover fetch+validate fan-out and
/// its SSRF guard. A canned <see cref="HttpMessageHandler"/> stands in for the
/// providers so we can prove: only real, big-enough, decodable images on
/// allow-listed hosts are accepted, and the fan-out takes the first valid one.
/// </summary>
public class HttpBookCoverSourceTests
{
    private static readonly CoverSourceOptions Options = new()
    {
        AllowedHosts = ["covers.openlibrary.org", "books.google.com"],
        MinDimension = 100,
        MinBytes = 100,
        MaxBytes = 10 * 1024 * 1024,
        PerFetchTimeoutSeconds = 5
    };

    private static byte[] PngBytes(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    private static HttpBookCoverSource BuildSource(StubHandler handler)
    {
        var factory = new StubHttpClientFactory(handler);
        return new HttpBookCoverSource(
            factory, OptionsWrap(Options), NullLogger<HttpBookCoverSource>.Instance);
    }

    private static IOptions<CoverSourceOptions> OptionsWrap(CoverSourceOptions o) => Microsoft.Extensions.Options.Options.Create(o);

    [Fact]
    public async Task FetchAsync_ReturnsFirstValidImage_FromAllowedHost()
    {
        var handler = new StubHandler();
        handler.Map("https://covers.openlibrary.org/a.jpg",
            HttpStatusCode.OK, "image/png", PngBytes(200, 300));

        var source = BuildSource(handler);

        var result = await source.FetchAsync(["https://covers.openlibrary.org/a.jpg"], CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("image/png", result!.ContentType);
        Assert.Equal("png", result.Extension);
    }

    [Fact]
    public async Task FetchAsync_SkipsTooSmallImage_AndTakesNextValid()
    {
        var handler = new StubHandler();
        handler.Map("https://covers.openlibrary.org/tiny.png",
            HttpStatusCode.OK, "image/png", PngBytes(10, 10)); // below MinDimension
        handler.Map("https://books.google.com/big.png",
            HttpStatusCode.OK, "image/png", PngBytes(150, 220));

        var source = BuildSource(handler);

        var result = await source.FetchAsync(
            [null, "https://covers.openlibrary.org/tiny.png", "https://books.google.com/big.png"],
            CancellationToken.None);

        // The 10x10 was rejected; the fan-out fell through to the 150x220 on the
        // next allow-listed host. Both URLs were attempted.
        Assert.NotNull(result);
        Assert.Equal("png", result!.Extension);
        Assert.Equal(2, handler.Requests);
    }

    [Fact]
    public async Task FetchAsync_RejectsNonImageBody_EvenWith200()
    {
        var handler = new StubHandler();
        handler.Map("https://covers.openlibrary.org/notimage",
            HttpStatusCode.OK, "image/png", "<html>404 page</html>"u8.ToArray());

        var source = BuildSource(handler);

        var result = await source.FetchAsync(["https://covers.openlibrary.org/notimage"], CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchAsync_RejectsHostNotOnAllowlist_WithoutFetching()
    {
        var handler = new StubHandler();
        // No mapping registered → if it tried to fetch, the handler would throw.
        var source = BuildSource(handler);

        var result = await source.FetchAsync(
            ["http://169.254.169.254/latest/meta-data/", "https://evil.example.com/x.png"],
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(0, handler.Requests);
    }

    [Fact]
    public async Task FetchAsync_TreatsNon200AsNoCover()
    {
        var handler = new StubHandler();
        handler.Map("https://covers.openlibrary.org/missing.png",
            HttpStatusCode.NotFound, "image/png", []);

        var source = BuildSource(handler);

        var result = await source.FetchAsync(["https://covers.openlibrary.org/missing.png"], CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode Status, string ContentType, byte[] Body)> _map = new();

        public int Requests { get; private set; }

        public void Map(string url, HttpStatusCode status, string contentType, byte[] body)
            => _map[url] = (status, contentType, body);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            var url = request.RequestUri!.ToString();
            if (!_map.TryGetValue(url, out var entry))
                throw new InvalidOperationException($"Unexpected request to {url}");

            var response = new HttpResponseMessage(entry.Status);
            var content = new ByteArrayContent(entry.Body);
            content.Headers.TryAddWithoutValidation("Content-Type", entry.ContentType);
            response.Content = content;
            return Task.FromResult(response);
        }
    }
}
