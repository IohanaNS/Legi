using System.Net;
using Legi.Catalog.Application.Common.Interfaces;

namespace Legi.Catalog.Infrastructure.ExternalServices.OpenLibrary;

/// <summary>
/// Open Library's API client.
/// 
/// API docs: https://openlibrary.org/developers/api
/// 
/// Key characteristics:
/// - Free, no API key required
/// - Authors are returned as references ("/authors/OL34184A") and need a second call to resolve names
/// - Descriptions often live at the Work level, not the Edition level
/// - Cover images are constructable from cover IDs without an API call
/// - Rate limits: Be respectful, no official limit but they ask for User-Agent
/// </summary>
internal class OpenLibraryClient(HttpClient httpClient, ILogger<OpenLibraryClient> logger) : IExternalBookClient
{
    private const string SearchFields =
        "key,title,author_name,isbn,cover_i,subject,publisher,number_of_pages_median,language,first_publish_year";

    public string ProviderName => "OpenLibrary";
    public int Priority => 1;

    public async Task<ExternalBookData?> GetByIsbnAsync(string isbn, CancellationToken ct)
    {
        try
        {
            // 1. Fetch edition by ISBN
            //    This endpoint returns a redirect then the edition JSON.
            //    HttpClient follows redirects by default.
            var edition = await httpClient.GetFromJsonAsync<OpenLibraryEdition>(
                $"/isbn/{isbn}.json", ct);

            if (edition is null)
                return null;

            // 2. Resolve author names (OL returns references, not names)
            var authorNames = await ResolveAuthorsAsync(edition.Authors, ct);

            // 3. Get description — try edition first, fall back to Work level
            var synopsis = edition.Description;
            if (string.IsNullOrWhiteSpace(synopsis) && edition.Works is { Count: > 0 })
            {
                synopsis = await FetchWorkDescriptionAsync(edition.Works[0].Key!, ct);
            }

            // 4. Map to normalized model
            return OpenLibraryMapper.ToExternalBookData(edition, authorNames, synopsis);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogDebug("ISBN {Isbn} not found in Open Library", isbn);
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Open Library API request failed for ISBN {Isbn}", isbn);
            return null;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Open Library API request timed out for ISBN {Isbn}", isbn);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected error fetching from Open Library for ISBN {Isbn}", isbn);
            return null;
        }
    }

    public async Task<IReadOnlyList<ExternalBookCandidate>> SearchAsync(
        string searchTerm,
        int maxResults,
        CancellationToken ct)
    {
        try
        {
            var limit = Math.Clamp(maxResults, 1, 50);
            var url = $"/search.json?q={Uri.EscapeDataString(searchTerm)}&limit={limit}&fields={SearchFields}";
            var response = await httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(url, ct);

            return response?.Docs?
                .Select(OpenLibraryMapper.ToExternalBookCandidate)
                .Where(candidate => candidate is not null)
                .Cast<ExternalBookCandidate>()
                .ToList() ?? [];
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Open Library API search failed for query {SearchTerm}", searchTerm);
            return [];
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Open Library API search timed out for query {SearchTerm}", searchTerm);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unexpected error searching Open Library for query {SearchTerm}", searchTerm);
            return [];
        }
    }

    /// <summary>
    /// Resolves author references into actual names.
    /// Each reference requires a separate HTTP call: GET /authors/{id}.json
    /// We cap at 10 to respect our domain's max authors limit.
    /// </summary>
    private async Task<List<string>> ResolveAuthorsAsync(
        List<OpenLibraryAuthorRef>? authorRefs, CancellationToken ct)
    {
        if (authorRefs is null || authorRefs.Count == 0)
            return [];

        var names = new List<string>();

        foreach (var authorRef in authorRefs.Take(10))
        {
            if (string.IsNullOrWhiteSpace(authorRef.Key))
                continue;

            try
            {
                var author = await httpClient.GetFromJsonAsync<OpenLibraryAuthor>(
                    $"{authorRef.Key}.json", ct);

                if (!string.IsNullOrWhiteSpace(author?.Name))
                    names.Add(author.Name);
            }
            catch (Exception ex)
            {
                // Partial author resolution is better than none.
                // Log and continue with the others.
                logger.LogWarning(ex,
                    "Failed to resolve Open Library author {AuthorKey}", authorRef.Key);
            }
        }

        return names;
    }

    /// <summary>
    /// Fetches description from the Work level when the Edition has none.
    /// Many editions lack descriptions, but their parent Work usually has one.
    /// </summary>
    private async Task<string?> FetchWorkDescriptionAsync(string workKey, CancellationToken ct)
    {
        try
        {
            var work = await httpClient.GetFromJsonAsync<OpenLibraryWork>(
                $"{workKey}.json", ct);
            return work?.Description;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex,
                "Failed to fetch Work description from Open Library for {WorkKey}", workKey);
            return null;
        }
    }
}
