using System.Net;
using System.Net.Http.Json;
using Legi.Catalog.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Legi.Catalog.Infrastructure.ExternalServices.GoogleBooks;

/// <summary>
/// Google Books API client.
/// 
/// API docs: https://developers.google.com/books/docs/v1/using
/// 
/// Key characteristics:
/// - Works without API key (limited to ~100 req/day) or with key (1,000 req/day free)
/// - ISBN search via: GET /books/v1/volumes?q=isbn:{isbn}
/// - Returns author names directly (no second call needed, unlike Open Library)
/// - Description is HTML-formatted and needs stripping
/// - Image URLs may use http:// instead of https://
/// </summary>
internal class GoogleBooksClient : IExternalBookClient
{
    private readonly HttpClient _httpClient;
    private readonly GoogleBooksSettings _settings;
    private readonly ILogger<GoogleBooksClient> _logger;

    public GoogleBooksClient(
        HttpClient httpClient,
        IOptions<GoogleBooksSettings> settings,
        ILogger<GoogleBooksClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public string ProviderName => "GoogleBooks";
    public int Priority => 2;

    public async Task<ExternalBookData?> GetByIsbnAsync(string isbn, CancellationToken ct)
    {
        try
        {
            var url = BuildSearchUrl(isbn);

            var response = await _httpClient.GetFromJsonAsync<GoogleBooksSearchResponse>(url, ct);

            if (response is null or { TotalItems: 0 } || response.Items is null)
            {
                _logger.LogDebug("ISBN {Isbn} not found in Google Books", isbn);
                return null;
            }

            var volume = response.Items.FirstOrDefault();
            if (volume?.VolumeInfo is null)
                return null;

            return GoogleBooksMapper.ToExternalBookData(volume.VolumeInfo);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Google Books API rate limit exceeded for ISBN {Isbn}", isbn);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Google Books API request failed for ISBN {Isbn}", isbn);
            return null;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Google Books API request timed out for ISBN {Isbn}", isbn);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error fetching from Google Books for ISBN {Isbn}", isbn);
            return null;
        }
    }

    private string BuildSearchUrl(string isbn)
    {
        var url = $"/books/v1/volumes?q=isbn:{isbn}";

        if (_settings.HasApiKey)
            url += $"&key={_settings.ApiKey}";

        return url;
    }
}