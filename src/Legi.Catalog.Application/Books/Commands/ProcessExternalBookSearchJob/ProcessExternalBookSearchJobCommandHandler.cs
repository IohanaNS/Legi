using System.Text.RegularExpressions;
using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Application.Books.Commands.ProcessExternalBookSearchJob;

public partial class ProcessExternalBookSearchJobCommandHandler(
    IBookDataProvider bookDataProvider,
    IBookRepository bookRepository,
    IBookSearchAliasWriter searchAliasWriter,
    IBookCoverUrlResolver coverUrlResolver,
    ILogger<ProcessExternalBookSearchJobCommandHandler> logger)
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
            var outcome = await ImportCandidateAsync(candidate, request.RequestedByUserId, cancellationToken);

            switch (outcome.Result)
            {
                case ImportResult.Imported:
                    imported++;
                    break;
                case ImportResult.Updated:
                    updated++;
                    break;
                case ImportResult.Skipped:
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

    private async Task<ImportOutcome> ImportCandidateAsync(
        ExternalBookCandidate candidate,
        Guid requestedByUserId,
        CancellationToken cancellationToken)
    {
        var isbn = TryCreateIsbn(candidate);
        var title = Clean(candidate.Title);
        var authors = CreateAuthors(candidate.Authors)
            .Take(Book.MaxAuthors)
            .ToList();

        if (isbn is null || string.IsNullOrWhiteSpace(title) || authors.Count == 0)
        {
            return new ImportOutcome(ImportResult.Skipped, null);
        }

        var existingBook = await bookRepository.GetByIsbnAsync(isbn.Value, cancellationToken);
        if (existingBook is not null)
        {
            var result = await EnrichExistingBookAsync(existingBook, candidate, cancellationToken);
            return new ImportOutcome(result, existingBook.Id);
        }

        var duplicateByTitleAndAuthor = await bookRepository.FindByTitleAndFirstAuthorAsync(
            title,
            authors[0].Name,
            cancellationToken);

        if (duplicateByTitleAndAuthor is not null)
        {
            var result = await EnrichExistingBookAsync(duplicateByTitleAndAuthor, candidate, cancellationToken);
            return new ImportOutcome(result, duplicateByTitleAndAuthor.Id);
        }

        try
        {
            var book = Book.Create(
                isbn,
                title,
                authors,
                requestedByUserId,
                Clean(candidate.Synopsis),
                candidate.PageCount,
                Clean(candidate.Publisher),
                ResolveCoverUrl(candidate.CoverUrl, isbn.Value),
                CreateTags(candidate.Tags).Take(Book.MaxTags));

            await bookRepository.AddAsync(book, cancellationToken);
            return new ImportOutcome(ImportResult.Imported, book.Id);
        }
        catch (DomainException ex)
        {
            logger.LogDebug(
                ex,
                "Skipped external book candidate {Provider}:{ProviderBookId}",
                candidate.Provider,
                candidate.ProviderBookId);
            return new ImportOutcome(ImportResult.Skipped, null);
        }
    }

    private async Task<ImportResult> EnrichExistingBookAsync(
        Book book,
        ExternalBookCandidate candidate,
        CancellationToken cancellationToken)
    {
        var synopsis = string.IsNullOrWhiteSpace(book.Synopsis) ? Clean(candidate.Synopsis) : null;
        var pageCount = book.PageCount.HasValue ? null : candidate.PageCount;
        var publisher = string.IsNullOrWhiteSpace(book.Publisher) ? Clean(candidate.Publisher) : null;
        var coverUrl = string.IsNullOrWhiteSpace(book.CoverUrl)
            ? ResolveCoverUrl(candidate.CoverUrl, book.Isbn.Value)
            : null;
        var tags = CreateTags(candidate.Tags)
            .Where(tag => book.Tags.All(existing => existing.Slug != tag.Slug))
            .Take(Math.Max(0, Book.MaxTags - book.Tags.Count))
            .ToList();

        var hasDetailChanges = synopsis is not null
                               || pageCount.HasValue
                               || publisher is not null
                               || coverUrl is not null;
        var hasTags = tags.Count > 0;

        if (!hasDetailChanges && !hasTags)
        {
            return ImportResult.Skipped;
        }

        if (hasDetailChanges)
        {
            book.UpdateDetails(
                synopsis: synopsis,
                pageCount: pageCount,
                publisher: publisher,
                coverUrl: coverUrl);
        }

        if (hasTags)
        {
            book.AddTags(tags);
        }

        if (pageCount.HasValue || coverUrl is not null)
        {
            book.RaiseUpdatedEvent();
        }

        await bookRepository.UpdateAsync(book, cancellationToken);
        return ImportResult.Updated;
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

    private static Isbn? TryCreateIsbn(ExternalBookCandidate candidate)
    {
        var isbn = SelectIsbn(candidate);
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return null;
        }

        try
        {
            return Isbn.Create(isbn);
        }
        catch (DomainException)
        {
            return null;
        }
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

    /// <summary>
    /// Uses the provider's cover when present, otherwise falls back to a
    /// deterministic ISBN-addressable cover so books still get an image when the
    /// search results omitted one.
    /// </summary>
    private string? ResolveCoverUrl(string? candidateCoverUrl, string isbn)
    {
        return Clean(candidateCoverUrl) ?? coverUrlResolver.ResolveByIsbn(isbn);
    }

    private static IReadOnlyList<string> CleanList(IEnumerable<string>? values)
    {
        return values?
            .Select(Clean)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList() ?? [];
    }

    private static IEnumerable<Author> CreateAuthors(IEnumerable<string>? authorNames)
    {
        foreach (var authorName in CleanList(authorNames))
        {
            Author author;
            try
            {
                author = Author.Create(authorName);
            }
            catch (DomainException)
            {
                continue;
            }

            yield return author;
        }
    }

    private static IEnumerable<Tag> CreateTags(IEnumerable<string>? tagNames)
    {
        foreach (var tagName in CleanList(tagNames))
        {
            Tag tag;
            try
            {
                tag = Tag.Create(tagName);
            }
            catch (DomainException)
            {
                continue;
            }

            yield return tag;
        }
    }

    private static string? NormalizeText(string? value)
    {
        return Clean(value)?.ToLowerInvariant();
    }

    private enum ImportResult
    {
        Imported,
        Updated,
        Skipped
    }

    private readonly record struct ImportOutcome(ImportResult Result, Guid? BookId);

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
