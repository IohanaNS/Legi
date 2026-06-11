using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Legi.Library.Infrastructure.Persistence.Repositories;

public class UserListReadRepository(
    LibraryDbContext context,
    IUserListVisibilityPolicy visibilityPolicy) : IUserListReadRepository
{
    private const int PreviewBookCount = 4;

    private sealed record SummaryRow(
        Guid ListId,
        Guid OwnerId,
        string Name,
        string? Description,
        bool IsPublic,
        int BooksCount,
        int LikesCount,
        DateTime CreatedAt);

    public async Task<IReadOnlyList<UserListSummaryDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.UserLists
            .AsNoTracking()
            .Where(ul => ul.UserId == userId)
            .OrderByDescending(ul => ul.UpdatedAt)
            .Select(ul => new SummaryRow(
                ul.Id, ul.UserId, ul.Name, ul.Description, ul.IsPublic, ul.BooksCount, ul.LikesCount, ul.CreatedAt))
            .ToListAsync(cancellationToken);

        return await BuildSummariesAsync(rows, cancellationToken);
    }

    public async Task<PaginatedList<UserListSummaryDto>> GetVisibleByUserIdAsync(
        Guid targetUserId,
        Guid viewerUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = VisibleListsForUser(targetUserId, viewerUserId);

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(ul => ul.UpdatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ul => new SummaryRow(
                ul.Id, ul.UserId, ul.Name, ul.Description, ul.IsPublic, ul.BooksCount, ul.LikesCount, ul.CreatedAt))
            .ToListAsync(cancellationToken);

        var summaries = await BuildSummariesAsync(rows, cancellationToken);

        return new PaginatedList<UserListSummaryDto>(summaries, totalCount, pageNumber, pageSize);
    }

    public async Task<int> CountVisibleByUserIdAsync(
        Guid targetUserId,
        Guid viewerUserId,
        CancellationToken cancellationToken = default)
    {
        return await VisibleListsForUser(targetUserId, viewerUserId)
            .CountAsync(cancellationToken);
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
                ul.UpdatedAt,
                false))
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
                context.BookSnapshots,
                i => i.BookId,
                bs => bs.BookId,
                (i, bs) => new UserListBookDto(
                    i.BookId,
                    i.Order,
                    new BookSnapshotDto(
                        bs.BookId,
                        bs.Title,
                        bs.AuthorDisplay,
                        bs.CoverUrl,
                        bs.PageCount),
                    i.AddedAt))
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

        var rows = await query
            .OrderByDescending(ul => ul.LikesCount)
            .ThenByDescending(ul => ul.BooksCount)
            .ThenByDescending(ul => ul.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ul => new SummaryRow(
                ul.Id, ul.UserId, ul.Name, ul.Description, ul.IsPublic, ul.BooksCount, ul.LikesCount, ul.CreatedAt))
            .ToListAsync(cancellationToken);

        var summaries = await BuildSummariesAsync(rows, cancellationToken);

        return new PaginatedList<UserListSummaryDto>(summaries, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyList<UserListSummaryDto>> GetPublicSummariesByIdsAsync(
        IReadOnlyList<Guid> listIds,
        CancellationToken cancellationToken = default)
    {
        if (listIds.Count == 0)
            return [];

        var rows = await context.UserLists
            .AsNoTracking()
            .Where(ul => ul.IsPublic && listIds.Contains(ul.Id))
            .Select(ul => new SummaryRow(
                ul.Id, ul.UserId, ul.Name, ul.Description, ul.IsPublic, ul.BooksCount, ul.LikesCount, ul.CreatedAt))
            .ToListAsync(cancellationToken);

        return await BuildSummariesAsync(rows, cancellationToken);
    }

    /// <summary>
    /// Maps the materialized base rows to <see cref="UserListSummaryDto"/>, populating
    /// <see cref="UserListSummaryDto.PreviewBooks"/> (up to <see cref="PreviewBookCount"/>
    /// covers per list) with a single query over the page of lists. Books missing a
    /// <c>BookSnapshot</c> are skipped.
    /// </summary>
    private async Task<IReadOnlyList<UserListSummaryDto>> BuildSummariesAsync(
        IReadOnlyList<SummaryRow> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return [];

        var listIds = rows.Select(r => r.ListId).ToList();

        var previewRows = await context.UserListItems
            .AsNoTracking()
            .Where(i => listIds.Contains(EF.Property<Guid>(i, "user_list_id")))
            .Join(
                context.BookSnapshots,
                i => i.BookId,
                bs => bs.BookId,
                (i, bs) => new
                {
                    ListId = EF.Property<Guid>(i, "user_list_id"),
                    i.Order,
                    Book = new BookSnapshotDto(bs.BookId, bs.Title, bs.AuthorDisplay, bs.CoverUrl, bs.PageCount)
                })
            .ToListAsync(cancellationToken);

        var previewsByList = previewRows
            .GroupBy(r => r.ListId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<BookSnapshotDto>)g
                    .OrderBy(r => r.Order)
                    .Take(PreviewBookCount)
                    .Select(r => r.Book)
                    .ToList());

        return rows
            .Select(r => new UserListSummaryDto(
                r.ListId,
                r.OwnerId,
                r.Name,
                r.Description,
                r.IsPublic,
                r.BooksCount,
                r.LikesCount,
                r.CreatedAt,
                previewsByList.TryGetValue(r.ListId, out var preview) ? preview : []))
            .ToList();
    }

    private IQueryable<Legi.Library.Domain.Entities.UserList> VisibleListsForUser(
        Guid targetUserId,
        Guid viewerUserId)
    {
        var canViewPrivateLists = visibilityPolicy.CanViewPrivateLists(targetUserId, viewerUserId);

        return context.UserLists
            .AsNoTracking()
            .Where(ul => ul.UserId == targetUserId)
            .Where(ul => canViewPrivateLists || ul.IsPublic);
    }
}
