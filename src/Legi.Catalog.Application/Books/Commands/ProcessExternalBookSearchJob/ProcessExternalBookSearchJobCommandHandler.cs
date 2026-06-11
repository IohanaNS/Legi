using System.Text.RegularExpressions;
using Legi.Catalog.Application.Books;
using Legi.Catalog.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Catalog.Application.Books.Commands.ProcessExternalBookSearchJob;

public partial class ProcessExternalBookSearchJobCommandHandler(
    IBookDataProvider bookDataProvider,
    BookImportService bookImportService,
    IBookSearchAliasWriter searchAliasWriter)
    : IRequestHandler<ProcessExternalBookSearchJobCommand, ProcessExternalBookSearchJobResponse>
{
    public async Task<ProcessExternalBookSearchJobResponse> Handle(
        ProcessExternalBookSearchJobCommand request,
        CancellationToken cancellationToken)
    {
        var candidates = await bookDataProvider.SearchAsync(
            request.SearchTerm,
            request.MaxResults,
            cancellationToken);

        var imported = 0;
        var updated = 0;
        var skipped = 0;
        var matchedBookIds = new List<Guid>();

        foreach (var candidate in Deduplicate(candidates).Take(request.MaxResults))
        {
            var outcome = await bookImportService.ImportCandidateAsync(
                candidate,
                request.RequestedByUserId,
                cancellationToken);

            switch (outcome.Result)
            {
                case BookImportResult.Imported:
                    imported++;
                    break;
                case BookImportResult.Updated:
                    updated++;
                    break;
                case BookImportResult.Skipped:
                    skipped++;
                    break;
            }

            // A candidate that resolved to a book — whether newly imported, enriched,
            // or an already-complete existing match — is relevant to this query, so
            // alias it even when the import itself was skipped.
            if (outcome.BookId.HasValue)
            {
                matchedBookIds.Add(outcome.BookId.Value);
            }
        }

        await searchAliasWriter.LinkAsync(request.SearchTerm, matchedBookIds, cancellationToken);

        return new ProcessExternalBookSearchJobResponse(
            candidates.Count,
            imported,
            updated,
            skipped);
    }

    private static IEnumerable<ExternalBookCandidate> Deduplicate(IEnumerable<ExternalBookCandidate> candidates)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            var key = GetDeduplicationKey(candidate);
            if (key is null)
            {
                yield return candidate;
                continue;
            }

            if (!seen.Add(key))
            {
                continue;
            }

            yield return candidate;
        }
    }

    private static string? GetDeduplicationKey(ExternalBookCandidate candidate)
    {
        var isbn = SelectIsbn(candidate);
        if (!string.IsNullOrWhiteSpace(isbn))
        {
            return $"isbn:{NormalizeIsbn(isbn)}";
        }

        var title = NormalizeText(candidate.Title);
        var firstAuthor = NormalizeText(candidate.Authors.FirstOrDefault());

        return string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(firstAuthor)
            ? null
            : $"title-author:{title}:{firstAuthor}";
    }

    private static string? SelectIsbn(ExternalBookCandidate candidate)
    {
        return !string.IsNullOrWhiteSpace(candidate.Isbn13)
            ? candidate.Isbn13
            : candidate.Isbn10;
    }

    private static string NormalizeIsbn(string isbn)
    {
        return isbn.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : WhitespaceRegex().Replace(value.Trim(), " ");
    }

    private static string? NormalizeText(string? value)
    {
        return Clean(value)?.ToLowerInvariant();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
