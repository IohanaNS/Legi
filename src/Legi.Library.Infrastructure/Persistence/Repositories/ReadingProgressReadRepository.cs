using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Legi.Library.Infrastructure.Persistence.Repositories;

public class ReadingProgressReadRepository : IReadingPostReadRepository
{
    private readonly LibraryDbContext _context;

    public ReadingProgressReadRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<ReadingPostDto>> GetByUserBookIdAsync(
        Guid userBookId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReadingPosts
            .AsNoTracking()
            .Where(rp => rp.UserBookId == userBookId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(rp => rp.ReadingDate)
            .ThenByDescending(rp => rp.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(rp => new ReadingPostDto(
                rp.Id,
                rp.UserBookId,
                rp.Content,
                rp.CurrentProgress != null ? rp.CurrentProgress.Value : null,
                rp.CurrentProgress != null ? rp.CurrentProgress.Type.ToString() : null,
                rp.ReadingDate,
                rp.LikesCount,
                rp.CommentsCount,
                rp.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<ReadingPostDto>(items, totalCount, pageNumber, pageSize);
    }
}