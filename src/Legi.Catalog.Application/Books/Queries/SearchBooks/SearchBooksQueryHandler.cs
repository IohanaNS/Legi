using Legi.Catalog.Application.Books.DTOs;
using Legi.Catalog.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;
using Legi.Catalog.Domain.Repositories;

namespace Legi.Catalog.Application.Books.Queries.SearchBooks;

public class SearchBooksQueryHandler(
    IBookReadRepository bookReadRepository,
    IExternalBookSearchQueue externalBookSearchQueue)
    : IRequestHandler<SearchBooksQuery, SearchBooksResponse>
{
    // Final cap on how many external books we import per query. Passed to each
    // provider as its page size too — Open Library serves up to 50 and Google up
    // to 40 in a single call, so the union (after dedup) fills this cap without
    // any extra API calls than the 2 we already make (one per provider).
    private const int MaxExternalCandidates = 50;

    public async Task<SearchBooksResponse> Handle(
        SearchBooksQuery request,
        CancellationToken cancellationToken)
    {
        // Execute search
        var (books, totalCount) = await bookReadRepository.SearchAsync(
            request.SearchTerm,
            request.AuthorSlug,
            request.TagSlug,
            request.MinRating,
            request.PageNumber,
            request.PageSize,
            request.SortBy,
            request.SortDescending,
            cancellationToken
        );

        // Map to DTOs
        var bookDtos = books.Select(b => new BookSummaryDto(
            b.Id,
            b.Isbn,
            b.Title,
            b.Authors.Select(a => new AuthorDto(a.Name, a.Slug)).ToList(),
            b.CoverUrl,
            b.AverageRating,
            b.RatingsCount,
            b.Tags.Select(t => new TagDto(t.Name, t.Slug)).ToList()
        )).ToList();

        // Build pagination metadata
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var pagination = new PaginationMetadata(
            CurrentPage: request.PageNumber,
            PageSize: request.PageSize,
            TotalCount: totalCount,
            TotalPages: totalPages,
            HasPrevious: request.PageNumber > 1,
            HasNext: request.PageNumber < totalPages
        );

        var enrichment = await GetEnrichmentAsync(request, totalCount, cancellationToken);

        return new SearchBooksResponse(bookDtos, pagination, enrichment);
    }

    private async Task<ExternalBookSearchEnrichment> GetEnrichmentAsync(
        SearchBooksQuery request,
        int totalCount,
        CancellationToken cancellationToken)
    {
        if (!IsPlainFirstPageTextSearch(request))
        {
            return ExternalBookSearchEnrichment.NotApplicable();
        }

        if (totalCount >= request.PageSize)
        {
            return ExternalBookSearchEnrichment.NotNeeded();
        }

        return await externalBookSearchQueue.QueueAsync(
            new ExternalBookSearchQueueRequest(
                request.SearchTerm!.Trim(),
                request.AuthenticatedUserId,
                MaxExternalCandidates),
            cancellationToken);
    }

    private static bool IsPlainFirstPageTextSearch(SearchBooksQuery request)
    {
        return !string.IsNullOrWhiteSpace(request.SearchTerm)
               && request.PageNumber == 1
               && string.IsNullOrWhiteSpace(request.AuthorSlug)
               && string.IsNullOrWhiteSpace(request.TagSlug)
               && !request.MinRating.HasValue;
    }
}
