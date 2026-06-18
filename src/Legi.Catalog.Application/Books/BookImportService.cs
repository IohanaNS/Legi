using System.Text.RegularExpressions;
using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Application.Common.Storage;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Legi.Catalog.Application.Books;

public sealed partial class BookImportService(
    IBookRepository bookRepository,
    IWorkRepository workRepository,
    IBookDataProvider bookDataProvider,
    IBookCoverUrlResolver coverUrlResolver,
    IBookCoverAcquisition coverAcquisition,
    ICoverIngestionQueue coverIngestionQueue,
    ILogger<BookImportService> logger)
{
    public async Task<Book> CreateManualAsync(
        BookImportInput input,
        CancellationToken cancellationToken)
    {
        var isbn = Isbn.Create(input.Isbn);

        var existingBook = await bookRepository.GetByIsbnAsync(isbn.Value, cancellationToken);
        if (existingBook is not null)
        {
            throw DuplicateBookConflict(
                $"A book with ISBN '{input.Isbn}' already exists.",
                existingBook.Id);
        }

        var externalData = await bookDataProvider.GetByIsbnAsync(isbn.Value, cancellationToken);
        var title = UseUserValueOrFallback(input.Title, externalData?.Title);
        var userAuthors = CleanList(input.Authors);
        var authorNames = userAuthors.Count > 0 ? userAuthors : CleanList(externalData?.Authors);
        var synopsis = UseUserValueOrFallback(input.Synopsis, externalData?.Synopsis);
        var pageCount = input.PageCount ?? externalData?.PageCount;
        var publisher = UseUserValueOrFallback(input.Publisher, externalData?.Publisher);
        // Acquire-cover inline: validate the candidate URLs by fetching and store
        // the first real one to the owned bucket. The user deliberately added one
        // book and already waits on the provider lookup, so ~1-3s is acceptable.
        // Returns an owned blob URL or null (cover-less is a valid, complete book).
        var coverUrl = await AcquireCoverAsync(
            UseUserValueOrFallback(input.CoverUrl, externalData?.CoverUrl),
            isbn.Value,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException(
                "Title is required when not available from external book sources.");
        }

        if (authorNames.Count == 0)
        {
            throw new DomainException(
                "At least one author is required when not available from external book sources.");
        }

        var duplicateByTitleAndAuthor = await bookRepository.FindByTitleAndFirstAuthorAsync(
            title,
            authorNames[0],
            cancellationToken);

        if (duplicateByTitleAndAuthor is not null)
        {
            throw DuplicateBookConflict(
                $"A book with title '{title}' and first author '{authorNames[0]}' already exists.",
                duplicateByTitleAndAuthor.Id);
        }

        var authorObjs = authorNames.Select(Author.Create).ToList();

        // Resolve-or-create the work BEFORE the book so the work id is known at
        // creation time and flows into BookCreatedDomainEvent (→ the integration
        // event consumers project from).
        var workId = await ResolveWorkIdAsync(
            externalData?.WorkKey, title, authorObjs[0].Name, coverUrl, cancellationToken);

        var book = Book.Create(
            isbn,
            title,
            authorObjs,
            input.CreatedByUserId,
            synopsis,
            pageCount,
            publisher,
            coverUrl,
            CreateTags(input.Tags).Take(Book.MaxTags),
            providerWorkKey: externalData?.WorkKey,
            workId: workId);

        await bookRepository.AddAsync(book, cancellationToken);
        await EnqueueCoverDiscoveryIfMissingAsync(book, coverUrl, cancellationToken);
        return book;
    }

    public async Task<BookImportOutcome> ImportCandidateAsync(
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
            return new BookImportOutcome(BookImportResult.Skipped, null);
        }

        var existingBook = await bookRepository.GetByIsbnAsync(isbn.Value, cancellationToken);
        if (existingBook is not null)
        {
            var result = await EnrichExistingBookAsync(existingBook, candidate, cancellationToken);
            return new BookImportOutcome(result, existingBook.Id);
        }

        var duplicateByTitleAndAuthor = await bookRepository.FindByTitleAndFirstAuthorAsync(
            title,
            authors[0].Name,
            cancellationToken);

        if (duplicateByTitleAndAuthor is not null)
        {
            var result = await EnrichExistingBookAsync(duplicateByTitleAndAuthor, candidate, cancellationToken);
            return new BookImportOutcome(result, duplicateByTitleAndAuthor.Id);
        }

        try
        {
            // Runs inside the external-search background worker, so acquire-cover
            // here isn't blocking a user request — store the owned cover inline.
            var coverUrl = await AcquireCoverAsync(candidate.CoverUrl, isbn.Value, cancellationToken);
            var workId = await ResolveWorkIdAsync(
                candidate.WorkKey, title, authors[0].Name, coverUrl, cancellationToken);

            var book = Book.Create(
                isbn,
                title,
                authors,
                requestedByUserId,
                Clean(candidate.Synopsis),
                candidate.PageCount,
                Clean(candidate.Publisher),
                coverUrl,
                CreateTags(candidate.Tags).Take(Book.MaxTags),
                providerWorkKey: candidate.WorkKey,
                workId: workId);

            await bookRepository.AddAsync(book, cancellationToken);
            await EnqueueCoverDiscoveryIfMissingAsync(book, coverUrl, cancellationToken);
            return new BookImportOutcome(BookImportResult.Imported, book.Id);
        }
        catch (DomainException ex)
        {
            logger.LogDebug(
                ex,
                "Skipped external book candidate {Provider}:{ProviderBookId}",
                candidate.Provider,
                candidate.ProviderBookId);
            return new BookImportOutcome(BookImportResult.Skipped, null);
        }
    }

    private async Task<BookImportResult> EnrichExistingBookAsync(
        Book book,
        ExternalBookCandidate candidate,
        CancellationToken cancellationToken)
    {
        var synopsis = string.IsNullOrWhiteSpace(book.Synopsis) ? Clean(candidate.Synopsis) : null;
        var pageCount = book.PageCount.HasValue ? null : candidate.PageCount;
        var publisher = string.IsNullOrWhiteSpace(book.Publisher) ? Clean(candidate.Publisher) : null;
        // Only backfill a cover when the book has none; acquire+store an owned one
        // so the RaiseUpdatedEvent below republishes the blob URL to Library/Social.
        var coverUrl = string.IsNullOrWhiteSpace(book.CoverUrl)
            ? await AcquireCoverAsync(candidate.CoverUrl, book.Isbn.Value, cancellationToken)
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
            return BookImportResult.Skipped;
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
        await EnqueueCoverDiscoveryIfMissingAsync(book, book.CoverUrl, cancellationToken);
        return BookImportResult.Updated;
    }

    /// <summary>
    /// When a book ends up cover-less after import, enqueue a durable discovery job
    /// (idempotent per book) so the worker re-probes the providers later — the
    /// safety net for a transient miss or a cover the providers add afterwards.
    /// Never lets a queue hiccup fail the import.
    /// </summary>
    private async Task EnqueueCoverDiscoveryIfMissingAsync(
        Book book,
        string? coverUrl,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(coverUrl))
            return;

        try
        {
            await coverIngestionQueue.EnqueueAsync(book.Id, book.Isbn.Value, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to enqueue cover discovery for book {BookId}", book.Id);
        }
    }

    private static ConflictException DuplicateBookConflict(string message, Guid existingBookId)
    {
        return new ConflictException(
            message,
            new Dictionary<string, object?> { ["existingBookId"] = existingBookId });
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

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : WhitespaceRegex().Replace(value.Trim(), " ");
    }

    /// <summary>
    /// The acquire-cover operation for the import paths: fan out across the
    /// preferred cover URL (user/provider) and the ISBN-addressable fallback,
    /// validate by fetching, and on success store to the owned bucket — returning
    /// the blob URL or null. Never throws; a null result is a valid cover-less
    /// book (eligible for later background discovery). We deliberately no longer
    /// persist an unvalidated external URL (locked decision 2).
    /// </summary>
    private async Task<string?> AcquireCoverAsync(
        string? preferredCoverUrl,
        string isbn,
        CancellationToken cancellationToken)
    {
        var candidates = new List<string?>
        {
            Clean(preferredCoverUrl),
            coverUrlResolver.ResolveByIsbn(isbn)
        };

        return await coverAcquisition.AcquireAsync(isbn, candidates, cancellationToken);
    }

    /// <summary>
    /// Resolves-or-creates the <see cref="Work"/> for an edition about to be
    /// created, by the work key computed identically to <see cref="Book.Create"/>
    /// (so <c>Book.WorkKey</c> and <c>Work.WorkKey</c> agree), and returns its id.
    /// A new edition contributes its cover as the work's default when the work
    /// doesn't have one yet. Runs before <see cref="Book.Create"/> so the work id
    /// is known at creation time (it flows into <c>BookCreatedDomainEvent</c>).
    /// </summary>
    private async Task<Guid> ResolveWorkIdAsync(
        string? providerWorkKey,
        string title,
        string primaryAuthorName,
        string? coverUrl,
        CancellationToken cancellationToken)
    {
        var workKey = WorkKey.Resolve(providerWorkKey, title.Trim(), primaryAuthorName);
        var work = await workRepository.GetByWorkKeyAsync(workKey.Value, cancellationToken);

        if (work is null)
        {
            work = Work.Create(workKey, title, coverUrl);
            await workRepository.AddAsync(work, cancellationToken);
        }
        else if (work.DefaultCoverUrl is null && !string.IsNullOrWhiteSpace(coverUrl))
        {
            work.EnsureDefaultCover(coverUrl);
            await workRepository.UpdateAsync(work, cancellationToken);
        }

        return work.Id;
    }

    private static string? UseUserValueOrFallback(string? userValue, string? externalValue)
    {
        return !string.IsNullOrWhiteSpace(userValue) ? Clean(userValue) : Clean(externalValue);
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

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}

public sealed record BookImportInput(
    string Isbn,
    string? Title,
    IReadOnlyList<string>? Authors,
    Guid CreatedByUserId,
    string? Synopsis = null,
    int? PageCount = null,
    string? Publisher = null,
    string? CoverUrl = null,
    IReadOnlyList<string>? Tags = null);

public sealed record BookImportOutcome(BookImportResult Result, Guid? BookId);

public enum BookImportResult
{
    Imported,
    Updated,
    Skipped
}
