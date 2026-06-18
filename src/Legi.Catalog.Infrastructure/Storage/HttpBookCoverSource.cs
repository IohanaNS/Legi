using Legi.Catalog.Application.Common.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace Legi.Catalog.Infrastructure.Storage;

/// <summary>
/// Fetches cover images over HTTP, validates them by actually decoding, and
/// returns the first real one. Fetching is the validation: a synthetic ISBN URL
/// that 404s or serves a blank placeholder simply fails to produce a real image.
/// All the SSRF / size / content-type / dimension guards live here because this
/// is the one place a user-supplied URL gets dereferenced.
/// </summary>
public sealed class HttpBookCoverSource : IBookCoverSource
{
    public const string HttpClientName = "book-cover-source";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CoverSourceOptions _options;
    private readonly ILogger<HttpBookCoverSource> _logger;

    public HttpBookCoverSource(
        IHttpClientFactory httpClientFactory,
        IOptions<CoverSourceOptions> options,
        ILogger<HttpBookCoverSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CoverImage?> FetchAsync(
        IReadOnlyList<string?> candidateUrls,
        CancellationToken cancellationToken)
    {
        // Overall cap across the whole fan-out so a slow chain of providers can't
        // stack up to N×per-fetch on the inline (manual-add) path.
        using var overallCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        overallCts.CancelAfter(TimeSpan.FromSeconds(_options.OverallTimeoutSeconds));

        foreach (var candidate in candidateUrls)
        {
            if (overallCts.IsCancellationRequested)
                break;
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            var image = await TryFetchAsync(candidate, overallCts.Token);
            if (image is not null)
                return image;
        }

        return null;
    }

    private async Task<CoverImage?> TryFetchAsync(string candidateUrl, CancellationToken cancellationToken)
    {
        if (!IsAllowed(candidateUrl, out var uri))
        {
            _logger.LogDebug("Rejected cover URL (host not allow-listed): {Url}", candidateUrl);
            return null;
        }

        // Per-URL timeout so one slow provider can't stall the whole fan-out.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(_options.PerFetchTimeoutSeconds));

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(
                uri, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return null;

            if (response.Content.Headers.ContentLength is { } declared && declared > _options.MaxBytes)
                return null;

            var bytes = await response.Content.ReadAsByteArrayAsync(timeoutCts.Token);
            return Validate(bytes);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            // Transient / timeout / unreachable — a normal "no cover here" outcome.
            // Don't surface as an error; the caller treats null as "try next / later".
            _logger.LogDebug(ex, "Cover fetch failed for {Url}", candidateUrl);
            return null;
        }
    }

    /// <summary>
    /// Validates bytes as a real, usable cover and derives the content type and
    /// extension from the <em>decoded</em> format (headers can lie). Returns null
    /// for anything that isn't a decodable image of at least the minimum size.
    /// </summary>
    private CoverImage? Validate(byte[] bytes)
    {
        if (bytes.Length < _options.MinBytes || bytes.Length > _options.MaxBytes)
            return null;

        try
        {
            var info = Image.Identify(bytes);
            if (info is null)
                return null;

            if (info.Width < _options.MinDimension || info.Height < _options.MinDimension)
                return null;

            var format = info.Metadata.DecodedImageFormat;
            if (format is null)
                return null;

            var extension = format.FileExtensions.FirstOrDefault() ?? "img";
            return new CoverImage(bytes, format.DefaultMimeType, extension);
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
        {
            // The body claimed image/* but doesn't decode — not a real cover.
            return null;
        }
    }

    /// <summary>
    /// SSRF guard: the URL must be absolute http(s) and its host must be (a
    /// subdomain of) an allow-listed host. Everything else is rejected before any
    /// network call, so a user-supplied URL can't reach internal addresses.
    /// </summary>
    private bool IsAllowed(string candidateUrl, out Uri uri)
    {
        if (!Uri.TryCreate(candidateUrl, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            uri = null!;
            return false;
        }

        uri = parsed;
        var host = parsed.Host;
        return _options.AllowedHosts.Any(allowed =>
            host.Equals(allowed, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("." + allowed, StringComparison.OrdinalIgnoreCase));
    }
}
