using Legi.Catalog.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Legi.Catalog.Infrastructure.Persistence.Repositories;

public class BookReadRepository(CatalogDbContext context) : IBookReadRepository
{
    public async Task<(List<BookSearchResult> Books, int TotalCount)> SearchAsync(
        string? searchTerm,
        string? authorSlug,
        string? tagSlug,
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
                b.Isbn.Value.Contains(normalizedSearch)
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

        if (!string.IsNullOrWhiteSpace(tagSlug))
        {
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

        if (minRating.HasValue)
        {
            query = query.Where(b => b.AverageRating >= minRating.Value);
        }

        // Get a total count before pagination
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
            result.Book.AverageRating,
            result.Book.RatingsCount,
            result.Book.ReviewsCount,
            result.Tags.Select(t => (t.Name, t.Slug)).ToList(),
            result.Book.CreatedByUserId,
            result.Book.CreatedAt,
            result.Book.UpdatedAt
        );
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
            result.Book.AverageRating,
            result.Book.RatingsCount,
            result.Book.ReviewsCount,
            result.Tags.Select(t => (t.Name, t.Slug)).ToList(),
            result.Book.CreatedByUserId,
            result.Book.CreatedAt,
            result.Book.UpdatedAt
        );
    }
}