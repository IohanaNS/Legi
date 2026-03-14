using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Legi.Library.Infrastructure.Persistence.Repositories;

public class UserListReadRepository(LibraryDbContext context) : IUserListReadRepository
{
    public async Task<IReadOnlyList<UserListSummaryDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await context.UserLists
            .AsNoTracking()
            .Where(ul => ul.UserId == userId)
            .OrderByDescending(ul => ul.UpdatedAt)
            .Select(ul => new UserListSummaryDto(
                ul.Id,
                ul.Name,
                ul.Description,
                ul.IsPublic,
                ul.BooksCount,
                ul.LikesCount,
                ul.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserListDetailDto?> GetDetailByIdAsync(
        Guid listId,
        CancellationToken cancellationToken = default)
    {
        return await context.UserLists
            .AsNoTracking()
            .Where(ul => ul.Id == listId)
            .Select(ul => new UserListDetailDto(
                ul.Id,
                ul.UserId,
                ul.Name,
                ul.Description,
                ul.IsPublic,
                ul.BooksCount,
                ul.LikesCount,
                ul.CommentsCount,
                ul.CreatedAt,
                ul.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaginatedList<UserListBookDto>> GetListBooksAsync(
        Guid listId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.UserListItems
            .AsNoTracking()
            .Where(i => EF.Property<Guid>(i, "user_list_id") == listId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(i => i.Order)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Join(
                context.UserBooks,
                i => i.UserBookId,
                ub => ub.Id,
                (i, ub) => new { Item = i, UserBook = ub })
            .Join(
                context.BookSnapshots,
                x => x.UserBook.BookId,
                bs => bs.BookId,
                (x, bs) => new { x.Item, x.UserBook, Snapshot = bs })
            .Select(x => new UserListBookDto(
                x.Item.UserBookId,
                x.Item.Order,
                new BookSnapshotDto(
                    x.Snapshot.BookId,
                    x.Snapshot.Title,
                    x.Snapshot.AuthorDisplay,
                    x.Snapshot.CoverUrl,
                    x.Snapshot.PageCount),
                x.UserBook.Status.ToString(),
                x.UserBook.CurrentRating != null ? x.UserBook.CurrentRating.Stars : null,
                x.Item.AddedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<UserListBookDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedList<UserListSummaryDto>> SearchPublicAsync(
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.UserLists
            .AsNoTracking()
            .Where(ul => ul.IsPublic);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(ul =>
                ul.Name.ToLower().Contains(term) ||
                (ul.Description != null && ul.Description.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(ul => ul.LikesCount)
            .ThenByDescending(ul => ul.BooksCount)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ul => new UserListSummaryDto(
                ul.Id,
                ul.Name,
                ul.Description,
                ul.IsPublic,
                ul.BooksCount,
                ul.LikesCount,
                ul.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<UserListSummaryDto>(items, totalCount, pageNumber, pageSize);
    }
}