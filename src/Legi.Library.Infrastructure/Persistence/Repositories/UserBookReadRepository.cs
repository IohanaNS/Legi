using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.Library.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Legi.Library.Infrastructure.Persistence.Repositories;

public class UserBookReadRepository : IUserBookReadRepository
{
    private readonly LibraryDbContext _context;

    public UserBookReadRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<UserBookDto>> GetByUserIdAsync(
        Guid userId,
        ReadingStatus? statusFilter,
        bool? wishlistFilter,
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserBooks
            .AsNoTracking()
            .Where(ub => ub.UserId == userId);

        // Apply filters
        if (statusFilter.HasValue)
            query = query.Where(ub => ub.Status == statusFilter.Value);

        if (wishlistFilter.HasValue)
            query = query.Where(ub => ub.WishList == wishlistFilter.Value);

        // Join with BookSnapshot for search and display
        var joined = query.Join(
            _context.BookSnapshots,
            ub => ub.BookId,
            bs => bs.BookId,
            (ub, bs) => new { UserBook = ub, Snapshot = bs });

        // Text search on title and author
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            joined = joined.Where(x =>
                x.Snapshot.Title.ToLower().Contains(term) ||
                x.Snapshot.AuthorDisplay.ToLower().Contains(term));
        }

        // Count before pagination
        var totalCount = await joined.CountAsync(cancellationToken);

        // Project to DTO with pagination
        var items = await joined
            .OrderByDescending(x => x.UserBook.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserBookDto(
                x.UserBook.Id,
                x.UserBook.BookId,
                x.UserBook.Status.ToString(),
                x.UserBook.CurrentProgress != null ? x.UserBook.CurrentProgress.Value : null,
                x.UserBook.CurrentProgress != null ? x.UserBook.CurrentProgress.Type.ToString() : null,
                x.UserBook.WishList,
                x.UserBook.CurrentRating != null ? x.UserBook.CurrentRating.Stars : null,
                new BookSnapshotDto(
                    x.Snapshot.BookId,
                    x.Snapshot.Title,
                    x.Snapshot.AuthorDisplay,
                    x.Snapshot.CoverUrl,
                    x.Snapshot.PageCount),
                x.UserBook.CreatedAt,
                x.UserBook.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<UserBookDto>(items, totalCount, pageNumber, pageSize);
    }
}