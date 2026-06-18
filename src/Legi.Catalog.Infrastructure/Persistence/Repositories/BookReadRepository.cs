using Legi.Catalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure.Persistence.Repositories;

public class BookReadRepository(CatalogDbContext context) : IBookReadRepository
{
    public async Task<(List<BookSearchResult> Books, int TotalCount)> SearchAsync(
        string? searchTerm,
        string? authorSlug,
        IReadOnlyList<string>? tagSlugs,
        decimal? minRating,
        int pageNumber,
        int pageSize,
        BookSortBy sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        var query = context.Books.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearch = searchTerm.ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(normalizedSearch) ||
                b.Isbn.Value.Contains(normalizedSearch) ||
                // A free-text term that names an author should surface that
                // author's books, not just books with the term in the title.
                context.BookAuthors
                    .Where(ba => ba.BookId == b.Id)
                    .Join(context.Authors,
                        ba => ba.AuthorId,
                        a => a.Id,
                        (ba, a) => a)
                    .Any(a => a.Name.ToLower().Contains(normalizedSearch)) ||
                // Books surfaced via external search are aliased to the query that
                // found them, so terms absent from the title/ISBN still match
                // (e.g. cross-language titles: "redoma" → "The Bell Jar").
                context.BookSearchAliases
                    .Any(a => a.BookId == b.Id && a.Alias.Contains(normalizedSearch))
            );
        }

        if (!string.IsNullOrWhiteSpace(authorSlug))
        {
            query = query.Where(b =>
                context.BookAuthors
                    .Where(ba => ba.BookId == b.Id)
                    .Join(context.Authors,
                        ba => ba.AuthorId,
                        a => a.Id,
                        (ba, a) => a)
                    .Any(a => a.Slug == authorSlug)
            );
        }

        if (tagSlugs is not null)
        {
            // AND semantics: each selected tag adds a separate filter, so a book
            // must carry every requested tag to survive (narrowing, not widening).
            foreach (var slug in tagSlugs.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
            {
                var tagSlug = slug;
                query = query.Where(b =>
                    context.BookTags
                        .Where(bt => bt.BookId == b.Id)
                        .Join(context.Tags,
                            bt => bt.TagId,
                            t => t.Id,
                            (bt, t) => t)
                        .Any(t => t.Slug == tagSlug)
                );
            }
        }

        if (minRating.HasValue)
        {
            query = query.Where(b => b.AverageRating >= minRating.Value);
        }

        // Collapse editions to one result per work so search returns works, not
        // duplicate editions of the same book. The representative is the work's
        // most-rated edition (tie-break: oldest), chosen among the editions that
        // matched the filters above. On singleton-work data this is a no-op.
        // (Work-level aggregate rating/title is deferred to the rating→Work step;
        //  until then the representative edition's own fields stand in.)
        var filtered = query;
        query = filtered.Where(b => !filtered.Any(b2 =>
            b2.WorkId == b.WorkId
            && b2.Id != b.Id
            && (b2.RatingsCount > b.RatingsCount
                || (b2.RatingsCount == b.RatingsCount && b2.CreatedAt < b.CreatedAt))));

        // Get a total count before pagination (now counts distinct matching works)
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = sortBy switch
        {
            BookSortBy.Title => sortDescending
                ? query.OrderByDescending(b => b.Title)
                : query.OrderBy(b => b.Title),
            BookSortBy.AverageRating => sortDescending
                ? query.OrderByDescending(b => b.AverageRating)
                : query.OrderBy(b => b.AverageRating),
            BookSortBy.RatingsCount => sortDescending
                ? query.OrderByDescending(b => b.RatingsCount)
                : query.OrderBy(b => b.RatingsCount),
            BookSortBy.CreatedAt => sortDescending
                ? query.OrderByDescending(b => b.CreatedAt)
                : query.OrderBy(b => b.CreatedAt),
            _ => sortDescending
                ? query.OrderByDescending(b => b.CreatedAt)
                : query.OrderBy(b => b.CreatedAt)
        };

        // Apply pagination
        var books = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                Book = b,
                Authors = context.BookAuthors
                    .Where(ba => ba.BookId == b.Id)
                    .OrderBy(ba => ba.Order)
                    .Join(context.Authors,
                        ba => ba.AuthorId,
                        a => a.Id,
                        (ba, a) => new { a.Name, a.Slug })
                    .ToList(),
                Tags = context.BookTags
                    .Where(bt => bt.BookId == b.Id)
                    .Join(context.Tags,
                        bt => bt.TagId,
                        t => t.Id,
                        (bt, t) => new { t.Name, t.Slug })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var results = books.Select(b => new BookSearchResult(
            b.Book.Id,
            b.Book.Isbn.Value,
            b.Book.Title,
            b.Authors.Select(a => (a.Name, a.Slug)).ToList(),
            b.Book.CoverUrl,
            b.Book.AverageRating,
            b.Book.RatingsCount,
            b.Tags.Select(t => (t.Name, t.Slug)).ToList()
        )).ToList();

        return (results, totalCount);
    }

    public async Task<BookDetailsResult?> GetBookDetailsByIdAsync(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var result = await context.Books
            .Where(b => b.Id == bookId)
            .Select(b => new
            {
                Book = b,
                // The rating shown on the book page is the work's aggregate across
                // its editions (attachment map: rating → Work).
                Work = context.Works
                    .Where(w => w.Id == b.WorkId)
                    .Select(w => new { w.AverageRating, w.RatingsCount, w.ReviewsCount })
                    .FirstOrDefault(),
                Authors = context.BookAuthors
                    .Where(ba => ba.BookId == b.Id)
                    .OrderBy(ba => ba.Order)
                    .Join(context.Authors,
                        ba => ba.AuthorId,
                        a => a.Id,
                        (ba, a) => new { a.Name, a.Slug })
                    .ToList(),
                Tags = context.BookTags
                    .Where(bt => bt.BookId == b.Id)
                    .Join(context.Tags,
                        bt => bt.TagId,
                        t => t.Id,
                        (bt, t) => new { t.Name, t.Slug })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return null;

        return new BookDetailsResult(
            result.Book.Id,
            result.Book.Isbn.Value,
            result.Book.Title,
            result.Authors.Select(a => (a.Name, a.Slug)).ToList(),
            result.Book.Synopsis,
            result.Book.PageCount,
            result.Book.Publisher,
            result.Book.CoverUrl,
            result.Work?.AverageRating ?? result.Book.AverageRating,
            result.Work?.RatingsCount ?? result.Book.RatingsCount,
            result.Work?.ReviewsCount ?? result.Book.ReviewsCount,
            result.Tags.Select(t => (t.Name, t.Slug)).ToList(),
            result.Book.CreatedByUserId,
            result.Book.CreatedAt,
            result.Book.UpdatedAt,
            result.Book.WorkId
        );
    }

    public Task<List<EditionResult>> GetEditionsByWorkIdAsync(
        Guid workId,
        CancellationToken cancellationToken = default)
    {
        return context.Books
            .Where(b => b.WorkId == workId)
            .OrderBy(b => b.CreatedAt)
            .Select(b => new EditionResult(
                b.Id,
                b.Isbn.Value,
                b.Title,
                b.CoverUrl,
                b.Publisher,
                b.PageCount))
            .ToListAsync(cancellationToken);
    }

    public async Task<BookDetailsResult?> GetBookDetailsByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default)
    {
        var normalizedIsbn = isbn.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();

        var result = await context.Books
            .Where(b => b.Isbn.Value == normalizedIsbn)
            .Select(b => new
            {
                Book = b,
                Work = context.Works
                    .Where(w => w.Id == b.WorkId)
                    .Select(w => new { w.AverageRating, w.RatingsCount, w.ReviewsCount })
                    .FirstOrDefault(),
                Authors = context.BookAuthors
                    .Where(ba => ba.BookId == b.Id)
                    .OrderBy(ba => ba.Order)
                    .Join(context.Authors,
                        ba => ba.AuthorId,
                        a => a.Id,
                        (ba, a) => new { a.Name, a.Slug })
                    .ToList(),
                Tags = context.BookTags
                    .Where(bt => bt.BookId == b.Id)
                    .Join(context.Tags,
                        bt => bt.TagId,
                        t => t.Id,
                        (bt, t) => new { t.Name, t.Slug })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return null;

        return new BookDetailsResult(
            result.Book.Id,
            result.Book.Isbn.Value,
            result.Book.Title,
            result.Authors.Select(a => (a.Name, a.Slug)).ToList(),
            result.Book.Synopsis,
            result.Book.PageCount,
            result.Book.Publisher,
            result.Book.CoverUrl,
            result.Work?.AverageRating ?? result.Book.AverageRating,
            result.Work?.RatingsCount ?? result.Book.RatingsCount,
            result.Work?.ReviewsCount ?? result.Book.ReviewsCount,
            result.Tags.Select(t => (t.Name, t.Slug)).ToList(),
            result.Book.CreatedByUserId,
            result.Book.CreatedAt,
            result.Book.UpdatedAt,
            result.Book.WorkId
        );
    }
}